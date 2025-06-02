from cakelab.arflow_grpc.v1 import ar_trackable_pb2 as _ar_trackable_pb2
from cakelab.arflow_grpc.v1 import vector3_pb2 as _vector3_pb2
from google.protobuf.internal import containers as _containers
from google.protobuf import descriptor as _descriptor
from google.protobuf import message as _message
from typing import ClassVar as _ClassVar, Iterable as _Iterable, Mapping as _Mapping, Optional as _Optional, Union as _Union

DESCRIPTOR: _descriptor.FileDescriptor

class ARPointCloud(_message.Message):
    __slots__ = ("trackable", "confidence_values", "identifiers", "positions")
    TRACKABLE_FIELD_NUMBER: _ClassVar[int]
    CONFIDENCE_VALUES_FIELD_NUMBER: _ClassVar[int]
    IDENTIFIERS_FIELD_NUMBER: _ClassVar[int]
    POSITIONS_FIELD_NUMBER: _ClassVar[int]
    trackable: _ar_trackable_pb2.ARTrackable
    confidence_values: _containers.RepeatedScalarFieldContainer[float]
    identifiers: _containers.RepeatedScalarFieldContainer[int]
    positions: _containers.RepeatedCompositeFieldContainer[_vector3_pb2.Vector3]
    def __init__(self, trackable: _Optional[_Union[_ar_trackable_pb2.ARTrackable, _Mapping]] = ..., confidence_values: _Optional[_Iterable[float]] = ..., identifiers: _Optional[_Iterable[int]] = ..., positions: _Optional[_Iterable[_Union[_vector3_pb2.Vector3, _Mapping]]] = ...) -> None: ...
