"""Data deserialization tests."""

# ruff:noqa: D103,D107
# pyright: reportPrivateUsage=false

from typing import Literal

import numpy as np
import pytest

from arflow._core import (
    _convert_2d_to_3d,
    _decode_depth_image,
    _decode_intrinsic,
    _decode_point_cloud,
    _decode_rgb_image,
    _decode_transform,
)
from arflow._types import (
    PlaneBoundaryPoints,
    PlaneCenter,
    PlaneNormal,
)


@pytest.mark.parametrize(
    "resolution_y,resolution_x,resize_factor_y,resize_factor_x,data_type,buffer_length,should_pass",
    [
        (4, 4, 1.0, 1.0, "RGB24", 4 * 4 * 3, True),  # Valid RGB24 case
        (4, 4, 1.0, 1.0, "YCbCr420", 4 * 4 * 3 // 2, True),  # Valid YCbCr420 case
        (4, 4, 1.0, 1.0, "Invalid", 4 * 4 * 3, False),  # Invalid data type
        (4, 4, 1.0, 1.0, "RGB24", 1, False),  # Buffer too small
    ],
)
def test_decode_rgb_image(
    resolution_y: int,
    resolution_x: int,
    resize_factor_y: float,
    resize_factor_x: float,
    data_type: Literal["RGB24", "YCbCr420"],
    buffer_length: int,
    should_pass: bool,
):
    buffer = np.random.randint(0, 255, buffer_length, dtype=np.uint8).tobytes()  # pyright: ignore [reportUnknownMemberType]
    if should_pass:
        assert _decode_rgb_image(
            resolution_y,
            resolution_x,
            resize_factor_y,
            resize_factor_x,
            data_type,
            buffer,
        ).shape == (
            resolution_y,
            resolution_x,
            3,
        )
        assert (
            _decode_rgb_image(
                resolution_y,
                resolution_x,
                resize_factor_y,
                resize_factor_x,
                data_type,
                buffer,
            ).dtype
            == np.uint8
        )
    else:
        with pytest.raises(ValueError):
            _decode_rgb_image(
                resolution_y,
                resolution_x,
                resize_factor_y,
                resize_factor_x,
                data_type,
                buffer,
            )


@pytest.mark.parametrize(
    "resolution_y,resolution_x,data_type,buffer_dtype,should_pass",
    [
        (4, 4, "f32", np.float32, True),  # Valid float32 depth case
        (4, 4, "u16", np.uint16, True),  # Valid uint16 depth case
        (4, 4, "Invalid", np.uint16, False),  # Invalid data type
        (4, 4, "f32", np.float32, False),  # Buffer too small
    ],
)
def test_decode_depth_image(
    resolution_y: int,
    resolution_x: int,
    data_type: Literal["f32", "u16"],
    buffer_dtype: np.float32 | np.uint16,
    should_pass: bool,
):
    buffer = np.random.rand(resolution_y * resolution_x).astype(buffer_dtype).tobytes()

    if should_pass:
        result = _decode_depth_image(resolution_y, resolution_x, data_type, buffer)
        assert result.shape == (resolution_y, resolution_x)
        assert result.dtype == np.float32
    else:
        with pytest.raises(ValueError):
            _decode_depth_image(resolution_y, resolution_x, data_type, buffer[:1])


@pytest.mark.parametrize(
    "buffer_length,should_pass",
    [
        (12 * 4, True),  # Correct size for 3x4 matrix
        (8, False),  # Incorrect buffer size
    ],
)
def test_decode_transform(buffer_length: int, should_pass: bool):
    buffer = np.random.rand(buffer_length // 4).astype(np.float32).tobytes()

    if should_pass:
        result = _decode_transform(buffer)
        assert result.shape == (4, 4)
        assert result.dtype == np.float32
    else:
        with pytest.raises(ValueError):
            _decode_transform(buffer)


@pytest.mark.parametrize(
    "resize_factor_y,resize_factor_x,focal_length_y,focal_length_x,principal_point_y,principal_point_x,should_pass",
    [
        (1.0, 1.0, 2.0, 2.0, 1.0, 1.0, True),  # Valid intrinsic matrix
        # TODO: Really no error cases?
    ],
)
def test_decode_intrinsic(
    resize_factor_y: float,
    resize_factor_x: float,
    focal_length_y: float,
    focal_length_x: float,
    principal_point_y: float,
    principal_point_x: float,
    should_pass: bool,
):
    if should_pass:
        result = _decode_intrinsic(
            resize_factor_y,
            resize_factor_x,
            focal_length_y,
            focal_length_x,
            principal_point_y,
            principal_point_x,
        )
        assert result.shape == (3, 3)
        assert result.dtype == np.float32
    else:
        with pytest.raises(ValueError):
            _decode_intrinsic(
                resize_factor_y,
                resize_factor_x,
                focal_length_y,
                focal_length_x,
                principal_point_y,
                principal_point_x,
            )


@pytest.mark.parametrize(
    "resolution_y,resolution_x,resize_factor_y,resize_factor_x,valid_transform,should_pass",
    [
        (4, 4, 1.0, 1.0, True, True),  # Valid point cloud case
        (4, 4, 1.0, 1.0, False, False),  # Invalid transformation matrix
    ],
)
def test_decode_point_cloud(
    resolution_y: int,
    resolution_x: int,
    resize_factor_y: float,
    resize_factor_x: float,
    valid_transform: bool,
    should_pass: bool,
):
    color_rgb = np.random.randint(  # pyright: ignore [reportUnknownMemberType]
        0, 255, (resolution_y, resolution_x, 3), dtype=np.uint8
    )
    depth_img = np.random.rand(resolution_y, resolution_x).astype(np.float32)
    k = np.array([[2.0, 0, 1.0], [0, 2.0, 1.0], [0, 0, 1.0]], dtype=np.float32)
    transform = (
        np.eye(4, dtype=np.float32) if valid_transform else np.eye(3, dtype=np.float32)
    )

    if should_pass:
        pcd, clr = _decode_point_cloud(
            resolution_y,
            resolution_x,
            resize_factor_y,
            resize_factor_x,
            k,
            color_rgb,
            depth_img,
            transform,
        )
        assert pcd.shape == (resolution_y * resolution_x, 3)
        assert pcd.dtype == np.float32
        assert clr.shape == (resolution_y * resolution_x, 3)
        assert clr.dtype == np.uint8
    else:
        with pytest.raises(IndexError):
            _decode_point_cloud(
                resolution_y,
                resolution_x,
                resize_factor_y,
                resize_factor_x,
                k,
                color_rgb,
                depth_img,
                transform,
            )


@pytest.mark.parametrize(
    "boundary_points_2d,normal,center,should_pass",
    [
        (
            np.array([[1, 2], [2, 3], [1, 3]]),
            np.array([4, 5, 6]),
            np.array([2, 3, 4]),
            True,
        ),  # Valid 2D points, normal, and center
        (
            np.array([[1, 2], [2, 3]]),
            np.array([4, 5, 6]),
            np.array([2, 3, 4]),
            False,
        ),  # Invalid 2D points
        (
            np.array([[1, 2], [2, 3], [1, 3]]),
            np.array([[2, 3], [4, 5]]),
            np.array([2, 3, 4]),
            False,
        ),  # Invalid normal
        (
            np.array([[1, 2], [2, 3], [1, 3]]),
            np.array([4, 5, 6]),
            np.array([[2, 3], [3, 4]]),
            False,
        ),  # Invalid center
    ],
)
def test_convert_2d_points_to_3d(
    boundary_points_2d: PlaneBoundaryPoints,
    normal: PlaneNormal,
    center: PlaneCenter,
    should_pass: bool,
):
    if should_pass:
        result = _convert_2d_to_3d(boundary_points_2d, normal, center)
        assert result.shape[0] == (boundary_points_2d.shape[0])
        assert result.shape[1] == 3
        assert result.dtype == np.float64

    else:
        with pytest.raises(ValueError):
            result = _convert_2d_to_3d(boundary_points_2d, normal, center)
