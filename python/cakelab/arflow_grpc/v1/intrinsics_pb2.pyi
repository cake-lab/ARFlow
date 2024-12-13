from cakelab.arflow_grpc.v1 import vector2_pb2 as _vector2_pb2
from cakelab.arflow_grpc.v1 import vector2_int_pb2 as _vector2_int_pb2
from google.protobuf import descriptor as _descriptor
from google.protobuf import message as _message
from typing import ClassVar as _ClassVar, Mapping as _Mapping, Optional as _Optional, Union as _Union

DESCRIPTOR: _descriptor.FileDescriptor

class Intrinsics(_message.Message):
    __slots__ = ("focal_length", "principal_point", "resolution")
    FOCAL_LENGTH_FIELD_NUMBER: _ClassVar[int]
    PRINCIPAL_POINT_FIELD_NUMBER: _ClassVar[int]
    RESOLUTION_FIELD_NUMBER: _ClassVar[int]
    focal_length: _vector2_pb2.Vector2
    principal_point: _vector2_pb2.Vector2
    resolution: _vector2_int_pb2.Vector2Int
    def __init__(self, focal_length: _Optional[_Union[_vector2_pb2.Vector2, _Mapping]] = ..., principal_point: _Optional[_Union[_vector2_pb2.Vector2, _Mapping]] = ..., resolution: _Optional[_Union[_vector2_int_pb2.Vector2Int, _Mapping]] = ...) -> None: ...
