from cakelab.arflow_grpc.v1 import audio_frame_pb2 as _audio_frame_pb2
from cakelab.arflow_grpc.v1 import color_frame_pb2 as _color_frame_pb2
from cakelab.arflow_grpc.v1 import depth_frame_pb2 as _depth_frame_pb2
from cakelab.arflow_grpc.v1 import gyroscope_frame_pb2 as _gyroscope_frame_pb2
from cakelab.arflow_grpc.v1 import mesh_detection_frame_pb2 as _mesh_detection_frame_pb2
from cakelab.arflow_grpc.v1 import plane_detection_frame_pb2 as _plane_detection_frame_pb2
from cakelab.arflow_grpc.v1 import point_cloud_detection_frame_pb2 as _point_cloud_detection_frame_pb2
from cakelab.arflow_grpc.v1 import transform_frame_pb2 as _transform_frame_pb2
from google.protobuf import descriptor as _descriptor
from google.protobuf import message as _message
from typing import ClassVar as _ClassVar, Mapping as _Mapping, Optional as _Optional, Union as _Union

DESCRIPTOR: _descriptor.FileDescriptor

class ARFrame(_message.Message):
    __slots__ = ("transform_frame", "color_frame", "depth_frame", "gyroscope_frame", "audio_frame", "plane_detection_frame", "point_cloud_detection_frame", "mesh_detection_frame")
    TRANSFORM_FRAME_FIELD_NUMBER: _ClassVar[int]
    COLOR_FRAME_FIELD_NUMBER: _ClassVar[int]
    DEPTH_FRAME_FIELD_NUMBER: _ClassVar[int]
    GYROSCOPE_FRAME_FIELD_NUMBER: _ClassVar[int]
    AUDIO_FRAME_FIELD_NUMBER: _ClassVar[int]
    PLANE_DETECTION_FRAME_FIELD_NUMBER: _ClassVar[int]
    POINT_CLOUD_DETECTION_FRAME_FIELD_NUMBER: _ClassVar[int]
    MESH_DETECTION_FRAME_FIELD_NUMBER: _ClassVar[int]
    transform_frame: _transform_frame_pb2.TransformFrame
    color_frame: _color_frame_pb2.ColorFrame
    depth_frame: _depth_frame_pb2.DepthFrame
    gyroscope_frame: _gyroscope_frame_pb2.GyroscopeFrame
    audio_frame: _audio_frame_pb2.AudioFrame
    plane_detection_frame: _plane_detection_frame_pb2.PlaneDetectionFrame
    point_cloud_detection_frame: _point_cloud_detection_frame_pb2.PointCloudDetectionFrame
    mesh_detection_frame: _mesh_detection_frame_pb2.MeshDetectionFrame
    def __init__(self, transform_frame: _Optional[_Union[_transform_frame_pb2.TransformFrame, _Mapping]] = ..., color_frame: _Optional[_Union[_color_frame_pb2.ColorFrame, _Mapping]] = ..., depth_frame: _Optional[_Union[_depth_frame_pb2.DepthFrame, _Mapping]] = ..., gyroscope_frame: _Optional[_Union[_gyroscope_frame_pb2.GyroscopeFrame, _Mapping]] = ..., audio_frame: _Optional[_Union[_audio_frame_pb2.AudioFrame, _Mapping]] = ..., plane_detection_frame: _Optional[_Union[_plane_detection_frame_pb2.PlaneDetectionFrame, _Mapping]] = ..., point_cloud_detection_frame: _Optional[_Union[_point_cloud_detection_frame_pb2.PointCloudDetectionFrame, _Mapping]] = ..., mesh_detection_frame: _Optional[_Union[_mesh_detection_frame_pb2.MeshDetectionFrame, _Mapping]] = ...) -> None: ...
