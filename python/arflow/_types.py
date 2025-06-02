"""Type definitions for ARFlow."""

from __future__ import annotations

from enum import StrEnum


class ARFrameType(StrEnum):
    """This must always match with the names in the `oneof` field defined in `ARFrame` proto schema."""

    TRANSFORM_FRAME = "transform_frame"
    COLOR_FRAME = "color_frame"
    DEPTH_FRAME = "depth_frame"
    GYROSCOPE_FRAME = "gyroscope_frame"
    AUDIO_FRAME = "audio_frame"
    PLANE_DETECTION_FRAME = "plane_detection_frame"
    POINT_CLOUD_DETECTION_FRAME = "point_cloud_detection_frame"
    MESH_DETECTION_FRAME = "mesh_detection_frame"


class Timeline(StrEnum):
    DEVICE = "device_timestamp"
    IMAGE = "image_timestamp"
