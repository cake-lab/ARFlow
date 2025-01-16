from cakelab.arflow_grpc.v1 import mesh_filter_pb2 as _mesh_filter_pb2
from google.protobuf import timestamp_pb2 as _timestamp_pb2
from google.protobuf.internal import enum_type_wrapper as _enum_type_wrapper
from google.protobuf import descriptor as _descriptor
from google.protobuf import message as _message
from typing import ClassVar as _ClassVar, Mapping as _Mapping, Optional as _Optional, Union as _Union

DESCRIPTOR: _descriptor.FileDescriptor

class MeshDetectionFrame(_message.Message):
    __slots__ = ("state", "device_timestamp", "mesh_filter")
    class State(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
        __slots__ = ()
        STATE_UNSPECIFIED: _ClassVar[MeshDetectionFrame.State]
        STATE_ADDED: _ClassVar[MeshDetectionFrame.State]
        STATE_UPDATED: _ClassVar[MeshDetectionFrame.State]
        STATE_REMOVED: _ClassVar[MeshDetectionFrame.State]
    STATE_UNSPECIFIED: MeshDetectionFrame.State
    STATE_ADDED: MeshDetectionFrame.State
    STATE_UPDATED: MeshDetectionFrame.State
    STATE_REMOVED: MeshDetectionFrame.State
    STATE_FIELD_NUMBER: _ClassVar[int]
    DEVICE_TIMESTAMP_FIELD_NUMBER: _ClassVar[int]
    MESH_FILTER_FIELD_NUMBER: _ClassVar[int]
    state: MeshDetectionFrame.State
    device_timestamp: _timestamp_pb2.Timestamp
    mesh_filter: _mesh_filter_pb2.MeshFilter
    def __init__(self, state: _Optional[_Union[MeshDetectionFrame.State, str]] = ..., device_timestamp: _Optional[_Union[_timestamp_pb2.Timestamp, _Mapping]] = ..., mesh_filter: _Optional[_Union[_mesh_filter_pb2.MeshFilter, _Mapping]] = ...) -> None: ...
