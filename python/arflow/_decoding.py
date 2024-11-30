from typing import Iterable

import numpy as np

from arflow._types import (
    DecodedColorFrames,
    DecodedDepthFrames,
)
from cakelab.arflow_grpc.v1.xr_cpu_image_pb2 import XRCpuImage


def decode_color_frames(
    raw_planes: list[Iterable[XRCpuImage.Plane]],
    format: XRCpuImage.Format,
) -> DecodedColorFrames:
    """Decode the color frames from the raw frames. Assumes that format, width, and height are the same across all frames.

    Raises:
        ValueError: If the frames are not in a supported format or is in bad shape.
    """
    if (
        format != XRCpuImage.FORMAT_IOS_YP_CBCR_420_8BI_PLANAR_FULL_RANGE
        or format != XRCpuImage.FORMAT_ANDROID_YUV_420_888
    ):
        raise ValueError(f"Unsupported frame format: {format}")
    decoded_frames = np.array(
        [np.concatenate([plane.data for plane in planes]) for planes in raw_planes]
    )
    return decoded_frames


def decode_depth_frames(
    raw_planes: list[Iterable[XRCpuImage.Plane]],
    format: XRCpuImage.Format,
    width: int,
    height: int,
) -> DecodedDepthFrames:
    """Decode the depth image from the buffer.

    Raises:
        ValueError: If the data type is not recognized.
    """
    if format == XRCpuImage.FORMAT_DEPTHFLOAT32:
        dtype = np.float32
    elif format == XRCpuImage.FORMAT_DEPTHUINT16:
        dtype = np.uint16
    else:
        raise ValueError(f"Unsupported frame format: {format}")

    decoded_frames = np.array(
        [
            np.frombuffer(np.concatenate([plane.data for plane in planes]), dtype=dtype)
            for planes in raw_planes
        ]
    ).reshape(-1, height, width)
    return decoded_frames


# def decode_transform(buffer: bytes) -> Transform:
#     y_down_to_y_up = np.array(
#         [
#             [1.0, -0.0, 0.0, 0],
#             [0.0, -1.0, 0.0, 0],
#             [0.0, 0.0, 1.0, 0],
#             [0.0, 0.0, 0, 1.0],
#         ],
#         dtype=np.float32,
#     )
#
#     t = np.frombuffer(buffer, dtype=np.float32)
#     transform = np.eye(4, dtype=np.float32)
#     transform[:3, :] = t.reshape((3, 4))
#     transform[:3, 3] = 0
#     transform = y_down_to_y_up @ transform
#
#     return transform.astype(np.float32)
#
#
# def decode_intrinsic(
#     resize_factor_y: float,
#     resize_factor_x: float,
#     focal_length_y: float,
#     focal_length_x: float,
#     principal_point_y: float,
#     principal_point_x: float,
# ) -> Intrinsic:
#     sx = resize_factor_x
#     sy = resize_factor_y
#
#     fx, fy = (
#         focal_length_x * sx,
#         focal_length_y * sy,
#     )
#     cx, cy = (
#         principal_point_x * sx,
#         principal_point_y * sy,
#     )
#
#     k = np.array([[fx, 0, cx], [0, fy, cy], [0, 0, 1]], dtype=np.float32)
#
#     return k
#
#
# def decode_point_cloud(
#     resolution_y: int,
#     resolution_x: int,
#     resize_factor_y: float,
#     resize_factor_x: float,
#     k: Intrinsic,
#     color_rgb: ColorRGB,
#     depth_img: DepthImg,
#     transform: Transform,
# ) -> tuple[PointCloudPCD, PointCloudCLR]:
#     # Flip image is needed for point cloud generation.
#     color_rgb = np.flipud(color_rgb)
#     depth_img = np.flipud(depth_img)
#
#     color_img_w = int(resolution_x * resize_factor_x)
#     color_img_h = int(resolution_y * resize_factor_y)
#
#     u, v = np.meshgrid(np.arange(color_img_w), np.arange(color_img_h))
#
#     fx: np.float32 = k[0, 0]
#     fy: np.float32 = k[1, 1]
#     cx: np.float32 = k[0, 2]
#     cy: np.float32 = k[1, 2]
#
#     z = depth_img.copy()
#     x = ((u - cx) * z) / fx
#     y = ((v - cy) * z) / fy
#     pre_pcd = np.stack([x, y, z], axis=-1).reshape(-1, 3)
#     pcd = np.matmul(transform[:3, :3], pre_pcd.T).T + transform[:3, 3]
#     clr = color_rgb.reshape(-1, 3)
#
#     return pcd.astype(np.float32), clr
#
#
# def convert_2d_to_3d_boundary_points(
#     boundary_points_2d: PlaneBoundaryPoints2D, normal: PlaneNormal, center: PlaneCenter
# ) -> PlaneBoundaryPoints3D:
#     # Check boundary points validity
#     if boundary_points_2d.shape[0] < 3:
#         raise ValueError("At least 3 boundary points are required")
#     if boundary_points_2d.shape[1] != 2:
#         raise ValueError("Boundary points should be in 2D")
#
#     # Check normal validity
#     if len(normal.shape) != 1:
#         raise ValueError("There should only be 1 normal")
#     if normal.shape[0] != 3:
#         raise ValueError("Normal should be in 3D")
#     if np.linalg.norm(normal) == 0:
#         raise ValueError("Normal should be non-zero")
#
#     # Check center validity
#     if len(center.shape) != 1:
#         raise ValueError("There should only be 1 center")
#     if center.shape[0] != 3:
#         raise ValueError("Center should be in 3D")
#
#     # Ensure the normal is normalized
#     normal = normal / np.linalg.norm(normal)
#
#     # Generate two orthogonal vectors (u and v) that lie on the plane
#     # Find a vector that is not parallel to the normal
#     arbitrary_vector = (
#         np.array([1, 0, 0])
#         if not np.allclose(normal, [1, 0, 0])
#         else np.array([0, 1, 0])
#     )
#
#     # Create u vector, which is perpendicular to the normal
#     u = np.cross(normal, arbitrary_vector)
#     u = u / np.linalg.norm(u)
#
#     # Create v vector, which is perpendicular to both the normal and u
#     v = np.cross(normal, u)
#
#     # Convert the 2D points into 3D
#     # Each 2D point can be written as a linear combination of u and v, plus the center
#     boundary_points_3d = np.array(
#         [center + point_2d[0] * u + point_2d[1] * v for point_2d in boundary_points_2d]
#     )
#
#     return np.array(boundary_points_3d, dtype=np.float32)
