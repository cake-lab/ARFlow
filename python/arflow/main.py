"""Data exchanging service."""
import asyncio
import uuid
from concurrent import futures
from typing import Dict

import grpc
import numpy as np
import rerun as rr
from arflow import service_pb2, service_pb2_grpc

sessions: Dict[str, service_pb2.RegisterRequest] = {}
rr.init("arflow", spawn=True)


class ARFlowService(service_pb2_grpc.ARFlowService):
    """ARFlow gRPC service."""

    num_frame: int = 0
    loop = asyncio.get_event_loop()

    def __init__(self) -> None:
        super().__init__()

    def register(self, request: service_pb2.RegisterRequest, context):
        uid = str(uuid.uuid4())
        sessions[uid] = request

        print("Registered a client with UUID: %s" % uid, request)

        return service_pb2.RegisterResponse(message=uid)

    @staticmethod
    def decode_rgb_image(
        session_configs: service_pb2.RegisterRequest, buffer: bytes
    ) -> np.ndarray:
        # Decode YCbCr bytes into RGB.
        color_img = np.frombuffer(buffer, dtype=np.uint8)
        color_img_w = session_configs.camera_intrinsics.sample_resolution_x
        color_img_h = session_configs.camera_intrinsics.sample_resolution_y
        p = color_img_w * color_img_h

        y = color_img[:p].reshape((color_img_h, color_img_w))
        cbcr = color_img[p:].reshape((color_img_h // 2, color_img_w // 2, 2))
        cb, cr = cbcr[:, :, 0], cbcr[:, :, 1]

        # Very important! Convert to float32 first!
        cb = np.repeat(cb, 2, axis=0).repeat(2, axis=1).astype(np.float32) - 128
        cr = np.repeat(cr, 2, axis=0).repeat(2, axis=1).astype(np.float32) - 128

        r = np.clip(y + 1.403 * cr, 0, 255)
        g = np.clip(y - 0.344 * cb - 0.714 * cr, 0, 255)
        b = np.clip(y + 1.772 * cb, 0, 255)

        color_rgb = np.stack([r, g, b], axis=-1)
        color_rgb = color_rgb.astype(np.uint8)

        return color_rgb

    @staticmethod
    def decode_depth_image(
        session_configs: service_pb2.RegisterRequest, buffer: bytes
    ) -> np.ndarray:
        dtype = np.float32 if session_configs.depth_data_length == 4 else np.uint16
        depth_img = np.frombuffer(buffer, dtype=dtype)
        depth_img = depth_img.reshape(
            (session_configs.depth_resolution_y, session_configs.depth_resolution_x)
        )

        # 16-bit unsigned integer, describing the depth (distance to an object) in millimeters.
        if dtype == np.uint16:
            depth_img = depth_img.astype(np.float32) / 1000.0

        return depth_img

    @staticmethod
    def decode_pose(
        session_configs: service_pb2.RegisterRequest,
        buffer: bytes,
    ):
        sx = session_configs.color_resolution_x / session_configs.color_sample_size_x
        sy = session_configs.color_resolution_y / session_configs.color_sample_size_y

        fx, fy = (
            session_configs.focal_length_x / sx,
            session_configs.focal_length_y / sy,
        )
        cx, cy = (
            session_configs.principal_point_x / sx,
            session_configs.principal_point_y / sy,
        )

        y_down_to_y_up = np.array(
            [
                [1.0, -0.0, 0.0, 0],
                [0.0, -1.0, 0.0, 0],
                [0.0, 0.0, 1.0, 0],
                [0.0, 0.0, 0, 1.0],
            ],
            dtype=np.float32,
        )

        t = np.frombuffer(buffer, dtype=np.float32)
        transform = np.eye(4)
        transform[:3, :] = t.reshape((3, 4))
        transform[:3, 3] = 0
        transform = y_down_to_y_up @ transform
        k = np.array([[fx, 0, cx], [0, fy, cy], [0, 0, 1]])

        return transform, k

    @staticmethod
    def decode_point_cloud(
        session_configs: service_pb2.RegisterRequest,
        k: np.ndarray,
        color_rgb: np.ndarray,
        depth_img: np.ndarray,
        transform: np.ndarray,
    ) -> np.ndarray:
        # Flip image is needed for point cloud generation.
        color_rgb = np.flipud(color_rgb)
        depth_img = np.flipud(depth_img)

        color_img_w = session_configs.color_sample_size_x
        color_img_h = session_configs.color_sample_size_y
        u, v = np.meshgrid(np.arange(color_img_w), np.arange(color_img_h))
        fx, fy = k[0, 0], k[1, 1]
        cx, cy = k[0, 2], k[1, 2]

        z = depth_img.copy()
        x = ((u - cx) * z) / fx
        y = ((v - cy) * z) / fy
        pcd = np.stack([x, y, z], axis=-1).reshape(-1, 3)
        pcd = np.matmul(transform[:3, :3], pcd.T).T + transform[:3, 3]
        clr = color_rgb.reshape(-1, 3)

        return pcd, clr

    def data_frame(self, request: service_pb2.DataFrameRequest, context):
        session_configs = sessions[request.uid]

        if session_configs.camera_color.enabled:
            color_rgb = ARFlowService.decode_rgb_image(session_configs, request.color)
            rr.log("rgb", rr.Image(color_rgb))

        if session_configs.camera_depth.enabled:
            depth_img = ARFlowService.decode_depth_image(session_configs, request.depth)
            rr.log("depth", rr.DepthImage(depth_img, meter=1.0))

        # Log point cloud.
        if (
            session_configs.camera_color.enabled
            and session_configs.camera_depth.enabled
        ):
            entity_3d = "world"
            rr.log(entity_3d, rr.ViewCoordinates.RIGHT_HAND_Y_DOWN)
            # rr.log(
            #     f"{entity_3d}/xyz",
            #     rr.Arrows3D(
            #         vectors=[[1, 0, 0], [0, 1, 0], [0, 0, 1]],
            #         colors=[[255, 0, 0], [0, 255, 0], [0, 0, 255]],
            #     ),
            # )

            color_img_w = session_configs.camera_intrinsics.sample_resolution_x
            color_img_h = session_configs.camera_intrinsics.sample_resolution_y
            transform, k = ARFlowService.decode_pose(session_configs, request.pose)
            rr.log(
                f"{entity_3d}/camera",
                rr.Transform3D(mat3x3=transform[:3, :3], translation=transform[:3, 3]),
            )
            rr.log(
                f"{entity_3d}/camera",
                rr.Pinhole(resolution=[color_img_w, color_img_h], image_from_camera=k),
            )
            rr.log(f"{entity_3d}/camera", rr.Image(color_rgb))

            pcd, clr = ARFlowService.decode_point_cloud(
                session_configs, k, color_rgb, depth_img, transform
            )
            rr.log(f"{entity_3d}/point_cloud", rr.Points3D(pcd, colors=clr))

        return service_pb2.DataFrameResponse(message="OK")


def serve():
    """Run gRPC server."""
    port = 8500
    server = grpc.server(futures.ThreadPoolExecutor(max_workers=10))
    service_pb2_grpc.add_ARFlowServiceServicer_to_server(ARFlowService(), server)
    server.add_insecure_port("[::]:%s" % port)
    server.start()

    print(f"Register server started on port {port}")
    server.wait_for_termination()


if __name__ == "__main__":
    serve()
