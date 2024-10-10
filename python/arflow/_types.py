"""Type definitions for ARFlow."""

from __future__ import annotations

from dataclasses import dataclass
from typing import Dict, List, Literal, NewType

import numpy as np
import numpy.typing as npt

from arflow_grpc.service_pb2 import ProcessFrameRequest, RegisterClientRequest

ARFlowRequest = ProcessFrameRequest | RegisterClientRequest


@dataclass
class EnrichedARFlowRequest:
    """An enriched ARFlow request."""

    timestamp: float
    """The timestamp of the request."""
    data: ARFlowRequest
    """The ARFlow request data."""


ColorDataType = Literal["RGB24", "YCbCr420"]
DepthDataType = Literal["f32", "u16"]
"""The depth data type. `f32` for iOS, `u16` for Android."""

ColorRGB = npt.NDArray[np.uint8]
DepthImg = npt.NDArray[np.float32]
Transform = npt.NDArray[np.float32]
Intrinsic = npt.NDArray[np.float32]
PointCloudPCD = npt.NDArray[np.float32]
PointCloudCLR = npt.NDArray[np.uint8]

Attitude = npt.NDArray[np.float32]
RotationRate = npt.NDArray[np.float32]
Gravity = npt.NDArray[np.float32]
Acceleration = npt.NDArray[np.float32]

Audio = npt.NDArray[np.float32]

MeshFaces = npt.NDArray[np.uint32]
MeshPoints = npt.NDArray[np.float32]
MeshNormals = npt.NDArray[np.float32]
MeshTexCoord = npt.NDArray[np.float32]
MeshColors = npt.NDArray[np.float32]

PlaneCenter = npt.NDArray[np.float32]
PlaneNormal = npt.NDArray[np.float32]
PlaneSize = npt.NDArray[np.float32]
PlaneBoundaryPoints2D = npt.NDArray[np.float32]
PlaneBoundaryPoints3D = npt.NDArray[np.float32]


@dataclass
class PlaneInfo:
    """Information about a plane."""

    center: PlaneCenter
    """The center of the plane. In world space (3D)."""
    normal: PlaneNormal
    """The normal of the plane. In world space (3D)."""
    size: PlaneSize
    """Width and Height of the plane. In meters (2D)"""
    boundary_points: PlaneBoundaryPoints2D
    """The boundary points of the plane. In plane space (2D)."""


@dataclass
class GyroscopeInfo:
    """Information about a gyroscope."""

    attitude: Attitude
    """The attitude of the gyroscope."""
    rotation_rate: RotationRate
    """The rotation rate of the gyroscope."""
    gravity: Gravity
    """The gravity of the gyroscope."""
    acceleration: Acceleration
    """The acceleration of the gyroscope."""


@dataclass
class Mesh:
    """A mesh object. Draco's meshes have additional methods and properties, but with limited documentation and usage on them, I will not include them here."""

    faces: MeshFaces
    """The mesh faces. Each face is an array of [3] indices."""
    points: MeshPoints
    """The mesh points. Each point is an array of [3] coordinates."""
    normals: MeshNormals | None = None
    """The mesh normals. Each normal is an array of [3] coordinates. If the mesh does not have normals, this field is `None`."""
    tex_coord: MeshTexCoord | None = None
    """The mesh texture coordinates. Each texture coordinate is an array of [2] coordinates. If the mesh does not have texture coordinates, this field is `None`."""
    colors: MeshColors | None = None
    """The mesh colors. If the mesh does not have colors, this field is `None`."""


@dataclass
class DecodedDataFrame:
    """A decoded data frame."""

    color_rgb: ColorRGB | None = None
    """The color image in RGB format."""
    depth_img: DepthImg | None = None
    """The depth image."""
    transform: Transform | None = None
    """The transformation matrix of the camera."""
    intrinsic: Intrinsic | None = None
    """The intrinsic matrix of the camera."""
    point_cloud_pcd: PointCloudPCD | None = None
    """The point cloud in PCD format."""
    point_cloud_clr: PointCloudCLR | None = None
    """The point cloud colors in RGB format."""


RequestsHistory = List[EnrichedARFlowRequest]
HashableClientIdentifier = NewType("HashableClientIdentifier", str)
"""This should match a hashable field in the `RegisterClientRequest` message."""
ClientConfigurations = Dict[HashableClientIdentifier, RegisterClientRequest]
