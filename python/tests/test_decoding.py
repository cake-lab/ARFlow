"""Data deserialization tests."""

# ruff:noqa: D103,D107
# pyright: reportPrivateUsage=false

from typing import Literal

import numpy as np
import pytest

from arflow._decoding import (
    convert_2d_to_3d_boundary_points,
    decode_depth_image,
    decode_intrinsic,
    decode_point_cloud,
    decode_rgb_image,
    decode_transform,
)
from arflow._types import (
    PlaneBoundaryPoints2D,
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
        assert decode_rgb_image(
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
            decode_rgb_image(
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
            decode_rgb_image(
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
        result = decode_depth_image(resolution_y, resolution_x, data_type, buffer)
        assert result.shape == (resolution_y, resolution_x)
        assert result.dtype == np.float32
    else:
        with pytest.raises(ValueError):
            decode_depth_image(resolution_y, resolution_x, data_type, buffer[:1])


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
        result = decode_transform(buffer)
        assert result.shape == (4, 4)
        assert result.dtype == np.float32
    else:
        with pytest.raises(ValueError):
            decode_transform(buffer)


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
        result = decode_intrinsic(
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
            decode_intrinsic(
                resize_factor_y,
                resize_factor_x,
                focal_length_y,
                focal_length_x,
                principal_point_y,
                principal_point_x,
            )


@pytest.mark.parametrize(
    "resolution_y,resolution_x,resize_factor_y,resize_factor_x,should_pass",
    [
        (4, 4, 1.0, 1.0, True),  # Valid point cloud case
        # TODO: Really no error cases? Because we can assume color_rgb, depth_img, and k are valid
    ],
)
def test_decode_point_cloud(
    resolution_y: int,
    resolution_x: int,
    resize_factor_y: float,
    resize_factor_x: float,
    should_pass: bool,
):
    color_rgb = np.random.randint(  # pyright: ignore [reportUnknownMemberType]
        0, 255, (resolution_y, resolution_x, 3), dtype=np.uint8
    )
    depth_img = np.random.rand(resolution_y, resolution_x).astype(np.float32)
    k = np.array([[2.0, 0, 1.0], [0, 2.0, 1.0], [0, 0, 1.0]], dtype=np.float32)
    transform = np.eye(4, dtype=np.float32)

    if should_pass:
        pcd, clr = decode_point_cloud(
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
        with pytest.raises(ValueError):
            decode_point_cloud(
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
            np.array([[1, 2, 3], [2, 3, 4], [1, 3, 4]]),
            np.array([4, 5, 6]),
            np.array([2, 3, 4]),
            False,
        ),  # Boundary points not in 2D
        (
            np.array([[1, 2], [2, 3]]),
            np.array([4, 5, 6]),
            np.array([2, 3, 4]),
            False,
        ),  # Only 2 boundary points
        (
            np.array([[1, 2], [2, 3], [1, 3]]),
            np.array([[2, 3, 4], [4, 5, 6]]),
            np.array([2, 3, 4]),
            False,
        ),  # More than 1 normal
        (
            np.array([[1, 2], [2, 3], [1, 3]]),
            np.array([2, 3]),
            np.array([2, 3, 4]),
            False,
        ),  # Normal not in 3D
        (
            np.array([[1, 2], [2, 3], [1, 3]]),
            np.array([0, 0, 0]),
            np.array([2, 3, 4]),
            False,
        ),  # Normal is zero
        (
            np.array([[1, 2], [2, 3], [1, 3]]),
            np.array([4, 5, 6]),
            np.array([[2, 3, 4], [2, 3, 4]]),
            False,
        ),  # More than 1 center
        (
            np.array([[1, 2], [2, 3], [1, 3]]),
            np.array([4, 5, 6]),
            np.array([2, 3, 4, 5]),
            False,
        ),  # Center not in 3D
    ],
)
def test_convert_2d_to_3d_boundary_points(
    boundary_points_2d: PlaneBoundaryPoints2D,
    normal: PlaneNormal,
    center: PlaneCenter,
    should_pass: bool,
):
    if should_pass:
        result = convert_2d_to_3d_boundary_points(boundary_points_2d, normal, center)
        assert result.shape[0] == (boundary_points_2d.shape[0])
        assert result.shape[1] == 3
        assert result.dtype == np.float32

    else:
        with pytest.raises(ValueError):
            result = convert_2d_to_3d_boundary_points(
                boundary_points_2d, normal, center
            )
