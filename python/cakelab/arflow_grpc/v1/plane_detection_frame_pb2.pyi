from cakelab.arflow_grpc.v1 import ar_plane_pb2 as _ar_plane_pb2
from google.protobuf import timestamp_pb2 as _timestamp_pb2
from google.protobuf.internal import enum_type_wrapper as _enum_type_wrapper
from google.protobuf import descriptor as _descriptor
from google.protobuf import message as _message
from typing import ClassVar as _ClassVar, Mapping as _Mapping, Optional as _Optional, Union as _Union

DESCRIPTOR: _descriptor.FileDescriptor

class PlaneDetectionFrame(_message.Message):
    __slots__ = ("state", "device_timestamp", "plane")
    class State(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
        __slots__ = ()
        STATE_UNSPECIFIED: _ClassVar[PlaneDetectionFrame.State]
        STATE_ADDED: _ClassVar[PlaneDetectionFrame.State]
        STATE_UPDATED: _ClassVar[PlaneDetectionFrame.State]
        STATE_REMOVED: _ClassVar[PlaneDetectionFrame.State]
    STATE_UNSPECIFIED: PlaneDetectionFrame.State
    STATE_ADDED: PlaneDetectionFrame.State
    STATE_UPDATED: PlaneDetectionFrame.State
    STATE_REMOVED: PlaneDetectionFrame.State
    STATE_FIELD_NUMBER: _ClassVar[int]
    DEVICE_TIMESTAMP_FIELD_NUMBER: _ClassVar[int]
    PLANE_FIELD_NUMBER: _ClassVar[int]
    state: PlaneDetectionFrame.State
    device_timestamp: _timestamp_pb2.Timestamp
    plane: _ar_plane_pb2.ARPlane
    def __init__(self, state: _Optional[_Union[PlaneDetectionFrame.State, str]] = ..., device_timestamp: _Optional[_Union[_timestamp_pb2.Timestamp, _Mapping]] = ..., plane: _Optional[_Union[_ar_plane_pb2.ARPlane, _Mapping]] = ...) -> None: ...
