"""Data exchanging service."""

import logging
import time
import uuid
from concurrent import futures
from pathlib import Path
from signal import SIGINT, SIGTERM, signal
from tempfile import gettempdir
from typing import Any, List, Type, cast

import DracoPy
import grpc
import numpy as np
import rerun as rr
from grpc_interceptor.exceptions import InvalidArgument, NotFound

from arflow._decoding import (
    convert_2d_to_3d_boundary_points,
    decode_depth_image,
    decode_intrinsic,
    decode_point_cloud,
    decode_rgb_image,
    decode_transform,
)
from arflow._error_interceptor import ErrorInterceptor
from arflow._types import (
    Audio,
    ClientInfo,
    ClientRegistry,
    ColorDataType,
    ColorRGB,
    DecodedDataFrame,
    DepthDataType,
    DepthImg,
    GyroscopeInfo,
    HashableClientIdentifier,
    Intrinsic,
    Mesh,
    PlaneBoundaryPoints3D,
    PlaneInfo,
    PointCloudCLR,
    PointCloudPCD,
    Transform,
)
from arflow_grpc import service_pb2_grpc
from arflow_grpc.service_pb2 import (
    JoinSessionRequest,
    JoinSessionResponse,
    ProcessFrameRequest,
    ProcessFrameResponse,
    RegisterClientRequest,
    RegisterClientResponse,
)

logger = logging.getLogger(__name__)


class ARFlowServicer(service_pb2_grpc.ARFlowServiceServicer):
    """Provides methods that implement the functionality of the ARFlow gRPC server."""

    def __init__(self, save_dir: Path | None = None, spawn_viewer: bool = True) -> None:
        """Initialize the ARFlowServicer.

        Args:
            save_dir: The path to save the data to. If None, defaults to a temporary directory located at `gettempdir()` in the `arflow` subdirectory.
            spawn_viewer: Whether to spawn the Rerun Viewer in another process.
        """
        self._client_registry: ClientRegistry = {}

        if save_dir is None:
            save_dir = Path(gettempdir()) / "arflow"
        self._save_dir = save_dir
        """The directory to save the data to."""
        self._save_dir.mkdir(parents=True, exist_ok=True)
        # Initializes SDK with an "empty" global recording. We don't want to log anything into the global recording.
        # We will create a new recording for each client.
        rr.init(application_id="arflow", spawn=spawn_viewer)
        super().__init__()

    def RegisterClient(
        self,
        request: RegisterClientRequest,
        context: grpc.ServicerContext | None = None,
        init_uid: str | None = None,
    ) -> RegisterClientResponse:
        """Register a client.

        @private
        """
        if init_uid is None:
            init_uid = uuid.uuid4().hex

        stream = rr.new_recording(
            application_id="arflow",
            recording_id=init_uid,
            # spawn=True,
        )
        self._client_registry[HashableClientIdentifier(init_uid)] = ClientInfo(
            config=request, rerun_stream=stream
        )
        logger.info("Registered a client with UUID: %s, Config: %s", init_uid, request)

        save_path = self._save_dir / Path(
            f"{request.device_name}_{time.strftime('%Y_%m_%d_%H_%M_%S', time.gmtime())}.rrd"
        )
        rr.save(
            path=save_path,
            recording=stream,
        )
        logger.info("Saving data of client %s to %s", init_uid, save_path)

        # Call the for user extension code.
        self.on_register(request)

        return RegisterClientResponse(uid=init_uid)

    def JoinSession(
        self, request: JoinSessionRequest, context: grpc.ServicerContext | None = None
    ) -> JoinSessionResponse:
        """Join a session.

        @private
        """
        logger.info("A client wants to join session %s", request.session_uid)
        try:
            session_info = self._client_registry[
                HashableClientIdentifier(request.session_uid)
            ]
        except KeyError:
            raise NotFound("Session not found")

        logger.debug("Found existing session %s", request.session_uid)
        client_uid = uuid.uuid4().hex
        self._client_registry[HashableClientIdentifier(client_uid)] = ClientInfo(
            config=request.client_config,
            # Share the same recording stream
            rerun_stream=session_info.rerun_stream,
        )
        logger.debug(
            "Registered a client with UUID: %s, Config: %s",
            client_uid,
            request.client_config,
        )

        # TODO: Test to see Rerun behavior when rr.save has already been called before
        # save_path = self._save_dir / Path(
        #     f"{request.client_config.device_name}_{time.strftime('%Y_%m_%d_%H_%M_%S', time.gmtime())}.rrd"
        # )
        # rr.save(
        #     path=save_path,
        #     recording=session_info.rerun_stream,
        # )

        # Call the for user extension code.
        self.on_join_session(request)

        return JoinSessionResponse(uid=client_uid)

    def ProcessFrame(
        self,
        request: ProcessFrameRequest,
        context: grpc.ServicerContext | None = None,
    ) -> ProcessFrameResponse:
        """Process an incoming frame.

        @private

        Raises:
            grpc_interceptor.exceptions.NotFound: If the client configuration is not found.
            grpc_interceptor.exceptions.InvalidArgument: If the color data type is not recognized
            or the depth data type is not recognized or if the request's data cannot be decoded (e.g., corrupted or invalid data).
        """
        try:
            client_info = self._client_registry[HashableClientIdentifier(request.uid)]
        except KeyError:
            raise NotFound("Client info not found")

        color_rgb: ColorRGB | None = None
        depth_img: DepthImg | None = None
        transform: Transform | None = None
        k: Intrinsic | None = None
        point_cloud_pcd: PointCloudPCD | None = None
        point_cloud_clr: PointCloudCLR | None = None
        audio_data: Audio | None = None

        # Used for all subsequent logging on the same thread until the next call to `rr.set_time_seconds()` (which
        # is the next `ProcessFrame()` call).
        rr.set_time_seconds(
            "stable_time",
            request.timestamp.seconds + request.timestamp.nanos / 1e9,
            recording=client_info.rerun_stream,
        )

        if client_info.config.camera_color.enabled:
            try:
                color_rgb = np.flipud(
                    decode_rgb_image(
                        client_info.config.camera_intrinsics.resolution_y,
                        client_info.config.camera_intrinsics.resolution_x,
                        client_info.config.camera_color.resize_factor_y,
                        client_info.config.camera_color.resize_factor_x,
                        cast(ColorDataType, client_info.config.camera_color.data_type),
                        request.color,
                    )
                )
            except ValueError as e:
                raise InvalidArgument(str(e))

            rr.log(
                f"arflow/{request.uid}/rgb",
                rr.Image(color_rgb),
                recording=client_info.rerun_stream,
            )

        if client_info.config.camera_depth.enabled:
            try:
                depth_img = np.flipud(
                    decode_depth_image(
                        client_info.config.camera_depth.resolution_y,
                        client_info.config.camera_depth.resolution_x,
                        cast(DepthDataType, client_info.config.camera_depth.data_type),
                        request.depth,
                    )
                )
            except ValueError as e:
                raise InvalidArgument(str(e))
            rr.log(
                f"arflow/{request.uid}/depth",
                rr.DepthImage(depth_img, meter=1.0),
                recording=client_info.rerun_stream,
            )

        if client_info.config.camera_transform.enabled:
            rr.log(
                f"arflow/{request.uid}/world/origin",
                rr.ViewCoordinates.RIGHT_HAND_Y_DOWN,
                recording=client_info.rerun_stream,
            )
            # self.logger.log(
            #     "world/xyz",
            #     rr.Arrows3D(
            #         vectors=[[1, 0, 0], [0, 1, 0], [0, 0, 1]],
            #         colors=[[255, 0, 0], [0, 255, 0], [0, 0, 255]],
            #     ),
            # )

            try:
                transform = decode_transform(request.transform)
            except ValueError as e:
                raise InvalidArgument(str(e))
            rr.log(
                f"arflow/{request.uid}/world/camera",
                rr.Transform3D(mat3x3=transform[:3, :3], translation=transform[:3, 3]),
                recording=client_info.rerun_stream,
            )

            # Won't thow any potential exceptions for now.
            k = decode_intrinsic(
                client_info.config.camera_color.resize_factor_y,
                client_info.config.camera_color.resize_factor_x,
                client_info.config.camera_intrinsics.focal_length_y,
                client_info.config.camera_intrinsics.focal_length_x,
                client_info.config.camera_intrinsics.principal_point_y,
                client_info.config.camera_intrinsics.principal_point_x,
            )

            rr.log(
                f"arflow/{request.uid}/world/camera",
                rr.Pinhole(image_from_camera=k),
                recording=client_info.rerun_stream,
            )
            if color_rgb is not None:
                rr.log(
                    f"arflow/{request.uid}/world/camera",
                    rr.Image(np.flipud(color_rgb)),
                    recording=client_info.rerun_stream,
                )

        if client_info.config.camera_point_cloud.enabled:
            if (
                k is not None
                and color_rgb is not None
                and depth_img is not None
                and transform is not None
            ):
                # Won't thow any potential exceptions for now.
                point_cloud_pcd, point_cloud_clr = decode_point_cloud(
                    client_info.config.camera_intrinsics.resolution_y,
                    client_info.config.camera_intrinsics.resolution_x,
                    client_info.config.camera_color.resize_factor_y,
                    client_info.config.camera_color.resize_factor_x,
                    k,
                    color_rgb,
                    depth_img,
                    transform,
                )
                rr.log(
                    f"arflow/{request.uid}/world/point_cloud",
                    rr.Points3D(point_cloud_pcd, colors=point_cloud_clr),
                    recording=client_info.rerun_stream,
                )

        if client_info.config.camera_plane_detection.enabled:
            strips: List[PlaneBoundaryPoints3D] = []
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

                try:
                    boundary_3d = convert_2d_to_3d_boundary_points(
                        plane.boundary_points, plane.normal, plane.center
                    )
                except ValueError as e:
                    raise InvalidArgument(str(e))

                # Close the boundary by adding the first point to the end.
                boundary_3d = np.vstack([boundary_3d, boundary_3d[0]])
                strips.append(boundary_3d)
            rr.log(
                f"arflow/{request.uid}/world/detected-planes",
                rr.LineStrips3D(
                    strips=strips,
                    colors=[[255, 0, 0]],
                    radii=rr.Radius.ui_points(5.0),
                ),
                recording=client_info.rerun_stream,
            )

        if client_info.config.gyroscope.enabled:
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
                xyzw=gyro_data.attitude,
            )
            rotation_rate = rr.datatypes.Vec3D(gyro_data.rotation_rate)
            gravity = rr.datatypes.Vec3D(gyro_data.gravity)
            acceleration = rr.datatypes.Vec3D(gyro_data.acceleration)
            # Attitute is displayed as a box, and the other acceleration variables are displayed as arrows.
            rr.log(
                f"arflow/{request.uid}/world/rotations/gyroscope/attitude",
                rr.Boxes3D(half_sizes=[0.5, 0.5, 0.5], quaternions=[attitude]),
                recording=client_info.rerun_stream,
            )
            rr.log(
                f"arflow/{request.uid}/world/rotations/gyroscope/rotation_rate",
                rr.Arrows3D(vectors=[rotation_rate], colors=[[0, 255, 0]]),
                recording=client_info.rerun_stream,
            )
            rr.log(
                f"arflow/{request.uid}/world/rotations/gyroscope/gravity",
                rr.Arrows3D(vectors=[gravity], colors=[[0, 0, 255]]),
                recording=client_info.rerun_stream,
            )
            rr.log(
                f"arflow/{request.uid}/world/rotations/gyroscope/acceleration",
                rr.Arrows3D(vectors=[acceleration], colors=[[255, 255, 0]]),
                recording=client_info.rerun_stream,
            )

        if client_info.config.audio.enabled:
            audio_data = np.array(request.audio_data)
            for i in audio_data:
                rr.log(
                    f"arflow/{request.uid}/world/audio",
                    rr.Scalar(i),
                    recording=client_info.rerun_stream,
                )

        if client_info.config.meshing.enabled:
            logger.debug("Number of meshes: %s", len(request.meshes))
            # Binary arrays can be empty if no mesh is sent. This could be due to non-supporting devices. We can log this in the future.
            binary_arrays = request.meshes
            for index, mesh_data in enumerate(binary_arrays):
                # We are ignoring type because DracoPy is written with Cython, and Pyright cannot infer types from a native module.
                dracoMesh = DracoPy.decode(mesh_data.data)  # pyright: ignore [reportUnknownMemberType, reportUnknownVariableType]

                mesh = Mesh(
                    faces=dracoMesh.faces,  # pyright: ignore [reportUnknownMemberType, reportUnknownArgumentType]
                    points=dracoMesh.points,  # pyright: ignore [reportUnknownMemberType, reportUnknownArgumentType]
                    normals=dracoMesh.normals,  # pyright: ignore [reportUnknownMemberType, reportUnknownArgumentType]
                    tex_coord=dracoMesh.tex_coord,  # pyright: ignore [reportUnknownMemberType, reportUnknownArgumentType]
                    colors=dracoMesh.colors,  # pyright: ignore [reportUnknownMemberType, reportUnknownArgumentType]
                )

                rr.log(
                    f"arflow/{request.uid}/world/mesh/mesh-{index}",
                    rr.Mesh3D(
                        vertex_positions=mesh.points,
                        triangle_indices=mesh.faces,
                        vertex_normals=mesh.normals,
                        vertex_colors=mesh.colors,
                        vertex_texcoords=mesh.tex_coord,
                    ),
                    recording=client_info.rerun_stream,
                )

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

        return ProcessFrameResponse(message="OK")

    def on_register(self, request: RegisterClientRequest) -> None:
        """Called when a new device is registered. Override this method to process the data."""
        pass

    def on_join_session(self, request: JoinSessionRequest) -> None:
        """Called when a new device joins a session. Override this method to process the data."""
        pass

    def on_frame_received(self, decoded_data_frame: DecodedDataFrame) -> None:
        """Called when a frame is received. Override this method to process the data."""
        pass  # pragma: no cover

    def on_program_exit(self) -> None:
        """Closes all TCP connections, servers, and files.

        @private
        """
        logger.info("Closing all TCP connections, servers, and files...")
        # Disconnects the global recording. Without this, this function will hang indefinitely.
        rr.disconnect()
        for client_id, client_info in self._client_registry.items():
            rr.disconnect(client_info.rerun_stream)
            logger.debug("Disconnected client %s", client_id)
        logger.info("All clients disconnected")


# TODO: Integration tests once more infrastructure work has been done (e.g., Docker). Remove pragma once implemented.
def run_server(  # pragma: no cover
    service: Type[ARFlowServicer],
    port: int = 8500,
    save_dir: Path | None = None,
    spawn_viewer: bool = True,
) -> None:
    """Run gRPC server.

    Args:
        service: The service class to use. Custom servers should subclass `arflow.ARFlowServicer`.
        port: The port to listen on.
        save_dir: The path to save the data to. If None, defaults to a temporary directory located at `gettempdir()` in the `arflow` subdirectory.
        spawn_viewer: Whether to spawn the Rerun Viewer in another process.
    """
    servicer = service(save_dir=save_dir, spawn_viewer=spawn_viewer)
    interceptors = [ErrorInterceptor()]  # pyright: ignore [reportUnknownVariableType]
    server = grpc.server(  # pyright: ignore [reportUnknownMemberType]
        futures.ThreadPoolExecutor(max_workers=10),
        interceptors=interceptors,  # pyright: ignore [reportArgumentType]
        options=[
            ("grpc.max_send_message_length", -1),
            ("grpc.max_receive_message_length", -1),
        ],
    )
    service_pb2_grpc.add_ARFlowServiceServicer_to_server(servicer, server)  # pyright: ignore [reportUnknownMemberType]
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
        logger.info("Shutting down gracefully")
        all_rpcs_done_event = server.stop(30)
        all_rpcs_done_event.wait(30)

        servicer.on_program_exit()

        # TODO: Discuss hook for user-defined cleanup procedures.

        logger.info("Server shut down gracefully")

    signal(SIGTERM, handle_shutdown)
    signal(SIGINT, handle_shutdown)
    server.wait_for_termination()
