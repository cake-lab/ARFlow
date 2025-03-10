""".. include:: ../README.md"""  # noqa: D415

# Imported symbols are private by default. By aliasing them here, we make it clear that they are part of the public API.
from arflow._core import ARFlowServicer as ARFlowServicer
from arflow._core import run_server as run_server
from arflow._session_stream import (
    SessionStream as SessionStream,
)
from cakelab.arflow_grpc.v1.ar_frame_pb2 import ARFrame as ARFrame
from cakelab.arflow_grpc.v1.audio_frame_pb2 import AudioFrame as AudioFrame
from cakelab.arflow_grpc.v1.color_frame_pb2 import ColorFrame as ColorFrame
from cakelab.arflow_grpc.v1.depth_frame_pb2 import DepthFrame as DepthFrame
from cakelab.arflow_grpc.v1.device_pb2 import Device as Device
from cakelab.arflow_grpc.v1.gyroscope_frame_pb2 import GyroscopeFrame as GyroscopeFrame
from cakelab.arflow_grpc.v1.mesh_detection_frame_pb2 import (
    MeshDetectionFrame as MeshDetectionFrame,
)
from cakelab.arflow_grpc.v1.plane_detection_frame_pb2 import (
    PlaneDetectionFrame as PlaneDetectionFrame,
)
from cakelab.arflow_grpc.v1.point_cloud_detection_frame_pb2 import (
    PointCloudDetectionFrame as PointCloudDetectionFrame,
)
from cakelab.arflow_grpc.v1.session_pb2 import Session as Session
from cakelab.arflow_grpc.v1.transform_frame_pb2 import TransformFrame as TransformFrame

__docformat__ = "google"  # Should match Ruff docstring format in ../pyproject.toml

# https://pdoc.dev/docs/pdoc.html#exclude-submodules-from-being-documented
__all__ = [
    "run_server",
    "ARFlowServicer",
    "ARFrame",
    "TransformFrame",
    "ColorFrame",
    "DepthFrame",
    "GyroscopeFrame",
    "AudioFrame",
    "PlaneDetectionFrame",
    "PointCloudDetectionFrame",
    "MeshDetectionFrame",
    "SessionStream",
    "Session",
    "Device",
]
