from google.protobuf import descriptor as _descriptor
from google.protobuf import message as _message
from typing import ClassVar as _ClassVar, Mapping as _Mapping, Optional as _Optional, Union as _Union

DESCRIPTOR: _descriptor.FileDescriptor

class RegisterRequest(_message.Message):
    __slots__ = ("device_name", "camera_intrinsics", "camera_color", "camera_depth", "camera_transform")
    class CameraIntrinsics(_message.Message):
        __slots__ = ("focal_length_x", "focal_length_y", "principal_point_x", "principal_point_y", "native_resolution_x", "native_resolution_y", "sample_resolution_x", "sample_resolution_y")
        FOCAL_LENGTH_X_FIELD_NUMBER: _ClassVar[int]
        FOCAL_LENGTH_Y_FIELD_NUMBER: _ClassVar[int]
        PRINCIPAL_POINT_X_FIELD_NUMBER: _ClassVar[int]
        PRINCIPAL_POINT_Y_FIELD_NUMBER: _ClassVar[int]
        NATIVE_RESOLUTION_X_FIELD_NUMBER: _ClassVar[int]
        NATIVE_RESOLUTION_Y_FIELD_NUMBER: _ClassVar[int]
        SAMPLE_RESOLUTION_X_FIELD_NUMBER: _ClassVar[int]
        SAMPLE_RESOLUTION_Y_FIELD_NUMBER: _ClassVar[int]
        focal_length_x: float
        focal_length_y: float
        principal_point_x: float
        principal_point_y: float
        native_resolution_x: int
        native_resolution_y: int
        sample_resolution_x: int
        sample_resolution_y: int
        def __init__(self, focal_length_x: _Optional[float] = ..., focal_length_y: _Optional[float] = ..., principal_point_x: _Optional[float] = ..., principal_point_y: _Optional[float] = ..., native_resolution_x: _Optional[int] = ..., native_resolution_y: _Optional[int] = ..., sample_resolution_x: _Optional[int] = ..., sample_resolution_y: _Optional[int] = ...) -> None: ...
    class CameraColor(_message.Message):
        __slots__ = ("enabled", "data_type")
        ENABLED_FIELD_NUMBER: _ClassVar[int]
        DATA_TYPE_FIELD_NUMBER: _ClassVar[int]
        enabled: bool
        data_type: int
        def __init__(self, enabled: bool = ..., data_type: _Optional[int] = ...) -> None: ...
    class CameraDepth(_message.Message):
        __slots__ = ("enabled", "data_depth", "confidence_filtering_level")
        ENABLED_FIELD_NUMBER: _ClassVar[int]
        DATA_DEPTH_FIELD_NUMBER: _ClassVar[int]
        CONFIDENCE_FILTERING_LEVEL_FIELD_NUMBER: _ClassVar[int]
        enabled: bool
        data_depth: int
        confidence_filtering_level: int
        def __init__(self, enabled: bool = ..., data_depth: _Optional[int] = ..., confidence_filtering_level: _Optional[int] = ...) -> None: ...
    class CameraTransform(_message.Message):
        __slots__ = ("enabled",)
        ENABLED_FIELD_NUMBER: _ClassVar[int]
        enabled: bool
        def __init__(self, enabled: bool = ...) -> None: ...
    DEVICE_NAME_FIELD_NUMBER: _ClassVar[int]
    CAMERA_INTRINSICS_FIELD_NUMBER: _ClassVar[int]
    CAMERA_COLOR_FIELD_NUMBER: _ClassVar[int]
    CAMERA_DEPTH_FIELD_NUMBER: _ClassVar[int]
    CAMERA_TRANSFORM_FIELD_NUMBER: _ClassVar[int]
    device_name: str
    camera_intrinsics: RegisterRequest.CameraIntrinsics
    camera_color: RegisterRequest.CameraColor
    camera_depth: RegisterRequest.CameraDepth
    camera_transform: RegisterRequest.CameraTransform
    def __init__(self, device_name: _Optional[str] = ..., camera_intrinsics: _Optional[_Union[RegisterRequest.CameraIntrinsics, _Mapping]] = ..., camera_color: _Optional[_Union[RegisterRequest.CameraColor, _Mapping]] = ..., camera_depth: _Optional[_Union[RegisterRequest.CameraDepth, _Mapping]] = ..., camera_transform: _Optional[_Union[RegisterRequest.CameraTransform, _Mapping]] = ...) -> None: ...

class RegisterResponse(_message.Message):
    __slots__ = ("message",)
    MESSAGE_FIELD_NUMBER: _ClassVar[int]
    message: str
    def __init__(self, message: _Optional[str] = ...) -> None: ...

class DataFrameRequest(_message.Message):
    __slots__ = ("uid", "color", "depth", "transform")
    UID_FIELD_NUMBER: _ClassVar[int]
    COLOR_FIELD_NUMBER: _ClassVar[int]
    DEPTH_FIELD_NUMBER: _ClassVar[int]
    TRANSFORM_FIELD_NUMBER: _ClassVar[int]
    uid: str
    color: bytes
    depth: bytes
    transform: bytes
    def __init__(self, uid: _Optional[str] = ..., color: _Optional[bytes] = ..., depth: _Optional[bytes] = ..., transform: _Optional[bytes] = ...) -> None: ...

class DataFrameResponse(_message.Message):
    __slots__ = ("message",)
    MESSAGE_FIELD_NUMBER: _ClassVar[int]
    message: str
    def __init__(self, message: _Optional[str] = ...) -> None: ...
