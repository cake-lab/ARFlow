"""Data exchanging service."""

import os
import pickle
import time
import uuid
from abc import abstractmethod
from concurrent import futures
from pathlib import Path
from signal import SIGINT, SIGTERM, signal
from typing import Any, Type

import grpc
import numpy as np
import rerun as rr

from arflow._errors import (
    GRPC_CLIENT_CONFIG_NOT_FOUND,
    CannotReachException,
    GRPCError,
    set_grpc_error,
)
from arflow._types import (
    ARFlowRequest,
    ClientConfigurations,
    ColorRGB,
    DecodedDataFrame,
    DepthImg,
    EnrichedARFlowRequest,
    HashableClientIdentifier,
    Intrinsic,
    PointCloudCLR,
    PointCloudPCD,
    RequestsHistory,
    Transform,
)
from arflow_grpc import service_pb2_grpc
from arflow_grpc.service_pb2 import (
    Acknowledgement,
    ClientConfiguration,
    ClientIdentifier,
    DataFrame,
)


class ARFlowServicer(service_pb2_grpc.ARFlowServicer):
    """Provides methods that implement the functionality of the ARFlow gRPC server."""

    def __init__(self) -> None:
        """Initialize the ARFlowServicer."""
        self._start_time = time.time_ns()
        self._requests_history: RequestsHistory = []
        self._client_configurations: ClientConfigurations = {}
        self.recorder = rr
        """A recorder object for logging data."""
        super().__init__()

    def _save_request(self, request: ARFlowRequest):
        timestamp = (time.time_ns() - self._start_time) / 1e9
        enriched_request = EnrichedARFlowRequest(
            timestamp=timestamp,
            data=request,
        )
        self._requests_history.append(enriched_request)

    def RegisterClient(
        self,
        request: ClientConfiguration,
        context: grpc.ServicerContext | None = None,
        init_uid: str | None = None,
    ) -> ClientIdentifier:
        """Register a client.

        @private
        """
        self._save_request(request)

        if init_uid is None:
            init_uid = str(uuid.uuid4())

        self._client_configurations[HashableClientIdentifier(init_uid)] = request

        self.recorder.init(f"{request.device_name} - ARFlow", spawn=True)
        print("Registered a client with UUID: %s" % init_uid, request)

        # Call the for user extension code.
        self.on_register(request)

        return ClientIdentifier(uid=init_uid)

    def ProcessFrame(
        self,
        request: DataFrame,
        context: grpc.ServicerContext | None = None,
    ) -> Acknowledgement:
        """Process an incoming frame.

        @private
        """
        self._save_request(request)

        # Start processing.
        try:
            client_config = self._client_configurations[
                HashableClientIdentifier(request.uid)
            ]
        except KeyError:
            if context is not None:
                set_grpc_error(context, GRPC_CLIENT_CONFIG_NOT_FOUND)
            return Acknowledgement()

        color_rgb: ColorRGB | None = None
        depth_img: DepthImg | None = None
        transform: Transform | None = None
        k: Intrinsic | None = None
        point_cloud_pcd: PointCloudPCD | None = None
        point_cloud_clr: PointCloudCLR | None = None

        if client_config.camera_color.enabled:
            if (
                client_config.camera_color.data_type not in ["RGB24", "YCbCr420"]
                and context is not None
            ):
                set_grpc_error(
                    context,
                    GRPCError(
                        details=f"Unknown color data type: {client_config.camera_color.data_type}",
                        status_code=grpc.StatusCode.INVALID_ARGUMENT,
                    ),
                )
            color_rgb = np.flipud(_decode_rgb_image(client_config, request.color))
            self.recorder.log("rgb", rr.Image(color_rgb))

        if client_config.camera_depth.enabled:
            depth_img = np.flipud(_decode_depth_image(client_config, request.depth))
            # https://github.com/rerun-io/rerun/blob/79da203f08e719f3b56029893185c3631f2a8b54/rerun_py/rerun_sdk/rerun/archetypes/encoded_image_ext.py#L15
            self.recorder.log("depth", rr.DepthImage(depth_img, meter=1.0))  # type: ignore

        if client_config.camera_transform.enabled:
            self.recorder.log("world/origin", rr.ViewCoordinates.RIGHT_HAND_Y_DOWN)
            # self.logger.log(
            #     "world/xyz",
            #     rr.Arrows3D(
            #         vectors=[[1, 0, 0], [0, 1, 0], [0, 0, 1]],
            #         colors=[[255, 0, 0], [0, 255, 0], [0, 0, 255]],
            #     ),
            # )

            transform = _decode_transform(request.transform)
            self.recorder.log(
                "world/camera",
                self.recorder.Transform3D(
                    mat3x3=transform[:3, :3], translation=transform[:3, 3]
                ),
            )

            k = _decode_intrinsic(client_config)
            self.recorder.log("world/camera", rr.Pinhole(image_from_camera=k))
            if color_rgb is not None:
                self.recorder.log("world/camera", rr.Image(np.flipud(color_rgb)))

        if client_config.camera_point_cloud.enabled:
            if (
                k is not None
                and color_rgb is not None
                and depth_img is not None
                and transform is not None
            ):
                point_cloud_pcd, point_cloud_clr = _decode_point_cloud(
                    client_config, k, color_rgb, depth_img, transform
                )
                self.recorder.log(
                    "world/point_cloud",
                    rr.Points3D(point_cloud_pcd, colors=point_cloud_clr),
                )

        # Call the for user extension code.
        # TODO: I really, really cannot figure out why TypedDict with total=False is not working properly
        # so the type guard should not be necessary. The point is to provide type hints for UX but also
        # allow for missing keys.
        if (
            color_rgb is not None
            and depth_img is not None
            and transform is not None
            and k is not None
            and point_cloud_pcd is not None
            and point_cloud_clr is not None
        ):
            self.on_frame_received(
                DecodedDataFrame(
                    color_rgb=color_rgb,
                    depth_img=depth_img,
                    transform=transform,
                    intrinsic=k,
                    point_cloud_pcd=point_cloud_pcd,
                    point_cloud_clr=point_cloud_clr,
                )
            )

        return Acknowledgement(message="OK")

    @abstractmethod
    def on_register(self, request: ClientConfiguration) -> None:
        """Called when a new device is registered. Override this method to process the data."""
        pass

    @abstractmethod
    def on_frame_received(self, decoded_data_frame: DecodedDataFrame) -> None:
        """Called when a frame is received. Override this method to process the data."""
        pass

    def on_program_exit(self, path_to_save: Path) -> None:
        """Save the data and exit.

        @private
        """
        print("Saving the data...")
        # Ensure the directory exists.
        path_to_save.parent.mkdir(parents=True, exist_ok=True)
        save_path = os.path.join(
            path_to_save,
            f"frames_{time.strftime('%Y_%m_%d_%H_%M_%S', time.gmtime())}.pkl",
        )
        with open(save_path, "wb") as f:
            pickle.dump(self._requests_history, f)

        print(f"Data saved to {save_path}")


def _decode_rgb_image(client_config: ClientConfiguration, buffer: bytes) -> ColorRGB:
    # Calculate the size of the image.
    color_img_w = int(
        client_config.camera_intrinsics.resolution_x
        * client_config.camera_color.resize_factor_x
    )
    color_img_h = int(
        client_config.camera_intrinsics.resolution_y
        * client_config.camera_color.resize_factor_y
    )
    p = color_img_w * color_img_h
    color_img = np.frombuffer(buffer, dtype=np.uint8)

    # Decode RGB bytes into RGB.
    if client_config.camera_color.data_type == "RGB24":
        color_rgb = color_img.reshape((color_img_h, color_img_w, 3))
        color_rgb = color_rgb.astype(np.uint8)

    # Decode YCbCr bytes into RGB.
    elif client_config.camera_color.data_type == "YCbCr420":
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

    else:
        raise CannotReachException(
            f"Unknown color data type: {client_config.camera_color.data_type}"
        )

    return color_rgb


def _decode_depth_image(client_config: ClientConfiguration, buffer: bytes) -> DepthImg:
    if client_config.camera_depth.data_type == "f32":
        dtype = np.float32
    elif client_config.camera_depth.data_type == "u16":
        dtype = np.uint16
    else:
        raise ValueError(
            f"Unknown depth data type: {client_config.camera_depth.data_type}"
        )

    # The `Any` means that the array can have any shape. We cannot
    # determine the shape of the array from the buffer.
    depth_img = np.frombuffer(buffer, dtype=dtype).reshape(
        (
            client_config.camera_depth.resolution_y,
            client_config.camera_depth.resolution_x,
        )
    )

    # If it's a 16-bit unsigned integer, convert to float32 and scale to meters.
    if dtype == np.uint16:
        depth_img = (depth_img.astype(np.float32) / 1000.0).astype(np.float32)

    return depth_img


def _decode_transform(buffer: bytes) -> Transform:
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
    transform = np.eye(4, dtype=np.float32)
    transform[:3, :] = t.reshape((3, 4))
    transform[:3, 3] = 0
    transform = (y_down_to_y_up @ transform).astype(np.float32)

    return transform


def _decode_intrinsic(client_config: ClientConfiguration) -> Intrinsic:
    sx = client_config.camera_color.resize_factor_x
    sy = client_config.camera_color.resize_factor_y

    fx, fy = (
        client_config.camera_intrinsics.focal_length_x * sx,
        client_config.camera_intrinsics.focal_length_y * sy,
    )
    cx, cy = (
        client_config.camera_intrinsics.principal_point_x * sx,
        client_config.camera_intrinsics.principal_point_y * sy,
    )

    k = np.array([[fx, 0, cx], [0, fy, cy], [0, 0, 1]], dtype=np.float32)

    return k


def _decode_point_cloud(
    client_config: ClientConfiguration,
    k: Intrinsic,
    color_rgb: ColorRGB,
    depth_img: DepthImg,
    transform: Transform,
) -> tuple[PointCloudPCD, PointCloudCLR]:
    # Flip image is needed for point cloud generation.
    color_rgb = np.flipud(color_rgb)
    depth_img = np.flipud(depth_img)

    color_img_w = int(
        client_config.camera_intrinsics.resolution_x
        * client_config.camera_color.resize_factor_x
    )
    color_img_h = int(
        client_config.camera_intrinsics.resolution_y
        * client_config.camera_color.resize_factor_y
    )

    u, v = np.meshgrid(np.arange(color_img_w), np.arange(color_img_h))

    fx: np.float32 = k[0, 0]
    fy: np.float32 = k[1, 1]
    cx: np.float32 = k[0, 2]
    cy: np.float32 = k[1, 2]

    z = depth_img.copy()
    x = ((u - cx) * z) / fx
    y = ((v - cy) * z) / fy
    pre_pcd = np.stack([x, y, z], axis=-1).reshape(-1, 3)
    pcd: PointCloudPCD = np.matmul(transform[:3, :3], pre_pcd.T).T + transform[:3, 3]
    clr = color_rgb.reshape(-1, 3)

    return pcd, clr


def create_server(
    service: Type[ARFlowServicer], port: int = 8500, path_to_save: Path | None = None
):
    """Run gRPC server.

    Args:
        service: The service class to use. Custom servers should subclass `arflow.ARFlowServicer`.
        port: The port to listen on.
        path_to_save: The path to save data to.
    """
    servicer = service()
    server = grpc.server(  # type: ignore
        futures.ThreadPoolExecutor(max_workers=10),
        options=[
            ("grpc.max_send_message_length", -1),
            ("grpc.max_receive_message_length", -1),
        ],
    )
    service_pb2_grpc.add_ARFlowServicer_to_server(servicer, server)  # type: ignore
    server.add_insecure_port("[::]:%s" % port)
    server.start()
    print(f"Server started, listening on {port}")

    def handle_shutdown(*_: Any) -> None:
        """Shutdown gracefully.

        This function handles graceful shutdown of the server. It is triggered by termination signals,
        which are typically sent by Kubernetes or other orchestration tools to stop the service.

        - When running locally, pressing <Ctrl+C> will raise a `KeyboardInterrupt`, which can be caught to call this function.
        - In a Kubernetes environment, a SIGTERM signal is sent to the service, followed by a SIGKILL if the service does not stop within 30 seconds.

        Steps:
        1. Catch the SIGTERM signal.
        2. Call `server.stop(30)` to refuse new requests and wait up to 30 seconds for ongoing requests to complete.
        3. Wait on the `threading.Event` object returned by `server.stop(30)` to ensure Python does not exit prematurely.
        4. Optionally, perform cleanup procedures and save any necessary data before shutting down completely.
        """
        print("Received shutdown signal")
        all_rpcs_done_event = server.stop(30)
        all_rpcs_done_event.wait(30)

        if path_to_save is not None:
            print("Saving data...")
            servicer.on_program_exit(path_to_save)

        print("Shut down gracefully")

    signal(SIGTERM, handle_shutdown)
    signal(SIGINT, handle_shutdown)
    server.wait_for_termination()
