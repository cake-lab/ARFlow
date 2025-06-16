"""A simple Python gRPC client."""

# ruff:noqa: D103
# pyright: reportUnknownMemberType=false, reportUnknownVariableType=false, reportUnknownArgumentType=false
# We have to do the above because the grpc stub has no type hints

from typing import Awaitable, Iterable

import grpc

from cakelab.arflow_grpc.v1.ar_frame_pb2 import ARFrame
from cakelab.arflow_grpc.v1.arflow_service_pb2_grpc import ARFlowServiceStub
from cakelab.arflow_grpc.v1.create_session_request_pb2 import CreateSessionRequest
from cakelab.arflow_grpc.v1.create_session_response_pb2 import CreateSessionResponse
from cakelab.arflow_grpc.v1.delete_session_request_pb2 import DeleteSessionRequest
from cakelab.arflow_grpc.v1.delete_session_response_pb2 import DeleteSessionResponse
from cakelab.arflow_grpc.v1.device_pb2 import Device
from cakelab.arflow_grpc.v1.get_session_request_pb2 import GetSessionRequest
from cakelab.arflow_grpc.v1.get_session_response_pb2 import GetSessionResponse
from cakelab.arflow_grpc.v1.join_session_request_pb2 import JoinSessionRequest
from cakelab.arflow_grpc.v1.join_session_response_pb2 import JoinSessionResponse
from cakelab.arflow_grpc.v1.leave_session_request_pb2 import LeaveSessionRequest
from cakelab.arflow_grpc.v1.leave_session_response_pb2 import LeaveSessionResponse
from cakelab.arflow_grpc.v1.list_sessions_request_pb2 import ListSessionsRequest
from cakelab.arflow_grpc.v1.list_sessions_response_pb2 import ListSessionsResponse
from cakelab.arflow_grpc.v1.save_ar_frames_request_pb2 import SaveARFramesRequest
from cakelab.arflow_grpc.v1.save_ar_frames_response_pb2 import SaveARFramesResponse
from cakelab.arflow_grpc.v1.session_pb2 import SessionMetadata, SessionUuid


class GrpcClient:
    """A simple gRPC client class."""

    stub: ARFlowServiceStub

    def __init__(self, url: str):
        """Initialize the gRPC client.

        Args:
            url: The URL of the gRPC server.
        """
        self.channel = grpc.insecure_channel(url)
        self.stub = ARFlowServiceStub(self.channel)

    async def create_session_async(
        self, name: str, device: Device, save_path: str = ""
    ) -> Awaitable[CreateSessionResponse]:
        """Create a new session.

        Args:
            name: The name of the session.
            device: The device to use for the session.
            save_path: The path to save the session data to.

        Returns:
                The response from the server.
        """
        request = CreateSessionRequest(
            session_metadata=SessionMetadata(name=name, save_path=save_path),
            device=device,
        )
        response: Awaitable[CreateSessionResponse] = self.stub.CreateSession(request)
        return response

    async def delete_session_async(
        self, session_id: str
    ) -> Awaitable[DeleteSessionResponse]:
        """Delete a session.

        Args:
            session_id: The session id to delete.

        Returns:
                The response from the server.
        """
        request = DeleteSessionRequest(session_id=SessionUuid(value=session_id))
        response: Awaitable[DeleteSessionResponse] = self.stub.DeleteSession(request)
        return response

    async def get_session_async(self, session_id: str) -> Awaitable[GetSessionResponse]:
        """Get a session by its ID.

        Args:
            session_id: The session id to get.

        Returns:
            The response from the server.
        """
        request = GetSessionRequest(session_id=SessionUuid(value=session_id))
        response: Awaitable[GetSessionResponse] = self.stub.GetSession(request)
        return response

    async def join_session_async(
        self, session_id: str, device: Device
    ) -> Awaitable[JoinSessionResponse]:
        """Join a session.

        Args:
            session_id: The session id to join.
            device: The device to join the session with.

        Returns:
            The response from the server.
        """
        request = JoinSessionRequest(
            session_id=SessionUuid(value=session_id), device=device
        )
        response: Awaitable[JoinSessionResponse] = self.stub.JoinSession(request)
        return response

    async def list_sessions_async(self) -> Awaitable[ListSessionsResponse]:
        """List all sessions."""
        request = ListSessionsRequest()
        response: Awaitable[ListSessionsResponse] = self.stub.ListSessions(request)
        return response

    async def leave_session_async(
        self, session_id: str, device: Device
    ) -> Awaitable[LeaveSessionResponse]:
        """Leave a session.

        Args:
            session_id: The session ID.
            device: The device that left the session.

        Returns:
            The response from the server.
        """
        request = LeaveSessionRequest(
            session_id=SessionUuid(value=session_id), device=device
        )
        response: Awaitable[LeaveSessionResponse] = self.stub.LeaveSession(request)
        return response

    async def save_ar_frames_async(
        self, session_id: str, ar_frames: Iterable[ARFrame], device: Device
    ) -> Awaitable[SaveARFramesResponse]:
        """Save AR frames to the session.

        Args:
            session_id: The session ID.
            ar_frames: The AR frames to save.
            device: The device that captured the AR frames.

        Returns:
            The response from the server.
        """
        request = SaveARFramesRequest(
            session_id=SessionUuid(value=session_id), frames=ar_frames, device=device
        )
        response: Awaitable[SaveARFramesResponse] = self.stub.SaveARFrames(request)
        return response

    def close(self):
        """Close the channel."""
        self.channel.close()
