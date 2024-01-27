"""Data exchanging service."""
import asyncio
import os
import pickle
import sys
import time
import uuid
from concurrent import futures
from time import gmtime, strftime
from typing import Dict, List

import grpc
import numpy as np
import rerun as rr
from arflow import service_pb2, service_pb2_grpc

sessions: Dict[str, service_pb2.RegisterRequest] = {}


class ARFlowService(service_pb2_grpc.ARFlowService):
    """ARFlow gRPC service."""

    num_frame: int = 0
    loop = asyncio.get_event_loop()

    _start_time = time.time_ns()
    _frame_data: List = []

    def __init__(self) -> None:
        super().__init__()

    def register(
        self, request: service_pb2.RegisterRequest, context, uid: str = None
    ) -> service_pb2.RegisterResponse:
        """Register a client."""
        # Save the frame data.
        time_stamp = (time.time_ns() - self._start_time) / 1e9
        self._frame_data.append(
            {"time_stamp": time_stamp, "data": request.SerializeToString()}
        )

        # Start processing.
        if uid is None:
            uid = str(uuid.uuid4())
        sessions[uid] = request

        rr.init(f"{request.device_name} - ARFlow", spawn=True)

        print("Registered a client with UUID: %s" % uid, request)

        return service_pb2.RegisterResponse(message=uid)

    @staticmethod
    def decode_rgb_image(
        session_configs: service_pb2.RegisterRequest, buffer: bytes
    ) -> np.ndarray:
        # Decode YCbCr bytes into RGB.
        color_img = np.frombuffer(buffer, dtype=np.uint8)
        color_img_w = int(
            session_configs.camera_intrinsics.resolution_x
            * session_configs.camera_color.resize_factor_x
        )
        color_img_h = int(
            session_configs.camera_intrinsics.resolution_y
            * session_configs.camera_color.resize_factor_y
        )
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
        if session_configs.camera_depth.data_type == "f32":
            dtype = np.float32
        elif session_configs.camera_depth.data_type == "u16":
            dtype = np.uint16
        else:
            raise ValueError(
                f"Unknown depth data type: {session_configs.camera_depth.data_type}"
            )

        depth_img = np.frombuffer(buffer, dtype=dtype)
        depth_img = depth_img.reshape(
            (
                session_configs.camera_depth.resolution_y,
                session_configs.camera_depth.resolution_x,
            )
        )

        # 16-bit unsigned integer, describing the depth (distance to an object) in millimeters.
        if dtype == np.uint16:
            depth_img = depth_img.astype(np.float32) / 1000.0

        return depth_img

    @staticmethod
    def decode_transform(buffer: bytes):
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

        return transform

    @staticmethod
    def decode_intrinsic(session_configs: service_pb2.RegisterRequest):
        sx = session_configs.camera_color.resize_factor_x
        sy = session_configs.camera_color.resize_factor_y

        fx, fy = (
            session_configs.camera_intrinsics.focal_length_x * sx,
            session_configs.camera_intrinsics.focal_length_y * sy,
        )
        cx, cy = (
            session_configs.camera_intrinsics.principal_point_x * sx,
            session_configs.camera_intrinsics.principal_point_y * sy,
        )

        k = np.array([[fx, 0, cx], [0, fy, cy], [0, 0, 1]])

        return k

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

        color_img_w = int(
            session_configs.camera_intrinsics.resolution_x
            * session_configs.camera_color.resize_factor_x
        )
        color_img_h = int(
            session_configs.camera_intrinsics.resolution_y
            * session_configs.camera_color.resize_factor_y
        )
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
        # Save the frame data.
        time_stamp = (time.time_ns() - self._start_time) / 1e9
        self._frame_data.append(
            {"time_stamp": time_stamp, "data": request.SerializeToString()}
        )

        # Start processing.
        session_configs = sessions[request.uid]

        if session_configs.camera_color.enabled:
            color_rgb = ARFlowService.decode_rgb_image(session_configs, request.color)
            rr.log("rgb", rr.Image(color_rgb))

        if session_configs.camera_depth.enabled:
            depth_img = ARFlowService.decode_depth_image(session_configs, request.depth)
            rr.log("depth", rr.DepthImage(depth_img, meter=1.0))

        if session_configs.camera_transform.enabled:
            rr.log("world/origin", rr.ViewCoordinates.RIGHT_HAND_Y_DOWN)
            # rr.log(
            #     "world/xyz",
            #     rr.Arrows3D(
            #         vectors=[[1, 0, 0], [0, 1, 0], [0, 0, 1]],
            #         colors=[[255, 0, 0], [0, 255, 0], [0, 0, 255]],
            #     ),
            # )

            transform = ARFlowService.decode_transform(request.transform)
            rr.log(
                "world/camera",
                rr.Transform3D(mat3x3=transform[:3, :3], translation=transform[:3, 3]),
            )

            k = ARFlowService.decode_intrinsic(session_configs)
            rr.log("world/camera", rr.Pinhole(image_from_camera=k))
            rr.log("world/camera", rr.Image(np.flipud(color_rgb)))

        if session_configs.camera_point_cloud.enabled:
            pcd, clr = ARFlowService.decode_point_cloud(
                session_configs, k, color_rgb, depth_img, transform
            )
            rr.log("world/point_cloud", rr.Points3D(pcd, colors=clr))

        # Call the for user extension code.
        self.on_frame_received(request)

        return service_pb2.DataFrameResponse(message="OK")

    def on_frame_received(self, frame_data: service_pb2.DataFrameRequest):
        pass

    def on_program_exit(self, path_to_save):
        """Save the data and exit."""
        print("Saving the data...")
        f_name = strftime("%Y_%m_%d_%H_%M_%S", gmtime())
        save_path = os.path.join(path_to_save, f"frames_{f_name}.pkl")
        with open(save_path, "wb") as f:
            pickle.dump(self._frame_data, f)

        print(f"Data saved to {save_path}")


def create_server(service, port: int = 8500, path_to_save: str = "./"):
    """Run gRPC server."""
    try:
        s: ARFlowService = service()

        server = grpc.server(futures.ThreadPoolExecutor(max_workers=10))
        service_pb2_grpc.add_ARFlowServiceServicer_to_server(s, server)
        server.add_insecure_port("[::]:%s" % port)
        server.start()

        print(f"Register server started on port {port}")
        server.wait_for_termination()
    except KeyboardInterrupt:
        s.on_program_exit(path_to_save)
        sys.exit(0)


def serve():
    create_server(ARFlowService, port=8500)


if __name__ == "__main__":
    serve()
