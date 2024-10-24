import numpy as np

from arflow._types import (
    ColorDataType,
    ColorRGB,
    DepthDataType,
    DepthImg,
    Intrinsic,
    PlaneBoundaryPoints2D,
    PlaneBoundaryPoints3D,
    PlaneCenter,
    PlaneNormal,
    PointCloudCLR,
    PointCloudPCD,
    Transform,
)


def decode_rgb_image(
    resolution_y: int,
    resolution_x: int,
    resize_factor_y: float,
    resize_factor_x: float,
    data_type: ColorDataType,
    buffer: bytes,
) -> ColorRGB:
    """Decode the color image from the buffer.

    Raises:
        ValueError: If the data type is not recognized
    """
    # Calculate the size of the image.
    color_img_w = int(resolution_x * resize_factor_x)
    color_img_h = int(resolution_y * resize_factor_y)
    p = color_img_w * color_img_h
    color_img = np.frombuffer(buffer, dtype=np.uint8)

    # Decode RGB bytes into RGB.
    if data_type == "RGB24":
        color_rgb = color_img.reshape((color_img_h, color_img_w, 3))

    # Decode YCbCr bytes into RGB.
    elif data_type == "YCbCr420":
        y = color_img[:p].reshape((color_img_h, color_img_w))
        cbcr = color_img[p:].reshape((color_img_h // 2, color_img_w // 2, 2))
        cb, cr = cbcr[:, :, 0], cbcr[:, :, 1]

        # Very important! Convert to float32 first!
        cb = np.repeat(cb, 2, axis=0).repeat(2, axis=1).astype(np.float32) - 128
        cr = np.repeat(cr, 2, axis=0).repeat(2, axis=1).astype(np.float32) - 128

        r = np.clip(y + 1.403 * cr, 0, 255)
        g = np.clip(y - 0.344 * cb - 0.714 * cr, 0, 255)
        b = np.clip(y + 1.772 * cb, 0, 255)

        color_rgb = np.stack([r, g, b], axis=-1)

    else:
        raise ValueError(f"Unknown data type: {data_type}")

    return color_rgb.astype(np.uint8)


def decode_depth_image(
    resolution_y: int,
    resolution_x: int,
    data_type: DepthDataType,
    buffer: bytes,
) -> DepthImg:
    """Decode the depth image from the buffer.

    Args:
        data_type: `f32` for iOS, `u16` for Android.

    Raises:
        ValueError: If the data type is not recognized.
    """
    # The `Any` means that the array can have any shape. We cannot
    # determine the shape of the array from the buffer.
    if data_type == "f32":
        dtype = np.float32
    elif data_type == "u16":
        dtype = np.uint16
    else:
        raise ValueError(f"Unknown data type: {data_type}")

    depth_img = np.frombuffer(buffer, dtype=dtype).reshape(
        (
            resolution_y,
            resolution_x,
        )
    )

    # If it's a 16-bit unsigned integer, convert to float32 and scale to meters.
    if dtype == np.uint16:
        depth_img = (depth_img.astype(np.float32) / 1000.0).astype(np.float32)

    return depth_img.astype(np.float32)


def decode_transform(buffer: bytes) -> Transform:
    y_down_to_y_up = np.array(
        [
            [1.0, -0.0, 0.0, 0],
            [0.0, -1.0, 0.0, 0],
            [0.0, 0.0, 1.0, 0],
            [0.0, 0.0, 0, 1.0],
        ],
        dtype=np.float32,
    )

    t = np.frombuffer(buffer, dtype=np.float32)
    transform = np.eye(4, dtype=np.float32)
    transform[:3, :] = t.reshape((3, 4))
    transform[:3, 3] = 0
    transform = y_down_to_y_up @ transform

    return transform.astype(np.float32)


def decode_intrinsic(
    resize_factor_y: float,
    resize_factor_x: float,
    focal_length_y: float,
    focal_length_x: float,
    principal_point_y: float,
    principal_point_x: float,
) -> Intrinsic:
    sx = resize_factor_x
    sy = resize_factor_y

    fx, fy = (
        focal_length_x * sx,
        focal_length_y * sy,
    )
    cx, cy = (
        principal_point_x * sx,
        principal_point_y * sy,
    )

    k = np.array([[fx, 0, cx], [0, fy, cy], [0, 0, 1]], dtype=np.float32)

    return k


def decode_point_cloud(
    resolution_y: int,
    resolution_x: int,
    resize_factor_y: float,
    resize_factor_x: float,
    k: Intrinsic,
    color_rgb: ColorRGB,
    depth_img: DepthImg,
    transform: Transform,
) -> tuple[PointCloudPCD, PointCloudCLR]:
    # Flip image is needed for point cloud generation.
    color_rgb = np.flipud(color_rgb)
    depth_img = np.flipud(depth_img)

    color_img_w = int(resolution_x * resize_factor_x)
    color_img_h = int(resolution_y * resize_factor_y)

    u, v = np.meshgrid(np.arange(color_img_w), np.arange(color_img_h))

    fx: np.float32 = k[0, 0]
    fy: np.float32 = k[1, 1]
    cx: np.float32 = k[0, 2]
    cy: np.float32 = k[1, 2]

    z = depth_img.copy()
    x = ((u - cx) * z) / fx
    y = ((v - cy) * z) / fy
    pre_pcd = np.stack([x, y, z], axis=-1).reshape(-1, 3)
    pcd = np.matmul(transform[:3, :3], pre_pcd.T).T + transform[:3, 3]
    clr = color_rgb.reshape(-1, 3)

    return pcd.astype(np.float32), clr


def convert_2d_to_3d_boundary_points(
    boundary_points_2d: PlaneBoundaryPoints2D, normal: PlaneNormal, center: PlaneCenter
) -> PlaneBoundaryPoints3D:
    # Check boundary points validity
    if boundary_points_2d.shape[1] != 2:
        raise ValueError("Boundary points should be in 2D")
    if boundary_points_2d.shape[0] < 3:
        raise ValueError("At least 3 boundary points are required")

    # Check normal validity
    if len(normal.shape) != 1:
        raise ValueError("There should only be 1 normal")
    if normal.shape[0] != 3:
        raise ValueError("Normal should be in 3D")
    if np.linalg.norm(normal) == 0:
        raise ValueError("Normal should be non-zero")

    # Check center validity
    if len(center.shape) != 1:
        raise ValueError("There should only be 1 center")
    if center.shape[0] != 3:
        raise ValueError("Center should be in 3D")

    # Ensure the normal is normalized
    normal = normal / np.linalg.norm(normal)

    # Generate two orthogonal vectors (u and v) that lie on the plane
    # Find a vector that is not parallel to the normal
    arbitrary_vector = (
        np.array([1, 0, 0])
        if not np.allclose(normal, [1, 0, 0])
        else np.array([0, 1, 0])
    )

    # Create u vector, which is perpendicular to the normal
    u = np.cross(normal, arbitrary_vector)
    u = u / np.linalg.norm(u)

    # Create v vector, which is perpendicular to both the normal and u
    v = np.cross(normal, u)

    # Convert the 2D points into 3D
    # Each 2D point can be written as a linear combination of u and v, plus the center
    boundary_points_3d = np.array(
        [center + point_2d[0] * u + point_2d[1] * v for point_2d in boundary_points_2d]
    )

    return np.array(boundary_points_3d, dtype=np.float32)
