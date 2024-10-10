from google.protobuf.internal import containers as _containers
from google.protobuf import descriptor as _descriptor
from google.protobuf import message as _message
from typing import ClassVar as _ClassVar, Iterable as _Iterable, Mapping as _Mapping, Optional as _Optional, Union as _Union

DESCRIPTOR: _descriptor.FileDescriptor

class RegisterClientRequest(_message.Message):
    __slots__ = ("device_name", "camera_intrinsics", "camera_color", "camera_depth", "camera_transform", "camera_point_cloud", "camera_plane_detection", "gyroscope", "audio", "meshing")
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
        __slots__ = ("enabled", "data_type", "resize_factor_x", "resize_factor_y")
        ENABLED_FIELD_NUMBER: _ClassVar[int]
        DATA_TYPE_FIELD_NUMBER: _ClassVar[int]
        RESIZE_FACTOR_X_FIELD_NUMBER: _ClassVar[int]
        RESIZE_FACTOR_Y_FIELD_NUMBER: _ClassVar[int]
        enabled: bool
        data_type: str
        resize_factor_x: float
        resize_factor_y: float
        def __init__(self, enabled: bool = ..., data_type: _Optional[str] = ..., resize_factor_x: _Optional[float] = ..., resize_factor_y: _Optional[float] = ...) -> None: ...
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
    class CameraPlaneDetection(_message.Message):
        __slots__ = ("enabled",)
        ENABLED_FIELD_NUMBER: _ClassVar[int]
        enabled: bool
        def __init__(self, enabled: bool = ...) -> None: ...
    class Gyroscope(_message.Message):
        __slots__ = ("enabled",)
        ENABLED_FIELD_NUMBER: _ClassVar[int]
        enabled: bool
        def __init__(self, enabled: bool = ...) -> None: ...
    class Audio(_message.Message):
        __slots__ = ("enabled",)
        ENABLED_FIELD_NUMBER: _ClassVar[int]
        enabled: bool
        def __init__(self, enabled: bool = ...) -> None: ...
    class Meshing(_message.Message):
        __slots__ = ("enabled",)
        ENABLED_FIELD_NUMBER: _ClassVar[int]
        enabled: bool
        def __init__(self, enabled: bool = ...) -> None: ...
    DEVICE_NAME_FIELD_NUMBER: _ClassVar[int]
    CAMERA_INTRINSICS_FIELD_NUMBER: _ClassVar[int]
    CAMERA_COLOR_FIELD_NUMBER: _ClassVar[int]
    CAMERA_DEPTH_FIELD_NUMBER: _ClassVar[int]
    CAMERA_TRANSFORM_FIELD_NUMBER: _ClassVar[int]
    CAMERA_POINT_CLOUD_FIELD_NUMBER: _ClassVar[int]
    CAMERA_PLANE_DETECTION_FIELD_NUMBER: _ClassVar[int]
    GYROSCOPE_FIELD_NUMBER: _ClassVar[int]
    AUDIO_FIELD_NUMBER: _ClassVar[int]
    MESHING_FIELD_NUMBER: _ClassVar[int]
    device_name: str
    camera_intrinsics: RegisterClientRequest.CameraIntrinsics
    camera_color: RegisterClientRequest.CameraColor
    camera_depth: RegisterClientRequest.CameraDepth
    camera_transform: RegisterClientRequest.CameraTransform
    camera_point_cloud: RegisterClientRequest.CameraPointCloud
    camera_plane_detection: RegisterClientRequest.CameraPlaneDetection
    gyroscope: RegisterClientRequest.Gyroscope
    audio: RegisterClientRequest.Audio
    meshing: RegisterClientRequest.Meshing
    def __init__(self, device_name: _Optional[str] = ..., camera_intrinsics: _Optional[_Union[RegisterClientRequest.CameraIntrinsics, _Mapping]] = ..., camera_color: _Optional[_Union[RegisterClientRequest.CameraColor, _Mapping]] = ..., camera_depth: _Optional[_Union[RegisterClientRequest.CameraDepth, _Mapping]] = ..., camera_transform: _Optional[_Union[RegisterClientRequest.CameraTransform, _Mapping]] = ..., camera_point_cloud: _Optional[_Union[RegisterClientRequest.CameraPointCloud, _Mapping]] = ..., camera_plane_detection: _Optional[_Union[RegisterClientRequest.CameraPlaneDetection, _Mapping]] = ..., gyroscope: _Optional[_Union[RegisterClientRequest.Gyroscope, _Mapping]] = ..., audio: _Optional[_Union[RegisterClientRequest.Audio, _Mapping]] = ..., meshing: _Optional[_Union[RegisterClientRequest.Meshing, _Mapping]] = ...) -> None: ...

class RegisterClientResponse(_message.Message):
    __slots__ = ("uid",)
    UID_FIELD_NUMBER: _ClassVar[int]
    uid: str
    def __init__(self, uid: _Optional[str] = ...) -> None: ...

class ProcessFrameRequest(_message.Message):
    __slots__ = ("uid", "color", "depth", "transform", "plane_detection", "gyroscope", "audio_data", "meshes")
    class Vector3(_message.Message):
        __slots__ = ("x", "y", "z")
        X_FIELD_NUMBER: _ClassVar[int]
        Y_FIELD_NUMBER: _ClassVar[int]
        Z_FIELD_NUMBER: _ClassVar[int]
        x: float
        y: float
        z: float
        def __init__(self, x: _Optional[float] = ..., y: _Optional[float] = ..., z: _Optional[float] = ...) -> None: ...
    class Vector2(_message.Message):
        __slots__ = ("x", "y")
        X_FIELD_NUMBER: _ClassVar[int]
        Y_FIELD_NUMBER: _ClassVar[int]
        x: float
        y: float
        def __init__(self, x: _Optional[float] = ..., y: _Optional[float] = ...) -> None: ...
    class Plane(_message.Message):
        __slots__ = ("center", "normal", "size", "boundary_points")
        CENTER_FIELD_NUMBER: _ClassVar[int]
        NORMAL_FIELD_NUMBER: _ClassVar[int]
        SIZE_FIELD_NUMBER: _ClassVar[int]
        BOUNDARY_POINTS_FIELD_NUMBER: _ClassVar[int]
        center: ProcessFrameRequest.Vector3
        normal: ProcessFrameRequest.Vector3
        size: ProcessFrameRequest.Vector2
        boundary_points: _containers.RepeatedCompositeFieldContainer[ProcessFrameRequest.Vector2]
        def __init__(self, center: _Optional[_Union[ProcessFrameRequest.Vector3, _Mapping]] = ..., normal: _Optional[_Union[ProcessFrameRequest.Vector3, _Mapping]] = ..., size: _Optional[_Union[ProcessFrameRequest.Vector2, _Mapping]] = ..., boundary_points: _Optional[_Iterable[_Union[ProcessFrameRequest.Vector2, _Mapping]]] = ...) -> None: ...
    class Quaternion(_message.Message):
        __slots__ = ("x", "y", "z", "w")
        X_FIELD_NUMBER: _ClassVar[int]
        Y_FIELD_NUMBER: _ClassVar[int]
        Z_FIELD_NUMBER: _ClassVar[int]
        W_FIELD_NUMBER: _ClassVar[int]
        x: float
        y: float
        z: float
        w: float
        def __init__(self, x: _Optional[float] = ..., y: _Optional[float] = ..., z: _Optional[float] = ..., w: _Optional[float] = ...) -> None: ...
    class GyroscopeData(_message.Message):
        __slots__ = ("attitude", "rotation_rate", "gravity", "acceleration")
        ATTITUDE_FIELD_NUMBER: _ClassVar[int]
        ROTATION_RATE_FIELD_NUMBER: _ClassVar[int]
        GRAVITY_FIELD_NUMBER: _ClassVar[int]
        ACCELERATION_FIELD_NUMBER: _ClassVar[int]
        attitude: ProcessFrameRequest.Quaternion
        rotation_rate: ProcessFrameRequest.Vector3
        gravity: ProcessFrameRequest.Vector3
        acceleration: ProcessFrameRequest.Vector3
        def __init__(self, attitude: _Optional[_Union[ProcessFrameRequest.Quaternion, _Mapping]] = ..., rotation_rate: _Optional[_Union[ProcessFrameRequest.Vector3, _Mapping]] = ..., gravity: _Optional[_Union[ProcessFrameRequest.Vector3, _Mapping]] = ..., acceleration: _Optional[_Union[ProcessFrameRequest.Vector3, _Mapping]] = ...) -> None: ...
    class Mesh(_message.Message):
        __slots__ = ("data",)
        DATA_FIELD_NUMBER: _ClassVar[int]
        data: bytes
        def __init__(self, data: _Optional[bytes] = ...) -> None: ...
    UID_FIELD_NUMBER: _ClassVar[int]
    COLOR_FIELD_NUMBER: _ClassVar[int]
    DEPTH_FIELD_NUMBER: _ClassVar[int]
    TRANSFORM_FIELD_NUMBER: _ClassVar[int]
    PLANE_DETECTION_FIELD_NUMBER: _ClassVar[int]
    GYROSCOPE_FIELD_NUMBER: _ClassVar[int]
    AUDIO_DATA_FIELD_NUMBER: _ClassVar[int]
    MESHES_FIELD_NUMBER: _ClassVar[int]
    uid: str
    color: bytes
    depth: bytes
    transform: bytes
    plane_detection: _containers.RepeatedCompositeFieldContainer[ProcessFrameRequest.Plane]
    gyroscope: ProcessFrameRequest.GyroscopeData
    audio_data: _containers.RepeatedScalarFieldContainer[float]
    meshes: _containers.RepeatedCompositeFieldContainer[ProcessFrameRequest.Mesh]
    def __init__(self, uid: _Optional[str] = ..., color: _Optional[bytes] = ..., depth: _Optional[bytes] = ..., transform: _Optional[bytes] = ..., plane_detection: _Optional[_Iterable[_Union[ProcessFrameRequest.Plane, _Mapping]]] = ..., gyroscope: _Optional[_Union[ProcessFrameRequest.GyroscopeData, _Mapping]] = ..., audio_data: _Optional[_Iterable[float]] = ..., meshes: _Optional[_Iterable[_Union[ProcessFrameRequest.Mesh, _Mapping]]] = ...) -> None: ...

class ProcessFrameResponse(_message.Message):
    __slots__ = ("message",)
    MESSAGE_FIELD_NUMBER: _ClassVar[int]
    message: str
    def __init__(self, message: _Optional[str] = ...) -> None: ...
