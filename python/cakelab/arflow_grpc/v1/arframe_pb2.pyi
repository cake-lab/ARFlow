from cakelab.arflow_grpc.v1 import camera_frame_pb2 as _camera_frame_pb2
from google.protobuf import descriptor as _descriptor
from google.protobuf import message as _message
from typing import ClassVar as _ClassVar, Mapping as _Mapping, Optional as _Optional, Union as _Union

DESCRIPTOR: _descriptor.FileDescriptor

class ARFrame(_message.Message):
    __slots__ = ("camera_frame",)
    CAMERA_FRAME_FIELD_NUMBER: _ClassVar[int]
    camera_frame: _camera_frame_pb2.CameraFrame
    def __init__(self, camera_frame: _Optional[_Union[_camera_frame_pb2.CameraFrame, _Mapping]] = ...) -> None: ...
