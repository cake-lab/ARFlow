"""Data deserialization tests."""

# ruff:noqa: D103,D107
# pyright: reportPrivateUsage=false
from collections.abc import Sequence

import numpy as np
import pytest

from arflow._session_stream import _convert_2d_to_3d_boundary_points
from cakelab.arflow_grpc.v1.vector2_pb2 import Vector2
from cakelab.arflow_grpc.v1.vector3_pb2 import Vector3


@pytest.mark.parametrize(
    "boundary,normal,center",
    [
        (
            [
                Vector2(x=1, y=2),
                Vector2(x=2, y=3),
                Vector2(x=1, y=3),
            ],
            Vector3(x=4, y=5, z=6),
            Vector3(x=2, y=3, z=4),
        ),  # Valid 2D points, normal, and center
        (
            [],
            Vector3(x=4, y=5, z=6),
            Vector3(x=2, y=3, z=4),
        ),  # Empty boundary
    ],
)
def test_convert_2d_to_3d_boundary_points(
    boundary: Sequence[Vector2],
    normal: Vector3,
    center: Vector3,
):
    result = _convert_2d_to_3d_boundary_points(boundary, normal, center)
    if len(boundary) == 0:
        np.testing.assert_array_equal(result, np.array([], dtype=np.float32))
        return
    np.testing.assert_array_equal(
        result,
        np.array(
            [
                [0.21987888, 4.3518677, 4.060191],
                [-0.6701817, 5.411912, 3.7701945],
                [-0.6701817, 4.6436906, 4.410379],
                [0.21987888, 4.3518677, 4.060191],  # Closing the boundary
            ],
            dtype=np.float32,
        ),
    )
