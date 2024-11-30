from cakelab.arflow_grpc.v1 import ar_trackable_pb2 as _ar_trackable_pb2
from cakelab.arflow_grpc.v1 import vector2_pb2 as _vector2_pb2
from cakelab.arflow_grpc.v1 import vector3_pb2 as _vector3_pb2
from google.protobuf.internal import containers as _containers
from google.protobuf import descriptor as _descriptor
from google.protobuf import message as _message
from typing import ClassVar as _ClassVar, Iterable as _Iterable, Mapping as _Mapping, Optional as _Optional, Union as _Union

DESCRIPTOR: _descriptor.FileDescriptor

class ARPlane(_message.Message):
    __slots__ = ("trackable", "boundary", "center", "normal", "size", "subsumed_by_id")
    TRACKABLE_FIELD_NUMBER: _ClassVar[int]
    BOUNDARY_FIELD_NUMBER: _ClassVar[int]
    CENTER_FIELD_NUMBER: _ClassVar[int]
    NORMAL_FIELD_NUMBER: _ClassVar[int]
    SIZE_FIELD_NUMBER: _ClassVar[int]
    SUBSUMED_BY_ID_FIELD_NUMBER: _ClassVar[int]
    trackable: _ar_trackable_pb2.ARTrackable
    boundary: _containers.RepeatedCompositeFieldContainer[_vector2_pb2.Vector2]
    center: _vector3_pb2.Vector3
    normal: _vector3_pb2.Vector3
    size: _vector2_pb2.Vector2
    subsumed_by_id: _ar_trackable_pb2.ARTrackable.TrackableId
    def __init__(self, trackable: _Optional[_Union[_ar_trackable_pb2.ARTrackable, _Mapping]] = ..., boundary: _Optional[_Iterable[_Union[_vector2_pb2.Vector2, _Mapping]]] = ..., center: _Optional[_Union[_vector3_pb2.Vector3, _Mapping]] = ..., normal: _Optional[_Union[_vector3_pb2.Vector3, _Mapping]] = ..., size: _Optional[_Union[_vector2_pb2.Vector2, _Mapping]] = ..., subsumed_by_id: _Optional[_Union[_ar_trackable_pb2.ARTrackable.TrackableId, _Mapping]] = ...) -> None: ...
