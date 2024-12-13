from cakelab.arflow_grpc.v1 import quaternion_pb2 as _quaternion_pb2
from cakelab.arflow_grpc.v1 import vector3_pb2 as _vector3_pb2
from google.protobuf import descriptor as _descriptor
from google.protobuf import message as _message
from typing import ClassVar as _ClassVar, Mapping as _Mapping, Optional as _Optional, Union as _Union

DESCRIPTOR: _descriptor.FileDescriptor

class Pose(_message.Message):
    __slots__ = ("forward", "position", "right", "rotation", "up")
    FORWARD_FIELD_NUMBER: _ClassVar[int]
    POSITION_FIELD_NUMBER: _ClassVar[int]
    RIGHT_FIELD_NUMBER: _ClassVar[int]
    ROTATION_FIELD_NUMBER: _ClassVar[int]
    UP_FIELD_NUMBER: _ClassVar[int]
    forward: _vector3_pb2.Vector3
    position: _vector3_pb2.Vector3
    right: _vector3_pb2.Vector3
    rotation: _quaternion_pb2.Quaternion
    up: _vector3_pb2.Vector3
    def __init__(self, forward: _Optional[_Union[_vector3_pb2.Vector3, _Mapping]] = ..., position: _Optional[_Union[_vector3_pb2.Vector3, _Mapping]] = ..., right: _Optional[_Union[_vector3_pb2.Vector3, _Mapping]] = ..., rotation: _Optional[_Union[_quaternion_pb2.Quaternion, _Mapping]] = ..., up: _Optional[_Union[_vector3_pb2.Vector3, _Mapping]] = ...) -> None: ...
