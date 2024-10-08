"""Data exchanging service."""

import logging
import os
import pickle
import time
import uuid
from concurrent import futures
from pathlib import Path
from signal import SIGINT, SIGTERM, signal
from typing import Any, Literal, Type, cast

import DracoPy
import grpc
import numpy as np
import numpy.typing as npt
import rerun as rr
from grpc_interceptor import ExceptionToStatusInterceptor
from grpc_interceptor.exceptions import InvalidArgument, NotFound

from arflow._error_logger import ErrorLogger
from arflow._types import (
    ARFlowRequest,
    Audio,
    ClientConfigurations,
    ColorRGB,
    DecodedDataFrame,
    DepthImg,
    EnrichedARFlowRequest,
    GyroscopeInfo,
    HashableClientIdentifier,
    Intrinsic,
    Mesh,
    PlaneBoundaryPoints,
    PlaneCenter,
    PlaneInfo,
    PlaneNormal,
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

logger = logging.getLogger(__name__)


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
        logger.debug(
            "Registered a client with UUID: %s, Request: %s", init_uid, request
        )

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

        Raises:
            ValueError: If the request's data cannot be decoded (e.g., corrupted or invalid data).
            grpc_interceptor.exceptions.NotFound: If the client configuration is not found.
            grpc_interceptor.exceptions.InvalidArgument: If the color data type is not recognized or the depth data type is not recognized.
        """
        self._save_request(request)

        # Start processing.
        try:
            client_config = self._client_configurations[
                HashableClientIdentifier(request.uid)
            ]
        except KeyError:
            raise NotFound("Client configuration not found")

        color_rgb: ColorRGB | None = None
        depth_img: DepthImg | None = None
        transform: Transform | None = None
        k: Intrinsic | None = None
        point_cloud_pcd: PointCloudPCD | None = None
        point_cloud_clr: PointCloudCLR | None = None
        audio_data: Audio | None = None

        if client_config.camera_color.enabled:
            if client_config.camera_color.data_type not in ["RGB24", "YCbCr420"]:
                raise InvalidArgument(
                    f"Unknown color data type: {client_config.camera_color.data_type}"
                )
            color_rgb = np.flipud(
                _decode_rgb_image(
                    client_config.camera_intrinsics.resolution_y,
                    client_config.camera_intrinsics.resolution_x,
                    client_config.camera_color.resize_factor_y,
                    client_config.camera_color.resize_factor_x,
                    cast(
                        Literal["RGB24", "YCbCr420"],
                        client_config.camera_color.data_type,
                    ),
                    request.color,
                )
            )
            self.recorder.log("rgb", rr.Image(color_rgb))

        if client_config.camera_depth.enabled:
            if client_config.camera_depth.data_type not in ["f32", "u16"]:
                raise InvalidArgument(
                    f"Unknown depth data type: {client_config.camera_depth.data_type}"
                )
            depth_img = np.flipud(
                _decode_depth_image(
                    client_config.camera_depth.resolution_y,
                    client_config.camera_depth.resolution_x,
                    cast(Literal["f32", "u16"], client_config.camera_depth.data_type),
                    request.depth,
                )
            )
            # https://github.com/rerun-io/rerun/blob/79da203f08e719f3b56029893185c3631f2a8b54/rerun_py/rerun_sdk/rerun/archetypes/encoded_image_ext.py#L15
            self.recorder.log("depth", rr.DepthImage(depth_img, meter=1.0))

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

            k = _decode_intrinsic(
                client_config.camera_color.resize_factor_y,
                client_config.camera_color.resize_factor_x,
                client_config.camera_intrinsics.focal_length_y,
                client_config.camera_intrinsics.focal_length_x,
                client_config.camera_intrinsics.principal_point_y,
                client_config.camera_intrinsics.principal_point_x,
            )
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
                    client_config.camera_intrinsics.resolution_y,
                    client_config.camera_intrinsics.resolution_x,
                    client_config.camera_color.resize_factor_y,
                    client_config.camera_color.resize_factor_x,
                    k,
                    color_rgb,
                    depth_img,
                    transform,
                )
                self.recorder.log(
                    "world/point_cloud",
                    rr.Points3D(point_cloud_pcd, colors=point_cloud_clr),
                )

        if client_config.camera_plane_detection.enabled:
            strips: List[npt.NDArray[np.float32]] = []
            for plane in request.plane_detection:
                boundary_points_2d: List[List[float]] = list(
                    map(lambda pt: [pt.x, pt.y], plane.boundary_points)
                )

                plane = PlaneInfo(
                    center=np.array([plane.center.x, plane.center.y, plane.center.z]),
                    normal=np.array([plane.normal.x, plane.normal.y, plane.normal.z]),
                    size=np.array([plane.size.x, plane.size.y]),
                    boundary_points=np.array(boundary_points_2d),
                )

                boundary_3d = convert_2d_to_3d(
                    plane.boundary_points, plane.normal, plane.center
                )

                # Close the boundary by adding the first point to the end.
                boundary_3d = np.vstack([boundary_3d, boundary_3d[0]])
                strips.append(boundary_3d)
            self.recorder.log(
                f"world/detected-planes",
                rr.LineStrips3D(
                    strips=strips,
                    colors=[[255, 0, 0]],
                    radii=rr.Radius.ui_points(5.0),
                ),
            )

        if client_config.gyroscope.enabled:
            gyro_data_proto = request.gyroscope
            gyro_data = GyroscopeInfo(
                attitude=np.array(
                    [
                        gyro_data_proto.attitude.x,
                        gyro_data_proto.attitude.y,
                        gyro_data_proto.attitude.z,
                        gyro_data_proto.attitude.w,
                    ]
                ),
                rotation_rate=np.array(
                    [
                        gyro_data_proto.rotation_rate.x,
                        gyro_data_proto.rotation_rate.y,
                        gyro_data_proto.rotation_rate.z,
                    ]
                ),
                gravity=np.array(
                    [
                        gyro_data_proto.gravity.x,
                        gyro_data_proto.gravity.y,
                        gyro_data_proto.gravity.z,
                    ]
                ),
                acceleration=np.array(
                    [
                        gyro_data_proto.acceleration.x,
                        gyro_data_proto.acceleration.y,
                        gyro_data_proto.acceleration.z,
                    ]
                ),
            )
            attitude = rr.Quaternion(
                xyzw=[gyro_data.attitude],
            )
            rotation_rate = rr.datatypes.Vec3D(gyro_data.rotation_rate)
            gravity = rr.datatypes.Vec3D(gyro_data.gravity)
            acceleration = rr.datatypes.Vec3D(gyro_data.acceleration)
            # Attitute is displayed as a box, and the other acceleration variables are displayed as arrows.
            rr.log(
                "rotations/gyroscope/attitude",
                rr.Boxes3D(half_sizes=[0.5, 0.5, 0.5], quaternions=[attitude]),
            )
            rr.log(
                "rotations/gyroscope/rotation_rate",
                rr.Arrows3D(vectors=[rotation_rate], colors=[[0, 255, 0]]),
            )
            rr.log(
                "rotations/gyroscope/gravity",
                rr.Arrows3D(vectors=[gravity], colors=[[0, 0, 255]]),
            )
            rr.log(
                "rotations/gyroscope/acceleration",
                rr.Arrows3D(vectors=[acceleration], colors=[[255, 255, 0]]),
            )

        if client_config.audio.enabled:
            audio_data = np.array(request.audio_data)
            for i in audio_data:
                self.recorder.log("world/audio", rr.Scalar(i))

        if client_config.meshing.enabled:
            print("Number of meshes: ", len(request.meshes))
            # Binary arrays can be empty if no mesh is sent. This could be due to non-supporting devices. We can log this in the future.
            binary_arrays = request.meshes
            index = 0
            for mesh_data in binary_arrays:
                # We are ignoring type because DracoPy is written with Cython, and Pyright cannot infer types from a native module.
                dracoMesh = DracoPy.decode(mesh_data.data)  # pyright: ignore [reportUnknownMemberType, reportUnknownVariableType]

                mesh = Mesh(
                    dracoMesh.faces,  # pyright: ignore [reportUnknownMemberType, reportUnknownArgumentType]
                    dracoMesh.points,  # pyright: ignore [reportUnknownMemberType, reportUnknownArgumentType]
                    dracoMesh.normals,  # pyright: ignore [reportUnknownMemberType, reportUnknownArgumentType]
                    dracoMesh.tex_coord,  # pyright: ignore [reportUnknownMemberType, reportUnknownArgumentType]
                    dracoMesh.colors,  # pyright: ignore [reportUnknownMemberType, reportUnknownArgumentType]
                )

                rr.log(
                    f"world/mesh/mesh-{index}",
                    rr.Mesh3D(
                        vertex_positions=mesh.points,
                        triangle_indices=mesh.faces,
                        vertex_normals=mesh.normals,
                        vertex_colors=mesh.colors,
                        vertex_texcoords=mesh.tex_coord,
                    ),
                )
                index += 1

        # Call the for user extension code.
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

    def on_register(self, request: ClientConfiguration) -> None:
        """Called when a new device is registered. Override this method to process the data."""
        pass

    def on_frame_received(self, decoded_data_frame: DecodedDataFrame) -> None:
        """Called when a frame is received. Override this method to process the data."""
        pass  # pragma: no cover

    def on_program_exit(self, path_to_save: Path) -> None:
        """Save the data and exit.

        @private
        """
        logger.debug("Saving the data...")
        # Ensure the directory exists.
        os.makedirs(path_to_save, exist_ok=True)
        save_path = (
            path_to_save
            / f"frames_{time.strftime('%Y_%m_%d_%H_%M_%S', time.gmtime())}.pkl"
        )
        with open(save_path, "wb") as f:
            pickle.dump(self._requests_history, f)

        logger.info("Data saved to %s", save_path)


def convert_2d_to_3d(
    boundary_points_2d: PlaneBoundaryPoints, normal: PlaneNormal, center: PlaneCenter
) -> npt.NDArray[np.float32]:
    # Ensure the normal is normalized
    normal = normal / np.linalg.norm(normal)

    # Generate two orthogonal vectors (u and v) that lie on the plane
    # Find a vector that is not parallel to the normal
    arbitrary_vector = (
        np.array([1, 0, 0])
        if not np.allclose(normal, [1, 0, 0])
        else np.array([0, 1, 0])
    )

    # Create u vector, which is perpendicular to the normal
    u = np.cross(normal, arbitrary_vector)
    u = u / np.linalg.norm(u)

    # Create v vector, which is perpendicular to both the normal and u
    v = np.cross(normal, u)

    # Convert the 2D points into 3D
    # Each 2D point can be written as a linear combination of u and v, plus the center
    boundary_points_3d = np.array(
        [center + point_2d[0] * u + point_2d[1] * v for point_2d in boundary_points_2d]
    )

    return np.array(boundary_points_3d)


def _decode_rgb_image(
    resolution_y: int,
    resolution_x: int,
    resize_factor_y: float,
    resize_factor_x: float,
    data_type: Literal["RGB24", "YCbCr420"],
    buffer: bytes,
) -> ColorRGB:
    """Decode the color image from the buffer.

    Raises:
        ValueError: If the data type is not recognized
    """
    # Calculate the size of the image.
    color_img_w = int(resolution_x * resize_factor_x)
    color_img_h = int(resolution_y * resize_factor_y)
    p = color_img_w * color_img_h
    color_img = np.frombuffer(buffer, dtype=np.uint8)

    # Decode RGB bytes into RGB.
    if data_type == "RGB24":
        color_rgb = color_img.reshape((color_img_h, color_img_w, 3))

    # Decode YCbCr bytes into RGB.
    elif data_type == "YCbCr420":
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

    else:
        raise ValueError(f"Unknown data type: {data_type}")

    return color_rgb.astype(np.uint8)


def _decode_depth_image(
    resolution_y: int,
    resolution_x: int,
    data_type: Literal["f32", "u16"],
    buffer: bytes,
) -> DepthImg:
    """Decode the depth image from the buffer.

    Args:
        data_type: `f32` for iOS, `u16` for Android.

    Raises:
        ValueError: If the data type is not recognized.
    """
    # The `Any` means that the array can have any shape. We cannot
    # determine the shape of the array from the buffer.
    if data_type == "f32":
        dtype = np.float32
    elif data_type == "u16":
        dtype = np.uint16
    else:
        raise ValueError(f"Unknown data type: {data_type}")

    depth_img = np.frombuffer(buffer, dtype=dtype).reshape(
        (
            resolution_y,
            resolution_x,
        )
    )

    # If it's a 16-bit unsigned integer, convert to float32 and scale to meters.
    if dtype == np.uint16:
        depth_img = (depth_img.astype(np.float32) / 1000.0).astype(np.float32)

    return depth_img.astype(np.float32)


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
    transform = y_down_to_y_up @ transform

    return transform.astype(np.float32)


def _decode_intrinsic(
    resize_factor_y: float,
    resize_factor_x: float,
    focal_length_y: float,
    focal_length_x: float,
    principal_point_y: float,
    principal_point_x: float,
) -> Intrinsic:
    sx = resize_factor_x
    sy = resize_factor_y

    fx, fy = (
        focal_length_x * sx,
        focal_length_y * sy,
    )
    cx, cy = (
        principal_point_x * sx,
        principal_point_y * sy,
    )

    k = np.array([[fx, 0, cx], [0, fy, cy], [0, 0, 1]], dtype=np.float32)

    return k


def _decode_point_cloud(
    resolution_y: int,
    resolution_x: int,
    resize_factor_y: float,
    resize_factor_x: float,
    k: Intrinsic,
    color_rgb: ColorRGB,
    depth_img: DepthImg,
    transform: Transform,
) -> tuple[PointCloudPCD, PointCloudCLR]:
    # Flip image is needed for point cloud generation.
    color_rgb = np.flipud(color_rgb)
    depth_img = np.flipud(depth_img)

    color_img_w = int(resolution_x * resize_factor_x)
    color_img_h = int(resolution_y * resize_factor_y)

    u, v = np.meshgrid(np.arange(color_img_w), np.arange(color_img_h))

    fx: np.float32 = k[0, 0]
    fy: np.float32 = k[1, 1]
    cx: np.float32 = k[0, 2]
    cy: np.float32 = k[1, 2]

    z = depth_img.copy()
    x = ((u - cx) * z) / fx
    y = ((v - cy) * z) / fy
    pre_pcd = np.stack([x, y, z], axis=-1).reshape(-1, 3)
    pcd = np.matmul(transform[:3, :3], pre_pcd.T).T + transform[:3, 3]
    clr = color_rgb.reshape(-1, 3)

    return pcd.astype(np.float32), clr


def run_server(
    service: Type[ARFlowServicer], port: int = 8500, path_to_save: Path | None = None
) -> None:
    """Run gRPC server.

    Args:
        service: The service class to use. Custom servers should subclass `arflow.ARFlowServicer`.
        port: The port to listen on.
        path_to_save: The path to save data to.
    """
    servicer = service()
    interceptors = [ExceptionToStatusInterceptor(), ErrorLogger()]  # pyright: ignore [reportUnknownVariableType]
    server = grpc.server(  # pyright: ignore [reportUnknownMemberType]
        futures.ThreadPoolExecutor(max_workers=10),
        interceptors=interceptors,  # pyright: ignore [reportArgumentType]
        options=[
            ("grpc.max_send_message_length", -1),
            ("grpc.max_receive_message_length", -1),
        ],
    )
    service_pb2_grpc.add_ARFlowServicer_to_server(servicer, server)  # pyright: ignore [reportUnknownMemberType]
    server.add_insecure_port("[::]:%s" % port)
    server.start()
    logger.info("Server started, listening on %s", port)

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
        logger.debug("Shutting down gracefully")
        all_rpcs_done_event = server.stop(30)
        all_rpcs_done_event.wait(30)

        if path_to_save is not None:
            servicer.on_program_exit(path_to_save)

        # TODO: Discuss hook for user-defined cleanup procedures.

        logger.debug("Server shut down gracefully")

    signal(SIGTERM, handle_shutdown)
    signal(SIGINT, handle_shutdown)
    server.wait_for_termination()
