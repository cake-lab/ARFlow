import math

import numpy as np


def cartesian_to_spherical(points):
    x = points[:, 0]
    y = points[:, 1]
    z = points[:, 2]

    r = np.linalg.norm(points, axis=-1)
    phi = np.arctan2(y, x)
    theta = np.arccos(z / r)

    return np.stack([theta, phi, r], axis=-1)


def spherical_to_cartesian(points):
    theta = points[:, 0]
    phi = points[:, 1]
    r = points[:, 2]

    x = r * np.sin(theta) * np.cos(phi)
    y = r * np.sin(theta) * np.sin(phi)
    z = r * np.cos(theta)

    return np.stack((x, y, z), axis=-1)


def cartesian_to_equirectangular_uv(points, canvas_height):
    vector_xyz_py = np.array([0, 1, 0], dtype=np.float32)
    vector_xz_pz = np.array([0, -1], dtype=np.float32)

    points_xz = np.stack((points[:, 0], points[:, 2]), axis=-1)
    points_xz_norm = np.linalg.norm(points_xz, axis=-1)
    points_xz_norm[points_xz_norm == 0] = 1
    points_xz = points_xz / points_xz_norm[:, np.newaxis]

    theta = np.arccos(points @ vector_xyz_py)

    phi = np.arctan2(points_xz[:, 1], points_xz[:, 0]) - np.arctan2(
        vector_xz_pz[1], vector_xz_pz[0]
    )
    phi[phi < 0] += 2 * math.pi
    phi[np.isnan(phi)] = math.pi
    phi = 2 * math.pi - phi

    u = phi / math.radians(360) * canvas_height * 2
    u = u.astype("int")

    v = theta / (math.radians(180) + np.finfo(np.float32).eps) * canvas_height
    v = v.astype("int")
    v[v == canvas_height] = canvas_height - 1

    uv = np.stack((u, v), axis=-1)

    return uv


def equirectangular_uv_to_cartesian(uv):
    u, v = uv[:, :, 0], uv[:, :, 1]

    u = (u / np.max(u) - 0.5) * 2  # [-1, 1]
    phi = u * math.radians(180)
    phi = phi.reshape((-1))

    v = v / np.max(v)  # [0, 1]
    v = v * math.radians(180)
    theta = v.reshape((-1))

    r = np.ones_like(theta, dtype=np.float32)

    coord_spherical = np.stack((theta, phi, r), axis=-1)
    coord_cartesian = spherical_to_cartesian(coord_spherical)

    return coord_cartesian
