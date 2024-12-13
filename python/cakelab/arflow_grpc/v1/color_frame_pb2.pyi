from cakelab.arflow_grpc.v1 import intrinsics_pb2 as _intrinsics_pb2
from cakelab.arflow_grpc.v1 import xr_cpu_image_pb2 as _xr_cpu_image_pb2
from google.protobuf import timestamp_pb2 as _timestamp_pb2
from google.protobuf import descriptor as _descriptor
from google.protobuf import message as _message
from typing import ClassVar as _ClassVar, Mapping as _Mapping, Optional as _Optional, Union as _Union

DESCRIPTOR: _descriptor.FileDescriptor

class ColorFrame(_message.Message):
    __slots__ = ("device_timestamp", "image", "intrinsics")
    DEVICE_TIMESTAMP_FIELD_NUMBER: _ClassVar[int]
    IMAGE_FIELD_NUMBER: _ClassVar[int]
    INTRINSICS_FIELD_NUMBER: _ClassVar[int]
    device_timestamp: _timestamp_pb2.Timestamp
    image: _xr_cpu_image_pb2.XRCpuImage
    intrinsics: _intrinsics_pb2.Intrinsics
    def __init__(self, device_timestamp: _Optional[_Union[_timestamp_pb2.Timestamp, _Mapping]] = ..., image: _Optional[_Union[_xr_cpu_image_pb2.XRCpuImage, _Mapping]] = ..., intrinsics: _Optional[_Union[_intrinsics_pb2.Intrinsics, _Mapping]] = ...) -> None: ...
