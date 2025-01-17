from collections import defaultdict
from collections.abc import Sequence
from typing import DefaultDict, Tuple

from cakelab.arflow_grpc.v1.color_frame_pb2 import ColorFrame
from cakelab.arflow_grpc.v1.depth_frame_pb2 import DepthFrame
from cakelab.arflow_grpc.v1.xr_cpu_image_pb2 import XRCpuImage


def group_color_frames_by_format_and_dims(
    frames: Sequence[ColorFrame],
) -> DefaultDict[Tuple[XRCpuImage.Format, int, int], list[ColorFrame]]:
    """Group color frames by format and dimensions (width x height)."""
    color_frames_grouped_by_format_and_dims: DefaultDict[
        Tuple[XRCpuImage.Format, int, int], list[ColorFrame]
    ] = defaultdict(list)
    for frame in frames:
        color_frames_grouped_by_format_and_dims[
            (
                frame.image.format,
                frame.image.dimensions.x,
                frame.image.dimensions.y,
            )
        ].append(frame)
    return color_frames_grouped_by_format_and_dims


def group_depth_frames_by_format_dims_and_smoothness(
    frames: Sequence[DepthFrame],
) -> DefaultDict[Tuple[XRCpuImage.Format, int, int, bool], list[DepthFrame]]:
    """Group depth frames by format, dimensions (width x height), and smoothness."""
    depth_frames_grouped_by_format_dims_and_smoothness: DefaultDict[
        Tuple[XRCpuImage.Format, int, int, bool], list[DepthFrame]
    ] = defaultdict(list)
    for frame in frames:
        depth_frames_grouped_by_format_dims_and_smoothness[
            (
                frame.image.format,
                frame.image.dimensions.x,
                frame.image.dimensions.y,
                frame.environment_depth_temporal_smoothing_enabled,
            )
        ].append(frame)
    return depth_frames_grouped_by_format_dims_and_smoothness
