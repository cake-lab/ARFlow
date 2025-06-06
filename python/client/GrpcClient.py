import grpc
from typing import Awaitable, Iterable

from cakelab.arflow_grpc.v1.ar_frame_pb2 import ARFrame 

from cakelab.arflow_grpc.v1.arflow_service_pb2_grpc import ARFlowServiceStub
from cakelab.arflow_grpc.v1.session_pb2 import SessionUuid, SessionMetadata
from cakelab.arflow_grpc.v1.device_pb2 import Device

from cakelab.arflow_grpc.v1.create_session_request_pb2 import CreateSessionRequest
from cakelab.arflow_grpc.v1.create_session_response_pb2 import CreateSessionResponse
from cakelab.arflow_grpc.v1.delete_session_request_pb2 import DeleteSessionRequest
from cakelab.arflow_grpc.v1.delete_session_response_pb2 import DeleteSessionResponse
from cakelab.arflow_grpc.v1.get_session_request_pb2 import GetSessionRequest
from cakelab.arflow_grpc.v1.get_session_response_pb2 import GetSessionResponse
from cakelab.arflow_grpc.v1.join_session_request_pb2 import JoinSessionRequest
from cakelab.arflow_grpc.v1.join_session_response_pb2 import JoinSessionResponse
from cakelab.arflow_grpc.v1.list_sessions_request_pb2 import ListSessionsRequest
from cakelab.arflow_grpc.v1.list_sessions_response_pb2 import ListSessionsResponse
from cakelab.arflow_grpc.v1.save_ar_frames_request_pb2 import SaveARFramesRequest
from cakelab.arflow_grpc.v1.save_ar_frames_response_pb2 import SaveARFramesResponse

class GrpcClient:
    def __init__(self, url):
        self.channel = grpc.insecure_channel(url)
    async def CreateSessionAsync(self, name: str, device: Device, save_path: str = "") -> CreateSessionResponse:
        request = CreateSessionRequest(
            session_metadata=SessionMetadata(name=name, save_path=save_path),
            device=device
        )
        response: Awaitable[CreateSessionResponse] = ARFlowServiceStub(self.channel).CreateSession(request)
        return response
    async def DeleteSessionAsync(self, session_id: str) -> DeleteSessionResponse:
        request = DeleteSessionRequest(
            session_id=SessionUuid(value = session_id)
        )
        response: Awaitable[DeleteSessionResponse] = ARFlowServiceStub(self.channel).DeleteSession(request)
        return response
    async def GetSessionAsync(self, session_id: str) -> GetSessionResponse:
        request = GetSessionRequest(
            session_id=SessionUuid(value=session_id)
        )
        response: Awaitable[GetSessionResponse] = ARFlowServiceStub(self.channel).GetSession(request)
        return response
    async def JoinSessionAsync(self, session_id: str, device: Device) -> JoinSessionResponse:
        request = JoinSessionRequest(
            session_id=SessionUuid(value=session_id),
            device=device
        )
        response: Awaitable[JoinSessionResponse] = ARFlowServiceStub(self.channel).JoinSession(request)
        return response
    async def ListSessionsAsync(self) -> ListSessionsResponse:
        request = ListSessionsRequest()
        response: Awaitable[ListSessionsResponse] = ARFlowServiceStub(self.channel).ListSessions(request)
        return response
    async def SaveARFramesAsync(self, session_id: str, ar_frames: Iterable[ARFrame], device: Device) -> SaveARFramesResponse:
        request = SaveARFramesRequest(
            session_id=SessionUuid(value=session_id),
            frames=ar_frames,
            device=device
        )
        response: Awaitable[SaveARFramesResponse] = ARFlowServiceStub(self.channel).SaveARFrames(request)
        return response

    def close(self):
        self.channel.close()
    