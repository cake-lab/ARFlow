"""The ARFlow gRPC server implementation."""

import logging
import uuid
from collections.abc import Sequence
from concurrent import futures
from pathlib import Path
from signal import SIGINT, SIGTERM, signal
from typing import Any, Type

import grpc
import rerun as rr
from grpc_interceptor.exceptions import InvalidArgument, NotFound

from arflow._error_interceptor import ErrorInterceptor
from arflow._session_stream import SessionStream
from arflow._types import (
    ARFrameType,
)
from cakelab.arflow_grpc.v1 import arflow_service_pb2_grpc
from cakelab.arflow_grpc.v1.ar_frame_pb2 import ARFrame
from cakelab.arflow_grpc.v1.audio_frame_pb2 import AudioFrame
from cakelab.arflow_grpc.v1.color_frame_pb2 import ColorFrame
from cakelab.arflow_grpc.v1.create_session_request_pb2 import CreateSessionRequest
from cakelab.arflow_grpc.v1.create_session_response_pb2 import CreateSessionResponse
from cakelab.arflow_grpc.v1.delete_session_request_pb2 import DeleteSessionRequest
from cakelab.arflow_grpc.v1.delete_session_response_pb2 import DeleteSessionResponse
from cakelab.arflow_grpc.v1.depth_frame_pb2 import DepthFrame
from cakelab.arflow_grpc.v1.device_pb2 import Device
from cakelab.arflow_grpc.v1.get_session_request_pb2 import GetSessionRequest
from cakelab.arflow_grpc.v1.get_session_response_pb2 import GetSessionResponse
from cakelab.arflow_grpc.v1.gyroscope_frame_pb2 import GyroscopeFrame
from cakelab.arflow_grpc.v1.join_session_request_pb2 import JoinSessionRequest
from cakelab.arflow_grpc.v1.join_session_response_pb2 import JoinSessionResponse
from cakelab.arflow_grpc.v1.leave_session_request_pb2 import LeaveSessionRequest
from cakelab.arflow_grpc.v1.leave_session_response_pb2 import LeaveSessionResponse
from cakelab.arflow_grpc.v1.list_sessions_request_pb2 import ListSessionsRequest
from cakelab.arflow_grpc.v1.list_sessions_response_pb2 import ListSessionsResponse
from cakelab.arflow_grpc.v1.mesh_detection_frame_pb2 import MeshDetectionFrame
from cakelab.arflow_grpc.v1.plane_detection_frame_pb2 import PlaneDetectionFrame
from cakelab.arflow_grpc.v1.point_cloud_detection_frame_pb2 import (
    PointCloudDetectionFrame,
)
from cakelab.arflow_grpc.v1.save_ar_frames_request_pb2 import (
    SaveARFramesRequest,
)
from cakelab.arflow_grpc.v1.save_ar_frames_response_pb2 import (
    SaveARFramesResponse,
)
from cakelab.arflow_grpc.v1.save_synchronized_ar_frame_request_pb2 import (
    SaveSynchronizedARFrameRequest,
)
from cakelab.arflow_grpc.v1.save_synchronized_ar_frame_response_pb2 import (
    SaveSynchronizedARFrameResponse,
)
from cakelab.arflow_grpc.v1.session_pb2 import Session, SessionUuid
from cakelab.arflow_grpc.v1.transform_frame_pb2 import TransformFrame

logger = logging.getLogger(__name__)


class ARFlowServicer(arflow_service_pb2_grpc.ARFlowServiceServicer):
    """Provides methods that implement the functionality of the ARFlow gRPC server."""

    def __init__(
        self,
        spawn_viewer: bool = True,
        save_dir: Path | None = None,
        application_id: str = "arflow",
    ) -> None:
        """Initialize the ARFlowServicer.

        Args:
            spawn_viewer: Whether to spawn the Rerun Viewer in another process.
            save_dir: The path to save the data to. Assumed to be an existing directory.
            application_id: The application ID to store recordings under.

        Raises:
            ValueError: If neither or both operational modes are selected.
        """
        if (spawn_viewer and save_dir is not None) or (
            not spawn_viewer and save_dir is None
        ):
            raise ValueError(
                "Either spawn the viewer or save the data, but not both, and neither can be disabled."
            )
        self.spawn_viewer = spawn_viewer
        self.save_dir = save_dir
        self.application_id = application_id
        self.client_sessions: dict[str, SessionStream] = {}
        """Active session streams, indexed by their ID."""
        # Initializes SDK with an "empty" global recording. We don't want to log anything into the global recording.
        rr.init(application_id=self.application_id, spawn=self.spawn_viewer)
        # TODO: This here is right? https://rerun.io/docs/concepts/spaces-and-transforms#view-coordinates
        # rr.log("/", rr.ViewCoordinates.RIGHT_HAND_Z_UP, static=True)
        super().__init__()

    def _get_session_stream(self, session_id: str) -> SessionStream:
        try:
            return self.client_sessions[session_id]
        except KeyError:
            raise NotFound("Session not found")

    def CreateSession(
        self, request: CreateSessionRequest, context: grpc.ServicerContext | None = None
    ) -> CreateSessionResponse:
        new_session_id = str(uuid.uuid4())
        new_rr_stream = rr.new_recording(
            application_id=self.application_id,
            recording_id=new_session_id,  # recording_id identifies streams
            spawn=self.spawn_viewer,
        )
        new_session = Session(
            id=SessionUuid(value=new_session_id),
            metadata=request.session_metadata,
            devices=[request.device],
        )
        new_session_stream = SessionStream(
            info=new_session,
            stream=new_rr_stream,
        )
        self.client_sessions[new_session_id] = new_session_stream
        logger.info("Created new session: %s", new_session_stream.info)

        if self.save_dir is not None:
            save_path = self.save_dir / Path(f"{new_session_stream.info.id.value}.rrd")

            # Overriding the save path if provided in session metadata
            if len(request.session_metadata.save_path) != 0:
                save_path = Path(request.session_metadata.save_path)

            rr.save(
                path=save_path,
                recording=new_rr_stream,
            )
            logger.info("Session data path: %s", save_path)

        self.on_create_session(
            session_stream=new_session_stream,
            device=request.device,
        )

        return CreateSessionResponse(session=new_session)

    def on_create_session(self, session_stream: SessionStream, device: Device) -> None:
        """Hook for user-defined procedures when a session is created.

        Args:
            session_stream: The session stream.
            device: The device that created the session.
        """
        pass

    def DeleteSession(
        self, request: DeleteSessionRequest, context: grpc.ServicerContext | None = None
    ) -> DeleteSessionResponse:
        try:
            session_stream = self.client_sessions.pop(request.session_id.value)
        except KeyError:
            raise NotFound("Session not found")

        rr.disconnect(session_stream.stream)
        logger.info("Deleted session: %s", session_stream.info)

        self.on_delete_session(session_stream=session_stream)

        return DeleteSessionResponse()

    def on_delete_session(
        self,
        session_stream: SessionStream,
    ) -> None:
        """Hook for user-defined procedures when a session is deleted.

        Args:
            session_stream: The deleted session stream.
        """
        pass

    def GetSession(
        self, request: GetSessionRequest, context: grpc.ServicerContext | None = None
    ) -> GetSessionResponse:
        session_stream = self._get_session_stream(request.session_id.value)

        logger.info("Retrieved session: %s", session_stream.info)

        return GetSessionResponse(session=session_stream.info)

    def ListSessions(
        self, request: ListSessionsRequest, context: grpc.ServicerContext | None = None
    ) -> ListSessionsResponse:
        current_sessions = [
            session_stream.info for session_stream in self.client_sessions.values()
        ]

        logger.info("Listed %s current sessions", len(current_sessions))

        return ListSessionsResponse(sessions=current_sessions)

    def JoinSession(
        self, request: JoinSessionRequest, context: grpc.ServicerContext | None = None
    ) -> JoinSessionResponse:
        """Join a session.

        @private
        """
        session_stream = self._get_session_stream(request.session_id.value)

        if request.device in session_stream.info.devices:
            raise InvalidArgument("Device already in session")

        session_stream.info.devices.append(request.device)

        logger.info("Client %s joined session %s", request.device, request.session_id)

        self.on_join_session(session_stream=session_stream, device=request.device)

        return JoinSessionResponse(session=session_stream.info)

    def on_join_session(
        self,
        session_stream: SessionStream,
        device: Device,
    ) -> None:
        """Hook for user-defined procedures when a new device joined a session.

        Args:
            session_stream: The session stream.
            device: The device that joined the session.
        """
        pass

    def LeaveSession(
        self, request: LeaveSessionRequest, context: grpc.ServicerContext | None = None
    ):
        """Leave a session.

        @private
        """
        session_stream = self._get_session_stream(request.session_id.value)

        try:
            session_stream.info.devices.remove(request.device)
        except ValueError:
            raise NotFound("Device not in session")

        logger.info(
            "Client %s left session %s", request.device, request.session_id.value
        )

        self.on_leave_session(
            session_stream=session_stream,
            device=request.device,
        )

        return LeaveSessionResponse()

    def on_leave_session(self, session_stream: SessionStream, device: Device) -> None:
        """Hook for user-defined procedures when a device leaves a session.

        Args:
            session_stream: The session stream.
            device: The device that left the session.
        """
        pass

    def SaveARFrames(
        self,
        request: SaveARFramesRequest,
        context: grpc.ServicerContext | None = None,
    ) -> SaveARFramesResponse:
        """Save AR frames to a session. Frames can be of different types and chronologically unordered."""
        if len(request.frames) == 0:
            raise InvalidArgument("No frames provided")

        session_stream = self._get_session_stream(request.session_id.value)

        if request.device not in session_stream.info.devices:
            raise NotFound("Device not in session")

        frames_grouped_by_type = {
            type: [
                frame for frame in request.frames if frame.WhichOneof("data") == type
            ]
            for type in ARFrameType
        }
        for frame_type, frames in frames_grouped_by_type.items():
            if len(frames) == 0:
                continue

            if frame_type == ARFrameType.TRANSFORM_FRAME and frames[0].transform_frame:
                self._process_transform_frames(
                    frames=[f.transform_frame for f in frames],
                    session_stream=session_stream,
                    device=request.device,
                )
            elif frame_type == ARFrameType.COLOR_FRAME and frames[0].color_frame:
                self._process_color_frames(
                    frames=[f.color_frame for f in frames],
                    session_stream=session_stream,
                    device=request.device,
                )
            elif frame_type == ARFrameType.DEPTH_FRAME and frames[0].depth_frame:
                self._process_depth_frames(
                    frames=[f.depth_frame for f in frames],
                    session_stream=session_stream,
                    device=request.device,
                )
            elif (
                frame_type == ARFrameType.GYROSCOPE_FRAME and frames[0].gyroscope_frame
            ):
                self._process_gyroscope_frames(
                    frames=[f.gyroscope_frame for f in frames],
                    session_stream=session_stream,
                    device=request.device,
                )
            elif frame_type == ARFrameType.AUDIO_FRAME and frames[0].audio_frame:
                self._process_audio_frames(
                    frames=[f.audio_frame for f in frames],
                    session_stream=session_stream,
                    device=request.device,
                )
            elif (
                frame_type == ARFrameType.PLANE_DETECTION_FRAME
                and frames[0].plane_detection_frame
            ):
                self._process_plane_detection_frames(
                    frames=[f.plane_detection_frame for f in frames],
                    session_stream=session_stream,
                    device=request.device,
                )
            elif (
                frame_type == ARFrameType.POINT_CLOUD_DETECTION_FRAME
                and frames[0].point_cloud_detection_frame
            ):
                self._process_point_cloud_detection_frames(
                    frames=[f.point_cloud_detection_frame for f in frames],
                    session_stream=session_stream,
                    device=request.device,
                )
            elif (
                frame_type == ARFrameType.MESH_DETECTION_FRAME
                and frames[0].mesh_detection_frame
            ):
                self._process_mesh_detection_frames(
                    frames=[f.mesh_detection_frame for f in frames],
                    session_stream=session_stream,
                    device=request.device,
                )

        logger.debug(
            "Saved AR frames of device %s to session %s",
            request.device,
            session_stream.info.id.value,
        )

        self.on_save_ar_frames(
            frames=request.frames,
            session_stream=session_stream,
            device=request.device,
        )

        return SaveARFramesResponse()

    def _process_transform_frames(
        self,
        frames: Sequence[TransformFrame],
        session_stream: SessionStream,
        device: Device,
    ) -> None:
        session_stream.save_transform_frames(
            frames=frames,
            device=device,
        )
        self.on_save_transform_frames(
            frames=frames,
            session_stream=session_stream,
            device=device,
        )

    def _process_color_frames(
        self,
        frames: Sequence[ColorFrame],
        session_stream: SessionStream,
        device: Device,
    ) -> None:
        session_stream.save_color_frames(
            frames=frames,
            device=device,
        )
        self.on_save_color_frames(
            frames=frames,
            session_stream=session_stream,
            device=device,
        )

    def _process_depth_frames(
        self,
        frames: Sequence[DepthFrame],
        session_stream: SessionStream,
        device: Device,
    ) -> None:
        session_stream.save_depth_frames(
            frames=frames,
            device=device,
        )
        self.on_save_depth_frames(
            frames=frames,
            session_stream=session_stream,
            device=device,
        )

    def _process_gyroscope_frames(
        self,
        frames: Sequence[GyroscopeFrame],
        session_stream: SessionStream,
        device: Device,
    ) -> None:
        session_stream.save_gyroscope_frames(
            frames=frames,
            device=device,
        )
        self.on_save_gyroscope_frames(
            frames=frames,
            session_stream=session_stream,
            device=device,
        )

    def _process_audio_frames(
        self,
        frames: Sequence[AudioFrame],
        session_stream: SessionStream,
        device: Device,
    ) -> None:
        session_stream.save_audio_frames(
            frames=frames,
            device=device,
        )
        self.on_save_audio_frames(
            frames=frames,
            session_stream=session_stream,
            device=device,
        )

    def _process_plane_detection_frames(
        self,
        frames: Sequence[PlaneDetectionFrame],
        session_stream: SessionStream,
        device: Device,
    ) -> None:
        session_stream.save_plane_detection_frames(
            frames=frames,
            device=device,
        )
        self.on_save_plane_detection_frames(
            frames=frames,
            session_stream=session_stream,
            device=device,
        )

    def _process_point_cloud_detection_frames(
        self,
        frames: Sequence[PointCloudDetectionFrame],
        session_stream: SessionStream,
        device: Device,
    ) -> None:
        session_stream.save_point_cloud_detection_frames(
            frames=frames,
            device=device,
        )
        self.on_save_point_cloud_detection_frames(
            frames=frames,
            session_stream=session_stream,
            device=device,
        )

    def _process_mesh_detection_frames(
        self,
        frames: Sequence[MeshDetectionFrame],
        session_stream: SessionStream,
        device: Device,
    ) -> None:
        session_stream.save_mesh_detection_frames(
            frames=frames,
            device=device,
        )
        self.on_save_mesh_detection_frames(
            frames=frames,
            session_stream=session_stream,
            device=device,
        )

    def on_save_ar_frames(
        self,
        frames: Sequence[ARFrame],
        session_stream: SessionStream,
        device: Device,
    ) -> None:
        """Hook for user-defined procedures when AR frames are saved to a recording stream.

        Args:
            frames: The AR frames.
            session_stream: The session stream.
            device: The device that sent the AR frames.
        """
        pass

    def on_save_transform_frames(
        self,
        frames: Sequence[TransformFrame],
        session_stream: SessionStream,
        device: Device,
    ) -> None:
        """Hook for user-defined procedures when transform frames are saved to a recording stream.

        Args:
            frames: The transform frames.
            session_stream: The session stream.
            device: The device that sent the AR frames.
        """
        pass

    def on_save_color_frames(
        self,
        frames: Sequence[ColorFrame],
        session_stream: SessionStream,
        device: Device,
    ) -> None:
        """Hook for user-defined procedures when color frames are saved to a recording stream. These frames are NOT homogenous in format or resolution.

        Args:
            frames: The color frames.
            session_stream: The session stream.
            device: The device that sent the AR frames.
        """
        pass

    def on_save_depth_frames(
        self,
        frames: Sequence[DepthFrame],
        session_stream: SessionStream,
        device: Device,
    ) -> None:
        """Hook for user-defined procedures when color frames are saved to a recording stream. These frames are NOT homogenous in format, resolution or smoothness.

        Args:
            frames: The depth frames.
            session_stream: The session stream.
            device: The device that sent the AR frames.
        """
        pass

    def on_save_gyroscope_frames(
        self,
        frames: Sequence[GyroscopeFrame],
        session_stream: SessionStream,
        device: Device,
    ) -> None:
        """Hook for user-defined procedures when gyroscope frames are saved to a recording stream.

        Args:
            frames: The gyroscope frames.
            session_stream: The session stream.
            device: The device that sent the AR frames.
        """
        pass

    def on_save_audio_frames(
        self,
        frames: Sequence[AudioFrame],
        session_stream: SessionStream,
        device: Device,
    ) -> None:
        """Hook for user-defined procedures when audio frames are saved to a recording stream.

        Args:
            frames: The audio frames.
            session_stream: The session stream.
            device: The device that sent the AR frames.
        """
        pass

    def on_save_plane_detection_frames(
        self,
        frames: Sequence[PlaneDetectionFrame],
        session_stream: SessionStream,
        device: Device,
    ) -> None:
        """Hook for user-defined procedures when plane detection frames are saved to a recording stream.

        Args:
            frames: The plane detection frames.
            session_stream: The session stream.
            device: The device that sent the AR frames.
        """
        pass

    def on_save_point_cloud_detection_frames(
        self,
        frames: Sequence[PointCloudDetectionFrame],
        session_stream: SessionStream,
        device: Device,
    ) -> None:
        """Hook for user-defined procedures when point cloud detection frames are saved to a recording stream.

        Args:
            frames: The point cloud detection frames.
            session_stream: The session stream.
            device: The device that sent the AR frames.
        """
        pass

    def on_save_mesh_detection_frames(
        self,
        frames: Sequence[MeshDetectionFrame],
        session_stream: SessionStream,
        device: Device,
    ) -> None:
        """Hook for user-defined procedures when mesh detection frames are saved to a recording stream.

        Args:
            frames: The mesh detection frames.
            session_stream: The session stream.
            device: The device that sent the AR frames.
        """
        pass

    def SaveSynchronizedARFrame(
        self,
        request: SaveSynchronizedARFrameRequest,
        context: grpc.ServicerContext | None = None,
    ) -> SaveSynchronizedARFrameResponse:
        session_stream = self._get_session_stream(request.session_id.value)

        if request.device not in session_stream.info.devices:
            raise NotFound("Device not in session")

        self._process_transform_frames(
            frames=[request.frame.transform_frame],
            session_stream=session_stream,
            device=request.device,
        )
        self._process_depth_frames(
            frames=[request.frame.depth_frame],
            session_stream=session_stream,
            device=request.device,
        )
        self._process_color_frames(
            frames=[request.frame.color_frame],
            session_stream=session_stream,
            device=request.device,
        )
        self._process_gyroscope_frames(
            frames=[request.frame.gyroscope_frame],
            session_stream=session_stream,
            device=request.device,
        )
        self._process_audio_frames(
            frames=[request.frame.audio_frame],
            session_stream=session_stream,
            device=request.device,
        )
        self._process_plane_detection_frames(
            frames=[request.frame.plane_detection_frame],
            session_stream=session_stream,
            device=request.device,
        )
        self._process_point_cloud_detection_frames(
            frames=[request.frame.point_cloud_detection_frame],
            session_stream=session_stream,
            device=request.device,
        )
        self._process_mesh_detection_frames(
            frames=[request.frame.mesh_detection_frame],
            session_stream=session_stream,
            device=request.device,
        )

        logger.info(
            "Saved synchronized AR frame of device %s to session %s",
            request.device,
            session_stream.info.id.value,
        )

        return SaveSynchronizedARFrameResponse()

    def on_server_exit(self) -> None:
        """Closes all TCP connections, servers, and files.

        @private
        """
        logger.debug("Closing all TCP connections, servers, and files...")
        # Disconnects the global recording. Without this, this function will hang indefinitely.
        rr.disconnect()
        for id, session in self.client_sessions.items():
            rr.disconnect(session.stream)
            logger.debug("Disconnected session: %s", id)
        logger.debug("All clients disconnected")


# TODO: Integration tests once more infrastructure work has been done (e.g., Docker). Remove pragma once implemented.
def run_server(  # pragma: no cover
    service: Type[ARFlowServicer],
    spawn_viewer: bool = True,
    save_dir: Path | None = None,
    application_id: str = "arflow",
    port: int = 8500,
) -> None:
    """Run gRPC server.

    Args:
        service: The service class to use. Custom servers should subclass `arflow.ARFlowServicer`.
        spawn_viewer: Whether to spawn the Rerun Viewer in another process.
        save_dir: The path to save the data to.
        port: The port to listen on.

    Raises:
        ValueError: If neither or both operational modes are selected.
    """
    try:
        servicer = service(
            spawn_viewer=spawn_viewer,
            save_dir=save_dir,
            application_id=application_id,
        )
    except ValueError as e:
        raise e
    interceptors = [ErrorInterceptor()]  # pyright: ignore [reportUnknownVariableType]
    server = grpc.server(  # pyright: ignore [reportUnknownMemberType]
        futures.ThreadPoolExecutor(max_workers=10),
        compression=grpc.Compression.Gzip,
        interceptors=interceptors,  # pyright: ignore [reportArgumentType]
        options=[
            # ("grpc.max_send_message_length", -1),
            ("grpc.max_receive_message_length", -1),
        ],
    )
    arflow_service_pb2_grpc.add_ARFlowServiceServicer_to_server(servicer, server)  # pyright: ignore [reportUnknownMemberType]
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

        servicer.on_server_exit()

        # TODO: Discuss hook for user-defined cleanup procedures.

        logger.info("Server shut down gracefully")

    signal(SIGTERM, handle_shutdown)
    signal(SIGINT, handle_shutdown)
    server.wait_for_termination()
