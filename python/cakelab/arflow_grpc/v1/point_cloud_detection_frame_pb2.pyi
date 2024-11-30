from cakelab.arflow_grpc.v1 import ar_point_cloud_pb2 as _ar_point_cloud_pb2
from google.protobuf import timestamp_pb2 as _timestamp_pb2
from google.protobuf.internal import enum_type_wrapper as _enum_type_wrapper
from google.protobuf import descriptor as _descriptor
from google.protobuf import message as _message
from typing import ClassVar as _ClassVar, Mapping as _Mapping, Optional as _Optional, Union as _Union

DESCRIPTOR: _descriptor.FileDescriptor

class PointCloudDetectionFrame(_message.Message):
    __slots__ = ("state", "device_timestamp", "point_cloud")
    class State(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
        __slots__ = ()
        STATE_UNSPECIFIED: _ClassVar[PointCloudDetectionFrame.State]
        STATE_ADDED: _ClassVar[PointCloudDetectionFrame.State]
        STATE_UPDATED: _ClassVar[PointCloudDetectionFrame.State]
        STATE_REMOVED: _ClassVar[PointCloudDetectionFrame.State]
    STATE_UNSPECIFIED: PointCloudDetectionFrame.State
    STATE_ADDED: PointCloudDetectionFrame.State
    STATE_UPDATED: PointCloudDetectionFrame.State
    STATE_REMOVED: PointCloudDetectionFrame.State
    STATE_FIELD_NUMBER: _ClassVar[int]
    DEVICE_TIMESTAMP_FIELD_NUMBER: _ClassVar[int]
    POINT_CLOUD_FIELD_NUMBER: _ClassVar[int]
    state: PointCloudDetectionFrame.State
    device_timestamp: _timestamp_pb2.Timestamp
    point_cloud: _ar_point_cloud_pb2.ARPointCloud
    def __init__(self, state: _Optional[_Union[PointCloudDetectionFrame.State, str]] = ..., device_timestamp: _Optional[_Union[_timestamp_pb2.Timestamp, _Mapping]] = ..., point_cloud: _Optional[_Union[_ar_point_cloud_pb2.ARPointCloud, _Mapping]] = ...) -> None: ...
