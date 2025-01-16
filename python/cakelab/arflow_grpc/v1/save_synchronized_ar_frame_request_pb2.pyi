from cakelab.arflow_grpc.v1 import device_pb2 as _device_pb2
from cakelab.arflow_grpc.v1 import session_pb2 as _session_pb2
from cakelab.arflow_grpc.v1 import synchronized_ar_frame_pb2 as _synchronized_ar_frame_pb2
from google.protobuf import descriptor as _descriptor
from google.protobuf import message as _message
from typing import ClassVar as _ClassVar, Mapping as _Mapping, Optional as _Optional, Union as _Union

DESCRIPTOR: _descriptor.FileDescriptor

class SaveSynchronizedARFrameRequest(_message.Message):
    __slots__ = ("session_id", "device", "frame")
    SESSION_ID_FIELD_NUMBER: _ClassVar[int]
    DEVICE_FIELD_NUMBER: _ClassVar[int]
    FRAME_FIELD_NUMBER: _ClassVar[int]
    session_id: _session_pb2.SessionUuid
    device: _device_pb2.Device
    frame: _synchronized_ar_frame_pb2.SynchronizedARFrame
    def __init__(self, session_id: _Optional[_Union[_session_pb2.SessionUuid, _Mapping]] = ..., device: _Optional[_Union[_device_pb2.Device, _Mapping]] = ..., frame: _Optional[_Union[_synchronized_ar_frame_pb2.SynchronizedARFrame, _Mapping]] = ...) -> None: ...
