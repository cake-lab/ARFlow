from cakelab.arflow_grpc.v1 import device_pb2 as _device_pb2
from cakelab.arflow_grpc.v1 import session_pb2 as _session_pb2
from google.protobuf import descriptor as _descriptor
from google.protobuf import message as _message
from typing import ClassVar as _ClassVar, Mapping as _Mapping, Optional as _Optional, Union as _Union

DESCRIPTOR: _descriptor.FileDescriptor

class CreateSessionRequest(_message.Message):
    __slots__ = ("session_metadata", "device")
    SESSION_METADATA_FIELD_NUMBER: _ClassVar[int]
    DEVICE_FIELD_NUMBER: _ClassVar[int]
    session_metadata: _session_pb2.SessionMetadata
    device: _device_pb2.Device
    def __init__(self, session_metadata: _Optional[_Union[_session_pb2.SessionMetadata, _Mapping]] = ..., device: _Optional[_Union[_device_pb2.Device, _Mapping]] = ...) -> None: ...
