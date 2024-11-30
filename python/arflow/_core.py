"""The ARFlow gRPC server implementation."""

import logging
import uuid
from collections import defaultdict
from concurrent import futures
from pathlib import Path
from signal import SIGINT, SIGTERM, signal
from typing import Any, DefaultDict, Iterable, Tuple, Type

import grpc
import numpy as np
import rerun as rr
from grpc_interceptor.exceptions import InvalidArgument, NotFound

from arflow._decoding import decode_color_frames, decode_depth_frames
from arflow._error_interceptor import ErrorInterceptor
from arflow._session_stream import SessionStream
from arflow._types import (
    ARFrameType,
    DecodedARFrames,
    DecodedColorFrames,
    DecodedDepthFrames,
)
from cakelab.arflow_grpc.v1 import arflow_service_pb2_grpc
from cakelab.arflow_grpc.v1.ar_frame_pb2 import ARFrame
from cakelab.arflow_grpc.v1.color_frame_pb2 import ColorFrame
from cakelab.arflow_grpc.v1.create_session_request_pb2 import CreateSessionRequest
from cakelab.arflow_grpc.v1.create_session_response_pb2 import CreateSessionResponse
from cakelab.arflow_grpc.v1.delete_session_request_pb2 import DeleteSessionRequest
from cakelab.arflow_grpc.v1.delete_session_response_pb2 import DeleteSessionResponse
from cakelab.arflow_grpc.v1.depth_frame_pb2 import DepthFrame
from cakelab.arflow_grpc.v1.device_pb2 import Device
from cakelab.arflow_grpc.v1.get_session_request_pb2 import GetSessionRequest
from cakelab.arflow_grpc.v1.get_session_response_pb2 import GetSessionResponse
from cakelab.arflow_grpc.v1.join_session_request_pb2 import JoinSessionRequest
from cakelab.arflow_grpc.v1.join_session_response_pb2 import JoinSessionResponse
from cakelab.arflow_grpc.v1.leave_session_request_pb2 import LeaveSessionRequest
from cakelab.arflow_grpc.v1.leave_session_response_pb2 import LeaveSessionResponse
from cakelab.arflow_grpc.v1.list_sessions_request_pb2 import ListSessionsRequest
from cakelab.arflow_grpc.v1.list_sessions_response_pb2 import ListSessionsResponse
from cakelab.arflow_grpc.v1.save_ar_frames_request_pb2 import (
    SaveARFramesRequest,
)
from cakelab.arflow_grpc.v1.save_ar_frames_response_pb2 import (
    SaveARFramesResponse,
)
from cakelab.arflow_grpc.v1.session_pb2 import Session, SessionUuid
from cakelab.arflow_grpc.v1.xr_cpu_image_pb2 import XRCpuImage

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

        Raises:
            ValueError: If neither or both operational modes are selected.
        """
        if (spawn_viewer and save_dir is not None) or (
            not spawn_viewer and save_dir is None
        ):
            raise ValueError(
                "Either spawn the viewer or save the data, but not both, and neither can be disabled."
            )
        self._spawn_viewer = spawn_viewer
        self._save_dir = save_dir
        self._application_id = application_id
        self._client_sessions: dict[str, SessionStream] = {}
        # Initializes SDK with an "empty" global recording. We don't want to log anything into the global recording.
        rr.init(application_id=application_id, spawn=self._spawn_viewer)
        super().__init__()

    def CreateSession(
        self, request: CreateSessionRequest, context: grpc.ServicerContext | None = None
    ) -> CreateSessionResponse:
        new_session_id = str(uuid.uuid4())
        new_rr_stream = rr.new_recording(
            application_id=self._application_id,
            recording_id=new_session_id,  # recording_id identifies streams
            spawn=self._spawn_viewer,
        )
        new_session = Session(
            id=SessionUuid(new_session_id),
            metadata=request.session_metadata,
            devices=[request.device],
        )
        new_session_stream = SessionStream(
            info=new_session,
            stream=new_rr_stream,
        )
        self._client_sessions[new_session_id] = new_session_stream
        logger.info("Created new session: %s", new_session_stream.info)

        if self._save_dir is not None:
            save_path = self._save_dir / Path(f"{new_session_stream.info.id}.rrd")

            # Overriding the save path if provided in session metadata
            if len(request.session_metadata.save_path) != 0:
                save_path = Path(request.session_metadata.save_path)

            rr.save(
                path=save_path,
                recording=new_rr_stream,
            )
            logger.debug("Session data path: %s", save_path)

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
            session_stream = self._client_sessions.pop(request.session_id.value)
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

        self.on_get_session(session_stream=session_stream)

        return GetSessionResponse(session=session_stream.info)

    def on_get_session(self, session_stream: SessionStream) -> None:
        """Hook for user-defined procedures when a session is retrieved.

        Args:
            session_stream: The session stream.
        """
        pass

    def ListSessions(
        self, request: ListSessionsRequest, context: grpc.ServicerContext | None = None
    ) -> ListSessionsResponse:
        current_session_streams = [
            session_stream for session_stream in self._client_sessions.values()
        ]
        current_sessions = [
            session_stream.info for session_stream in current_session_streams
        ]

        logger.info("Listed current sessions: %s", current_session_streams)

        self.on_list_sessions(session_streams=current_session_streams)

        return ListSessionsResponse(sessions=current_sessions)

    def on_list_sessions(self, session_streams: Iterable[SessionStream]) -> None:
        """Hook for user-defined procedures when all sessions are requested.

        Args:
            session_streams: The session streams.
        """
        pass

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

        # TODO: Test to see Rerun behavior when rr.save has already been called before
        # save_path = self._save_dir / Path(
        #     f"{request.client_config.device_name}_{time.strftime('%Y_%m_%d_%H_%M_%S', time.gmtime())}.rrd"
        # )
        # rr.save(
        #     path=save_path,
        #     recording=session_info.rerun_stream,
        # )

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
            raise InvalidArgument("Device not in session")

        logger.info("Client %s left session %s", request.device, request.session_id)

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
            raise InvalidArgument("Device not in session")

        frames_grouped_by_type = self._group_frames_by_type(frames=request.frames)
        decoded_ar_frames = self._process_frames(
            frames_grouped_by_type=frames_grouped_by_type,
            session_stream=session_stream,
            device=request.device,
        )

        logger.info(
            "Saved AR frames of device %s to session %s",
            request.device,
            session_stream.info.id.value,
        )

        self.on_save_ar_frames(
            frames=decoded_ar_frames,
            session_stream=session_stream,
            device=request.device,
        )

        return SaveARFramesResponse()

    def on_save_ar_frames(
        self,
        frames: DecodedARFrames,
        session_stream: SessionStream,
        device: Device,
    ) -> None:
        """Hook for user-defined procedures when AR frames are saved to a recording stream.

        Args:
            frames: The decoded AR frames.
            session_stream: The session stream.
            device: The device that sent the AR frames.
        """
        pass

    def on_save_color_frames(
        self,
        frames: DecodedColorFrames,
        format: XRCpuImage.Format,
        width: int,
        height: int,
        session_stream: SessionStream,
        device: Device,
    ) -> None:
        """Hook for user-defined procedures when color frames are saved to a recording stream. These frames are homogenous in format and resolution.

        Args:
            frames: The decoded color frames.
            session_stream: The session stream.
            device: The device that sent the AR frames.
        """
        pass

    def on_save_depth_frames(
        self,
        frames: DecodedDepthFrames,
        format: XRCpuImage.Format,
        width: int,
        height: int,
        session_stream: SessionStream,
        device: Device,
    ) -> None:
        """Hook for user-defined procedures when color frames are saved to a recording stream. These frames are homogenous in format and resolution.

        Args:
            frames: The decoded color frames.
            session_stream: The session stream.
            device: The device that sent the AR frames.
        """
        pass

    def on_program_exit(self) -> None:
        """Closes all TCP connections, servers, and files.

        @private
        """
        logger.debug("Closing all TCP connections, servers, and files...")
        # Disconnects the global recording. Without this, this function will hang indefinitely.
        rr.disconnect()
        for id, session in self._client_sessions.items():
            rr.disconnect(session.stream)
            logger.debug("Disconnected session: %s", id)
        logger.debug("All clients disconnected")

    def _get_session_stream(self, session_id: str) -> SessionStream:
        try:
            return self._client_sessions[session_id]
        except KeyError:
            raise NotFound("Session not found")

    def _group_frames_by_type(
        self, frames: Iterable[ARFrame]
    ) -> DefaultDict[ARFrameType, list[ARFrame]]:
        frames_grouped_by_type: DefaultDict[ARFrameType, list[ARFrame]] = defaultdict(
            list
        )
        for frame in frames:
            frame_type: str | None = frame.WhichOneof("data")
            logger.debug("Valid frame types: %s", ARFrameType._value2member_map_)
            if frame_type is not None and frame_type in ARFrameType._value2member_map_:
                frames_grouped_by_type[ARFrameType(frame_type)].append(frame)
        logger.debug("Frames grouped by data type: %s", frames_grouped_by_type)
        return frames_grouped_by_type

    def _process_frames(
        self,
        frames_grouped_by_type: DefaultDict[ARFrameType, list[ARFrame]],
        session_stream: SessionStream,
        device: Device,
    ) -> DecodedARFrames:
        decoded_ar_frames = np.array([])
        for frame_type, frames in frames_grouped_by_type.items():
            if frame_type == ARFrameType.COLOR_FRAME and frames[0].color_frame:
                decoded_ar_frames = self._process_color_frames(
                    frames=frames, session_stream=session_stream, device=device
                )
            elif frame_type == ARFrameType.DEPTH_FRAME and frames[0].depth_frame:
                decoded_ar_frames = self._process_depth_frames(
                    frames=frames, session_stream=session_stream, device=device
                )
        return decoded_ar_frames

    def _process_color_frames(
        self,
        frames: list[ARFrame],
        session_stream: SessionStream,
        device: Device,
    ) -> DecodedARFrames:
        color_frames_grouped_by_format_and_dims: DefaultDict[
            Tuple[XRCpuImage.Format, int, int], list[ColorFrame]
        ] = defaultdict(list)
        for frame in frames:
            color_frames_grouped_by_format_and_dims[
                (
                    frame.color_frame.image.format,
                    frame.color_frame.image.dimensions.x,
                    frame.color_frame.image.dimensions.y,
                )
            ].append(frame.color_frame)
        decoded_color_frames = np.array([])
        for (
            color_frame_format,
            width,
            height,
        ), homogenous_color_frames in color_frames_grouped_by_format_and_dims.items():
            try:
                decoded_color_frames = decode_color_frames(
                    raw_planes=[f.image.planes for f in homogenous_color_frames],
                    format=color_frame_format,
                )
            except ValueError as e:
                logger.warning("Error decoding color frames: %s", e)
                continue

            try:
                session_stream.save_color_frames(
                    frames=decoded_color_frames,
                    device_timestamps=[
                        f.device_timestamp.seconds + f.device_timestamp.nanos / 1e9
                        for f in homogenous_color_frames
                    ],
                    image_timestamps=[
                        f.image.timestamp for f in homogenous_color_frames
                    ],
                    device=device,
                    format=color_frame_format,
                    width=width,
                    height=height,
                )
            except ValueError as e:
                logger.warning("Error saving color frames: %s", e)
                continue

            self.on_save_color_frames(
                frames=decoded_color_frames,
                format=color_frame_format,
                width=width,
                height=height,
                session_stream=session_stream,
                device=device,
            )

        return decoded_color_frames

    def _process_depth_frames(
        self,
        frames: list[ARFrame],
        session_stream: SessionStream,
        device: Device,
    ) -> DecodedARFrames:
        depth_frames_grouped_by_format_and_dims: DefaultDict[
            Tuple[XRCpuImage.Format, int, int], list[DepthFrame]
        ] = defaultdict(list)
        for frame in frames:
            depth_frames_grouped_by_format_and_dims[
                (
                    frame.depth_frame.image.format,
                    frame.depth_frame.image.dimensions.x,
                    frame.depth_frame.image.dimensions.y,
                )
            ].append(frame.depth_frame)
        decoded_depth_frames = np.array([])
        for (
            depth_frame_format,
            width,
            height,
        ), homogenous_depth_frames in depth_frames_grouped_by_format_and_dims.items():
            try:
                decoded_depth_frames = decode_depth_frames(
                    raw_planes=[f.image.planes for f in homogenous_depth_frames],
                    format=depth_frame_format,
                    width=width,
                    height=height,
                )
            except ValueError as e:
                logger.warning("Error decoding depth frames: %s", e)
                continue

            try:
                session_stream.save_depth_frames(
                    frames=decoded_depth_frames,
                    device_timestamps=[
                        f.device_timestamp.seconds + f.device_timestamp.nanos / 1e9
                        for f in homogenous_depth_frames
                    ],
                    image_timestamps=[
                        f.image.timestamp for f in homogenous_depth_frames
                    ],
                    device=device,
                    format=depth_frame_format,
                    width=width,
                    height=height,
                )
            except ValueError as e:
                logger.warning("Error saving depth frames: %s", e)
                continue

            self.on_save_depth_frames(
                frames=decoded_depth_frames,
                format=depth_frame_format,
                width=width,
                height=height,
                session_stream=session_stream,
                device=device,
            )

        return decoded_depth_frames


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

        servicer.on_program_exit()

        # TODO: Discuss hook for user-defined cleanup procedures.

        logger.info("Server shut down gracefully")

    signal(SIGTERM, handle_shutdown)
    signal(SIGINT, handle_shutdown)
    server.wait_for_termination()
