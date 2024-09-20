"""Data exchanging service."""

import asyncio
import os
import pickle
import time
import uuid
from time import gmtime, strftime
from typing import Dict, List

import numpy as np
import rerun as rr

from arflow import service_pb2, service_pb2_grpc

sessions: Dict[str, service_pb2.RegisterRequest] = {}
"""@private"""


class ARFlowService(service_pb2_grpc.ARFlowService):
    """ARFlow gRPC service."""

    _num_frame: int = 0
    _loop = asyncio.get_event_loop()

    _start_time = time.time_ns()
    _frame_data: List[Dict[str, float | bytes]] = []

    def __init__(self) -> None:
        super().__init__()

    def _save_frame_data(
        self, request: service_pb2.DataFrameRequest | service_pb2.RegisterRequest
    ):
        """@private"""
        time_stamp = (time.time_ns() - self._start_time) / 1e9
        self._frame_data.append(
            {"time_stamp": time_stamp, "data": request.SerializeToString()}
        )

    def register(
        self, request: service_pb2.RegisterRequest, context, uid: str | None = None
    ) -> service_pb2.RegisterResponse:
        """Register a client."""

        self._save_frame_data(request)

        # Start processing.
        if uid is None:
            uid = str(uuid.uuid4())

        sessions[uid] = request

        rr.init(f"{request.device_name} - ARFlow", spawn=True)

        print("Registered a client with UUID: %s" % uid, request)

        return service_pb2.RegisterResponse(uid=uid)

    def data_frame(
        self, request: service_pb2.DataFrameRequest, context
    ) -> service_pb2.DataFrameResponse:
        """Process an incoming frame."""

        self._save_frame_data(request)

        # Start processing.
        decoded_data = {}
        session_configs = sessions[request.uid]

        if session_configs.camera_color.enabled:
            color_rgb = ARFlowService.decode_rgb_image(session_configs, request.color)
            decoded_data["color_rgb"] = color_rgb
            color_rgb = np.flipud(color_rgb)
            rr.log("rgb", rr.Image(color_rgb))

        if session_configs.camera_depth.enabled:
            depth_img = ARFlowService.decode_depth_image(session_configs, request.depth)
            decoded_data["depth_img"] = depth_img
            depth_img = np.flipud(depth_img)
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
            decoded_data["transform"] = transform
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
            decoded_data["point_cloud_pcd"] = pcd
            decoded_data["point_cloud_clr"] = clr
            rr.log("world/point_cloud", rr.Points3D(pcd, colors=clr))

        if session_configs.camera_plane_detection.enabled:
            pass

        if session_configs.gyroscope.enabled:
            gyroscope_data = request.gyroscope
            attitude = rr.Quaternion(gyroscope_data.attitude)
            rotation_rate = rr.Vector3(gyroscope_data.rotation_rate)
            gravity = rr.Vector3(gyroscope_data.gravity)
            acceleration = rr.Vector3(gyroscope_data.acceleration)
            rr.log(
                "world/gyroscope/rotations",
                rr.Arrows3D(attitude, colors=[[255, 0, 0]]),
                rr.Arrows3D(rotation_rate, colors=[[0, 255, 0]]),
            )
            rr.log(
                "world/gyroscope/acceleration",
                rr.Arrows3D(gravity, colors=[[0, 0, 255]]),
                rr.Arrows3D(acceleration, colors=[[255, 255, 0]]),
            )

        # Call the for user extension code.
        self.on_frame_received(decoded_data)

        return service_pb2.DataFrameResponse(message="OK")

    def on_frame_received(self, frame_data: service_pb2.DataFrameRequest):
        """Called when a frame is received. Override this method to process the data."""
        pass

    def on_program_exit(self, path_to_save: str):
        """Save the data and exit."""
        print("Saving the data...")
        f_name = strftime("%Y_%m_%d_%H_%M_%S", gmtime())
        save_path = os.path.join(path_to_save, f"frames_{f_name}.pkl")
        with open(save_path, "wb") as f:
            pickle.dump(self._frame_data, f)

        print(f"Data saved to {save_path}")

    @staticmethod
    def decode_rgb_image(
        session_configs: service_pb2.RegisterRequest, buffer: bytes
    ) -> np.ndarray:
        # Calculate the size of the image.
        color_img_w = int(
            session_configs.camera_intrinsics.resolution_x
            * session_configs.camera_color.resize_factor_x
        )
        color_img_h = int(
            session_configs.camera_intrinsics.resolution_y
            * session_configs.camera_color.resize_factor_y
        )
        p = color_img_w * color_img_h
        color_img = np.frombuffer(buffer, dtype=np.uint8)

        # Decode RGB bytes into RGB.
        if session_configs.camera_color.data_type == "RGB24":
            color_rgb = color_img.reshape((color_img_h, color_img_w, 3))
            color_rgb = color_rgb.astype(np.uint8)

        # Decode YCbCr bytes into RGB.
        elif session_configs.camera_color.data_type == "YCbCr420":
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