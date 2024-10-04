"""Type definitions for ARFlow."""

from __future__ import annotations

from dataclasses import dataclass
from typing import Dict, List, NewType

import numpy as np
import numpy.typing as npt

from arflow_grpc.service_pb2 import ClientConfiguration, DataFrame

ARFlowRequest = DataFrame | ClientConfiguration


@dataclass
class EnrichedARFlowRequest:
    """An enriched ARFlow request."""

    timestamp: float
    """The timestamp of the request."""
    data: ARFlowRequest
    """The ARFlow request data."""


ColorRGB = npt.NDArray[np.uint8]
DepthImg = npt.NDArray[np.float32]
Transform = npt.NDArray[np.float32]
Intrinsic = npt.NDArray[np.float32]
PointCloudPCD = npt.NDArray[np.float32]
PointCloudCLR = npt.NDArray[np.uint8]


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
"""This should match a hashable field in the `ClientConfiguration` message."""
ClientConfigurations = Dict[HashableClientIdentifier, ClientConfiguration]
