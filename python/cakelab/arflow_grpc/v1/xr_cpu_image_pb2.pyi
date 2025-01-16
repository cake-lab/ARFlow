from cakelab.arflow_grpc.v1 import vector2_int_pb2 as _vector2_int_pb2
from google.protobuf.internal import containers as _containers
from google.protobuf.internal import enum_type_wrapper as _enum_type_wrapper
from google.protobuf import descriptor as _descriptor
from google.protobuf import message as _message
from typing import ClassVar as _ClassVar, Iterable as _Iterable, Mapping as _Mapping, Optional as _Optional, Union as _Union

DESCRIPTOR: _descriptor.FileDescriptor

class XRCpuImage(_message.Message):
    __slots__ = ("dimensions", "format", "timestamp", "planes")
    class Format(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
        __slots__ = ()
        FORMAT_UNSPECIFIED: _ClassVar[XRCpuImage.Format]
        FORMAT_ANDROID_YUV_420_888: _ClassVar[XRCpuImage.Format]
        FORMAT_IOS_YP_CBCR_420_8BI_PLANAR_FULL_RANGE: _ClassVar[XRCpuImage.Format]
        FORMAT_DEPTHFLOAT32: _ClassVar[XRCpuImage.Format]
        FORMAT_DEPTHUINT16: _ClassVar[XRCpuImage.Format]
    FORMAT_UNSPECIFIED: XRCpuImage.Format
    FORMAT_ANDROID_YUV_420_888: XRCpuImage.Format
    FORMAT_IOS_YP_CBCR_420_8BI_PLANAR_FULL_RANGE: XRCpuImage.Format
    FORMAT_DEPTHFLOAT32: XRCpuImage.Format
    FORMAT_DEPTHUINT16: XRCpuImage.Format
    class Plane(_message.Message):
        __slots__ = ("row_stride", "pixel_stride", "data")
        ROW_STRIDE_FIELD_NUMBER: _ClassVar[int]
        PIXEL_STRIDE_FIELD_NUMBER: _ClassVar[int]
        DATA_FIELD_NUMBER: _ClassVar[int]
        row_stride: int
        pixel_stride: int
        data: bytes
        def __init__(self, row_stride: _Optional[int] = ..., pixel_stride: _Optional[int] = ..., data: _Optional[bytes] = ...) -> None: ...
    DIMENSIONS_FIELD_NUMBER: _ClassVar[int]
    FORMAT_FIELD_NUMBER: _ClassVar[int]
    TIMESTAMP_FIELD_NUMBER: _ClassVar[int]
    PLANES_FIELD_NUMBER: _ClassVar[int]
    dimensions: _vector2_int_pb2.Vector2Int
    format: XRCpuImage.Format
    timestamp: float
    planes: _containers.RepeatedCompositeFieldContainer[XRCpuImage.Plane]
    def __init__(self, dimensions: _Optional[_Union[_vector2_int_pb2.Vector2Int, _Mapping]] = ..., format: _Optional[_Union[XRCpuImage.Format, str]] = ..., timestamp: _Optional[float] = ..., planes: _Optional[_Iterable[_Union[XRCpuImage.Plane, _Mapping]]] = ...) -> None: ...
