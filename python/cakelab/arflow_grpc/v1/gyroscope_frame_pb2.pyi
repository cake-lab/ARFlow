from cakelab.arflow_grpc.v1 import quaternion_pb2 as _quaternion_pb2
from cakelab.arflow_grpc.v1 import vector3_pb2 as _vector3_pb2
from google.protobuf import timestamp_pb2 as _timestamp_pb2
from google.protobuf import descriptor as _descriptor
from google.protobuf import message as _message
from typing import ClassVar as _ClassVar, Mapping as _Mapping, Optional as _Optional, Union as _Union

DESCRIPTOR: _descriptor.FileDescriptor

class GyroscopeFrame(_message.Message):
    __slots__ = ("device_timestamp", "attitude", "rotation_rate", "gravity", "acceleration")
    DEVICE_TIMESTAMP_FIELD_NUMBER: _ClassVar[int]
    ATTITUDE_FIELD_NUMBER: _ClassVar[int]
    ROTATION_RATE_FIELD_NUMBER: _ClassVar[int]
    GRAVITY_FIELD_NUMBER: _ClassVar[int]
    ACCELERATION_FIELD_NUMBER: _ClassVar[int]
    device_timestamp: _timestamp_pb2.Timestamp
    attitude: _quaternion_pb2.Quaternion
    rotation_rate: _vector3_pb2.Vector3
    gravity: _vector3_pb2.Vector3
    acceleration: _vector3_pb2.Vector3
    def __init__(self, device_timestamp: _Optional[_Union[_timestamp_pb2.Timestamp, _Mapping]] = ..., attitude: _Optional[_Union[_quaternion_pb2.Quaternion, _Mapping]] = ..., rotation_rate: _Optional[_Union[_vector3_pb2.Vector3, _Mapping]] = ..., gravity: _Optional[_Union[_vector3_pb2.Vector3, _Mapping]] = ..., acceleration: _Optional[_Union[_vector3_pb2.Vector3, _Mapping]] = ...) -> None: ...
