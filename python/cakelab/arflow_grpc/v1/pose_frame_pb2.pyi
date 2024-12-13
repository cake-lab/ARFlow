from cakelab.arflow_grpc.v1 import pose_pb2 as _pose_pb2
from google.protobuf import timestamp_pb2 as _timestamp_pb2
from google.protobuf import descriptor as _descriptor
from google.protobuf import message as _message
from typing import ClassVar as _ClassVar, Mapping as _Mapping, Optional as _Optional, Union as _Union

DESCRIPTOR: _descriptor.FileDescriptor

class PoseFrame(_message.Message):
    __slots__ = ("device_timestamp", "pose")
    DEVICE_TIMESTAMP_FIELD_NUMBER: _ClassVar[int]
    POSE_FIELD_NUMBER: _ClassVar[int]
    device_timestamp: _timestamp_pb2.Timestamp
    pose: _pose_pb2.Pose
    def __init__(self, device_timestamp: _Optional[_Union[_timestamp_pb2.Timestamp, _Mapping]] = ..., pose: _Optional[_Union[_pose_pb2.Pose, _Mapping]] = ...) -> None: ...
