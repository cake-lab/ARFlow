from cakelab.arflow_grpc.v1 import xr_cpu_image_pb2 as _xr_cpu_image_pb2
from google.protobuf import timestamp_pb2 as _timestamp_pb2
from google.protobuf import descriptor as _descriptor
from google.protobuf import message as _message
from typing import ClassVar as _ClassVar, Mapping as _Mapping, Optional as _Optional, Union as _Union

DESCRIPTOR: _descriptor.FileDescriptor

class DepthFrame(_message.Message):
    __slots__ = ("device_timestamp", "environment_depth_temporal_smoothing_enabled", "image")
    DEVICE_TIMESTAMP_FIELD_NUMBER: _ClassVar[int]
    ENVIRONMENT_DEPTH_TEMPORAL_SMOOTHING_ENABLED_FIELD_NUMBER: _ClassVar[int]
    IMAGE_FIELD_NUMBER: _ClassVar[int]
    device_timestamp: _timestamp_pb2.Timestamp
    environment_depth_temporal_smoothing_enabled: bool
    image: _xr_cpu_image_pb2.XRCpuImage
    def __init__(self, device_timestamp: _Optional[_Union[_timestamp_pb2.Timestamp, _Mapping]] = ..., environment_depth_temporal_smoothing_enabled: bool = ..., image: _Optional[_Union[_xr_cpu_image_pb2.XRCpuImage, _Mapping]] = ...) -> None: ...
