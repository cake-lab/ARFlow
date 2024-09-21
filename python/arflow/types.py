from typing import Any, Dict, List, NewType, TypedDict

import numpy as np
from numpy.typing import NDArray

from arflow.service_pb2 import ClientConfiguration, ClientIdentifier, DataFrame

Timestamp = NewType("Timestamp", float)
ARFlowRequest = DataFrame | ClientConfiguration


class EnrichedARFlowRequest(TypedDict):
    timestamp: Timestamp
    data: ARFlowRequest


RequestsHistory = List[EnrichedARFlowRequest]
ClientConfigurations = Dict[ClientIdentifier, ClientConfiguration]

ColorRGB = NDArray[np.uint8]
DepthImg = NDArray[np.float32] | np.ndarray[Any, np.dtype[np.float32 | np.uint16]]
Transform = NDArray[np.float32]
Intrinsic = NDArray[np.float32]
PointCloudPCD = NDArray[np.float32]
PointCloudCLR = NDArray[np.float32]


class DecodedDataFrame(TypedDict, total=False):
    color_rgb: ColorRGB
    depth_img: DepthImg
    transform: Transform
    point_cloud_pcd: PointCloudPCD
    point_cloud_clr: PointCloudCLR
