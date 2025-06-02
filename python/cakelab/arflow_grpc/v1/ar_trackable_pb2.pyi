from cakelab.arflow_grpc.v1 import pose_pb2 as _pose_pb2
from google.protobuf.internal import enum_type_wrapper as _enum_type_wrapper
from google.protobuf import descriptor as _descriptor
from google.protobuf import message as _message
from typing import ClassVar as _ClassVar, Mapping as _Mapping, Optional as _Optional, Union as _Union

DESCRIPTOR: _descriptor.FileDescriptor

class ARTrackable(_message.Message):
    __slots__ = ("pose", "trackable_id", "tracking_state")
    class TrackingState(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
        __slots__ = ()
        TRACKING_STATE_UNSPECIFIED: _ClassVar[ARTrackable.TrackingState]
        TRACKING_STATE_LIMITED: _ClassVar[ARTrackable.TrackingState]
        TRACKING_STATE_NONE: _ClassVar[ARTrackable.TrackingState]
        TRACKING_STATE_TRACKING: _ClassVar[ARTrackable.TrackingState]
    TRACKING_STATE_UNSPECIFIED: ARTrackable.TrackingState
    TRACKING_STATE_LIMITED: ARTrackable.TrackingState
    TRACKING_STATE_NONE: ARTrackable.TrackingState
    TRACKING_STATE_TRACKING: ARTrackable.TrackingState
    class TrackableId(_message.Message):
        __slots__ = ("sub_id_1", "sub_id_2")
        SUB_ID_1_FIELD_NUMBER: _ClassVar[int]
        SUB_ID_2_FIELD_NUMBER: _ClassVar[int]
        sub_id_1: int
        sub_id_2: int
        def __init__(self, sub_id_1: _Optional[int] = ..., sub_id_2: _Optional[int] = ...) -> None: ...
    POSE_FIELD_NUMBER: _ClassVar[int]
    TRACKABLE_ID_FIELD_NUMBER: _ClassVar[int]
    TRACKING_STATE_FIELD_NUMBER: _ClassVar[int]
    pose: _pose_pb2.Pose
    trackable_id: ARTrackable.TrackableId
    tracking_state: ARTrackable.TrackingState
    def __init__(self, pose: _Optional[_Union[_pose_pb2.Pose, _Mapping]] = ..., trackable_id: _Optional[_Union[ARTrackable.TrackableId, _Mapping]] = ..., tracking_state: _Optional[_Union[ARTrackable.TrackingState, str]] = ...) -> None: ...
