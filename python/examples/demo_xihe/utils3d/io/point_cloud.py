from __future__ import annotations

import numpy as np

from utils3d.io.image import Image
from utils3d.container import Vector2, Vector3
from utils3d.math.transform import euler_rotation_xyz

from typing import Tuple


class PointCloud:
    def __init__(self, positions: np.ndarray, features: np.ndarray):
        self.positions = positions
        self.features = features

    @staticmethod
    def from_array(data: np.ndarray):
        assert len(data.shape) == 2

        positions = data[:, :3]
        features = data[:, 3:]

        return PointCloud(positions, features)

    def __array__(self):
        return np.concatenate(
            (self.positions, self.features),
            axis=-1
        ).astype(np.float32)

    def __getitem__(self, key):
        return PointCloud(self.positions[key], self.features[key])

    def translate(self, vector: Vector3) -> PointCloud:
        return PointCloud(np.array(self.positions - vector, dtype=np.float32), self.features)

    def rotate(self, rotation: Vector3) -> PointCloud:
        pos_rotated = euler_rotation_xyz(self.positions, angels=rotation)
        return PointCloud(pos_rotated, self.features)


class RGBColoredPointCloud(PointCloud):
    def __init__(self, positions: np.ndarray, rgb_colors: np.ndarray):
        assert rgb_colors.shape[-1] == 3
        super().__init__(positions, rgb_colors)
        self.rgb_colors = rgb_colors

    @staticmethod
    def from_array(data: np.ndarray):
        assert len(data.shape) == 2

        positions = data[:, :3]
        features = data[:, 3:]

        return RGBColoredPointCloud(positions, features)

    @property
    def color_dtype(self) -> np.dtype:
        return self.rgb_colors.dtype


# Utils
def point_cloud_util_split(point_cloud: PointCloud) -> Tuple[np.ndarray, np.ndarray]:
    return point_cloud.positions, point_cloud.features


def point_cloud_from_rgbd_fov(
    rgb: Image, depth: Image,
    fov: Vector2, center: Vector2 = None
) -> PointCloud:

    if center is None:
        center = Vector2((rgb.size.x // 2, rgb.size.y // 2))

    dcx = (center.x - rgb.size.x / 2) / rgb.size.x
    dcy = (center.y - rgb.size.x / 2) / rgb.size.y

    x = np.repeat(
        np.arange(rgb.size.x)[np.newaxis, :], rgb.size.y, axis=0)
    x = x / np.max(x) * 2 - 1
    # x = x + dcx

    y = np.repeat(
        np.arange(rgb.size.y)[:, np.newaxis], rgb.size.x, axis=-1)
    y = y / np.max(y) * 2 - 1
    # y = y + dcy

    z = np.array(depth).reshape((depth.size.y, depth.size.x))

    # Assume matterport3d camera view has FoV of 60 degree horizontally
    # which might not be true, but still used in official repo
    # https://github.com/yindaz/PanoBasic/blob/master/demo_matterport.m
    x = z * np.tan(x * fov.x / 2)
    y = z * np.tan(y * fov.y / 2) * -1

    xyz = np.stack((x, y, z), axis=-1)

    xyz = xyz.reshape((-1, 3))
    rgb = np.array(rgb).reshape((-1, 3))

    pc = PointCloud(xyz, rgb)

    return pc


def point_cloud_from_rgbd_intrinsics(rgb, depth, intrinsics):
    raise NotImplementedError('TODO')


# Convenience
def point_cloud(positions, features) -> PointCloud:
    p = PointCloud(positions, features)
    return p


__all__ = [
    'PointCloud', 'RGBColoredPointCloud', 'point_cloud_util_split',
    'point_cloud_from_rgbd_fov', 'point_cloud_from_rgbd_intrinsics',
    'point_cloud'
]
