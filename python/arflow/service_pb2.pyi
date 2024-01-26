from google.protobuf import descriptor as _descriptor
from google.protobuf import message as _message
from typing import ClassVar as _ClassVar, Mapping as _Mapping, Optional as _Optional, Union as _Union

DESCRIPTOR: _descriptor.FileDescriptor

class RegisterRequest(_message.Message):
    __slots__ = ("device_name", "camera_intrinsics", "camera_color", "camera_depth", "camera_transform", "camera_point_cloud")
    class CameraIntrinsics(_message.Message):
        __slots__ = ("focal_length_x", "focal_length_y", "principal_point_x", "principal_point_y", "resolution_x", "resolution_y")
        FOCAL_LENGTH_X_FIELD_NUMBER: _ClassVar[int]
        FOCAL_LENGTH_Y_FIELD_NUMBER: _ClassVar[int]
        PRINCIPAL_POINT_X_FIELD_NUMBER: _ClassVar[int]
        PRINCIPAL_POINT_Y_FIELD_NUMBER: _ClassVar[int]
        RESOLUTION_X_FIELD_NUMBER: _ClassVar[int]
        RESOLUTION_Y_FIELD_NUMBER: _ClassVar[int]
        focal_length_x: float
        focal_length_y: float
        principal_point_x: float
        principal_point_y: float
        resolution_x: int
        resolution_y: int
        def __init__(self, focal_length_x: _Optional[float] = ..., focal_length_y: _Optional[float] = ..., principal_point_x: _Optional[float] = ..., principal_point_y: _Optional[float] = ..., resolution_x: _Optional[int] = ..., resolution_y: _Optional[int] = ...) -> None: ...
    class CameraColor(_message.Message):
        __slots__ = ("enabled", "resize_factor_x", "resize_factor_y")
        ENABLED_FIELD_NUMBER: _ClassVar[int]
        RESIZE_FACTOR_X_FIELD_NUMBER: _ClassVar[int]
        RESIZE_FACTOR_Y_FIELD_NUMBER: _ClassVar[int]
        enabled: bool
        resize_factor_x: float
        resize_factor_y: float
        def __init__(self, enabled: bool = ..., resize_factor_x: _Optional[float] = ..., resize_factor_y: _Optional[float] = ...) -> None: ...
    class CameraDepth(_message.Message):
        __slots__ = ("enabled", "data_type", "confidence_filtering_level", "resolution_x", "resolution_y")
        ENABLED_FIELD_NUMBER: _ClassVar[int]
        DATA_TYPE_FIELD_NUMBER: _ClassVar[int]
        CONFIDENCE_FILTERING_LEVEL_FIELD_NUMBER: _ClassVar[int]
        RESOLUTION_X_FIELD_NUMBER: _ClassVar[int]
        RESOLUTION_Y_FIELD_NUMBER: _ClassVar[int]
        enabled: bool
        data_type: str
        confidence_filtering_level: int
        resolution_x: int
        resolution_y: int
        def __init__(self, enabled: bool = ..., data_type: _Optional[str] = ..., confidence_filtering_level: _Optional[int] = ..., resolution_x: _Optional[int] = ..., resolution_y: _Optional[int] = ...) -> None: ...
    class CameraTransform(_message.Message):
        __slots__ = ("enabled",)
        ENABLED_FIELD_NUMBER: _ClassVar[int]
        enabled: bool
        def __init__(self, enabled: bool = ...) -> None: ...
    class CameraPointCloud(_message.Message):
        __slots__ = ("enabled", "depth_upscale_factor")
        ENABLED_FIELD_NUMBER: _ClassVar[int]
        DEPTH_UPSCALE_FACTOR_FIELD_NUMBER: _ClassVar[int]
        enabled: bool
        depth_upscale_factor: float
        def __init__(self, enabled: bool = ..., depth_upscale_factor: _Optional[float] = ...) -> None: ...
    DEVICE_NAME_FIELD_NUMBER: _ClassVar[int]
    CAMERA_INTRINSICS_FIELD_NUMBER: _ClassVar[int]
    CAMERA_COLOR_FIELD_NUMBER: _ClassVar[int]
    CAMERA_DEPTH_FIELD_NUMBER: _ClassVar[int]
    CAMERA_TRANSFORM_FIELD_NUMBER: _ClassVar[int]
    CAMERA_POINT_CLOUD_FIELD_NUMBER: _ClassVar[int]
    device_name: str
    camera_intrinsics: RegisterRequest.CameraIntrinsics
    camera_color: RegisterRequest.CameraColor
    camera_depth: RegisterRequest.CameraDepth
    camera_transform: RegisterRequest.CameraTransform
    camera_point_cloud: RegisterRequest.CameraPointCloud
    def __init__(self, device_name: _Optional[str] = ..., camera_intrinsics: _Optional[_Union[RegisterRequest.CameraIntrinsics, _Mapping]] = ..., camera_color: _Optional[_Union[RegisterRequest.CameraColor, _Mapping]] = ..., camera_depth: _Optional[_Union[RegisterRequest.CameraDepth, _Mapping]] = ..., camera_transform: _Optional[_Union[RegisterRequest.CameraTransform, _Mapping]] = ..., camera_point_cloud: _Optional[_Union[RegisterRequest.CameraPointCloud, _Mapping]] = ...) -> None: ...

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
