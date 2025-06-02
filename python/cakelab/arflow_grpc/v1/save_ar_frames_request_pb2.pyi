from cakelab.arflow_grpc.v1 import ar_frame_pb2 as _ar_frame_pb2
from cakelab.arflow_grpc.v1 import device_pb2 as _device_pb2
from cakelab.arflow_grpc.v1 import session_pb2 as _session_pb2
from google.protobuf.internal import containers as _containers
from google.protobuf import descriptor as _descriptor
from google.protobuf import message as _message
from typing import ClassVar as _ClassVar, Iterable as _Iterable, Mapping as _Mapping, Optional as _Optional, Union as _Union

DESCRIPTOR: _descriptor.FileDescriptor

class SaveARFramesRequest(_message.Message):
    __slots__ = ("session_id", "device", "frames")
    SESSION_ID_FIELD_NUMBER: _ClassVar[int]
    DEVICE_FIELD_NUMBER: _ClassVar[int]
    FRAMES_FIELD_NUMBER: _ClassVar[int]
    session_id: _session_pb2.SessionUuid
    device: _device_pb2.Device
    frames: _containers.RepeatedCompositeFieldContainer[_ar_frame_pb2.ARFrame]
    def __init__(self, session_id: _Optional[_Union[_session_pb2.SessionUuid, _Mapping]] = ..., device: _Optional[_Union[_device_pb2.Device, _Mapping]] = ..., frames: _Optional[_Iterable[_Union[_ar_frame_pb2.ARFrame, _Mapping]]] = ...) -> None: ...
