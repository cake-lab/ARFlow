from cakelab.arflow_grpc.v1 import vector2_pb2 as _vector2_pb2
from cakelab.arflow_grpc.v1 import vector2_int_pb2 as _vector2_int_pb2
from google.protobuf import timestamp_pb2 as _timestamp_pb2
from google.protobuf.internal import enum_type_wrapper as _enum_type_wrapper
from google.protobuf import descriptor as _descriptor
from google.protobuf import message as _message
from typing import ClassVar as _ClassVar, Mapping as _Mapping, Optional as _Optional, Union as _Union

DESCRIPTOR: _descriptor.FileDescriptor

class CameraFrame(_message.Message):
    __slots__ = ("device_timestamp", "image_timestamp", "format", "intrinsics", "data")
    class Format(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
        __slots__ = ()
        FORMAT_UNSPECIFIED: _ClassVar[CameraFrame.Format]
        FORMAT_RGBA32: _ClassVar[CameraFrame.Format]
        FORMAT_RGB24: _ClassVar[CameraFrame.Format]
    FORMAT_UNSPECIFIED: CameraFrame.Format
    FORMAT_RGBA32: CameraFrame.Format
    FORMAT_RGB24: CameraFrame.Format
    class Intrinsics(_message.Message):
        __slots__ = ("focal_length", "principal_point", "resolution")
        FOCAL_LENGTH_FIELD_NUMBER: _ClassVar[int]
        PRINCIPAL_POINT_FIELD_NUMBER: _ClassVar[int]
        RESOLUTION_FIELD_NUMBER: _ClassVar[int]
        focal_length: _vector2_pb2.Vector2
        principal_point: _vector2_pb2.Vector2
        resolution: _vector2_int_pb2.Vector2Int
        def __init__(self, focal_length: _Optional[_Union[_vector2_pb2.Vector2, _Mapping]] = ..., principal_point: _Optional[_Union[_vector2_pb2.Vector2, _Mapping]] = ..., resolution: _Optional[_Union[_vector2_int_pb2.Vector2Int, _Mapping]] = ...) -> None: ...
    DEVICE_TIMESTAMP_FIELD_NUMBER: _ClassVar[int]
    IMAGE_TIMESTAMP_FIELD_NUMBER: _ClassVar[int]
    FORMAT_FIELD_NUMBER: _ClassVar[int]
    INTRINSICS_FIELD_NUMBER: _ClassVar[int]
    DATA_FIELD_NUMBER: _ClassVar[int]
    device_timestamp: _timestamp_pb2.Timestamp
    image_timestamp: float
    format: CameraFrame.Format
    intrinsics: CameraFrame.Intrinsics
    data: bytes
    def __init__(self, device_timestamp: _Optional[_Union[_timestamp_pb2.Timestamp, _Mapping]] = ..., image_timestamp: _Optional[float] = ..., format: _Optional[_Union[CameraFrame.Format, str]] = ..., intrinsics: _Optional[_Union[CameraFrame.Intrinsics, _Mapping]] = ..., data: _Optional[bytes] = ...) -> None: ...
