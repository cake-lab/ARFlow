from cakelab.arflow_grpc.v1 import session_pb2 as _session_pb2
from google.protobuf import descriptor as _descriptor
from google.protobuf import message as _message
from typing import ClassVar as _ClassVar, Mapping as _Mapping, Optional as _Optional, Union as _Union

DESCRIPTOR: _descriptor.FileDescriptor

class GetSessionRequest(_message.Message):
    __slots__ = ("session_id",)
    SESSION_ID_FIELD_NUMBER: _ClassVar[int]
    session_id: _session_pb2.SessionUuid
    def __init__(self, session_id: _Optional[_Union[_session_pb2.SessionUuid, _Mapping]] = ...) -> None: ...