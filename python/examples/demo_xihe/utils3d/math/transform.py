import numpy as np


def euler_rotation_xyz(vertices, angels=(0, 0, 0)):
    # Euler XYZ rotation matrix

    rx, ry, rz = angels

    R_z = np.array([
        [np.cos(rz), -np.sin(rz), 0],
        [np.sin(rz), np.cos(rz), 0],
        [0, 0, 1]
    ])

    R_y = np.array([
        [np.cos(ry), 0, np.sin(ry)],
        [0, 1, 0],
        [-np.sin(ry), 0, np.cos(ry)]
    ])

    R_x = np.array([
        [1, 0, 0],
        [0, np.cos(rx), -np.sin(rx)],
        [0, np.sin(rx), np.cos(rx)]
    ])

    R = R_z @ R_y @ R_x
    vertices_rotated = np.dot(vertices, R.T)

    return vertices_rotated


def get_euler_rotation_matrix(angels=(0, 0, 0)):
    rx, ry, rz = angels

    R_z = np.array([
        [np.cos(rz), -np.sin(rz), 0],
        [np.sin(rz), np.cos(rz), 0],
        [0, 0, 1]
    ])

    R_y = np.array([
        [np.cos(ry), 0, np.sin(ry)],
        [0, 1, 0],
        [-np.sin(ry), 0, np.cos(ry)]
    ])

    R_x = np.array([
        [1, 0, 0],
        [0, np.cos(rx), -np.sin(rx)],
        [0, np.sin(rx), np.cos(rx)]
    ])

    R = R_z @ R_y @ R_x
    R = R.astype(np.float32)

    return R


# https://en.wikipedia.org/wiki/Cube_mapping
def cube_uv_to_xyz(face, uv) -> np.array:
    uvc = 2.0 * uv - 1.0
    ones = np.ones((uv.shape[0]), dtype=np.float32)

    if face == 0:
        # return np.array([1.0, vc, -uc])
        res = np.stack((ones, uvc[:, 1], -1 * uvc[:, 0]), axis=-1)
    elif face == 1:
        # return np.array([-1.0, vc, uc])
        res = np.stack((-1 * ones, uvc[:, 1], uvc[:, 0]), axis=-1)
    elif face == 2:
        # return np.array([uc, 1.0, -vc])
        res = np.stack((uvc[:, 0], ones, -1 * uvc[:, 1]), axis=-1)
    elif face == 3:
        # return np.array([uc, -1.0, vc])
        res = np.stack((uvc[:, 0], -1 * ones, uvc[:, 1]), axis=-1)
    elif face == 4:
        # return np.array([uc, vc, 1.0])
        res = np.stack((uvc[:, 0], uvc[:, 1], ones), axis=-1)
    elif face == 5:
        # return np.array([-uc, vc, -1.0])
        res = np.stack((-1 * uvc[:, 0], uvc[:, 1], -1 * ones), axis=-1)

    res = res.astype(np.float32)
    return res


__all__ = ['euler_rotation_xyz', 'get_euler_rotation_matrix', 'cube_uv_to_xyz']
