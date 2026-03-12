import math

import numpy as np
import PIL.Image

import utils3d as u3d


class Canvas:
    def __init__(self, width, height, channels=3):
        self.data = np.zeros((height, width, channels), dtype=np.float32)

    def clear(self):
        self.data *= 0

    def to_pil_image(self):
        data_rgb = (self.data.astype(np.float32) + 1) / 2 * 255
        data_rgb = data_rgb.astype(np.uint8)
        return PIL.Image.fromarray(data_rgb, mode="RGB")


# Convenience
def canvas(width, height, channels=3):
    c = Canvas(width, height, channels=channels)
    return c


def canvas_equirectangular_panorama(height, channels=3):
    c = Canvas(height * 2, height, channels=channels)

    u = np.arange(height * 2, dtype="int")
    v = np.arange(height, dtype="int")
    uv = np.stack(np.meshgrid(u, v), axis=-1)

    uv_xyz = u3d.equirectangular_uv_to_cartesian(uv)
    uv_xyz = u3d.euler_rotation_xyz(uv_xyz, (math.radians(-90), math.radians(0), 0))
    uv_xyz = u3d.euler_rotation_xyz(uv_xyz, (math.radians(0), math.radians(90), 0))

    c = Canvas(height * 2, height)
    c.data = uv_xyz.reshape((height, height * 2, 3))

    return c


def draw_point_cloud_on_equirectangular(canvas, point_cloud):
    canvas_height = canvas.data.shape[0]

    xyz, rgb = u3d.point_cloud_util_split(point_cloud)
    uv = u3d.cartesian_to_equirectangular_uv(xyz, canvas_height)

    idx_u, idx_v = uv[..., 0], uv[..., 1]
    idx = idx_v * canvas_height * 2 + idx_u

    s = canvas.data.shape
    canvas.data = canvas.data.reshape((-1, 3))

    np.add.at(canvas.data, idx, rgb)

    canvas.data = canvas.data.reshape(s)

    return canvas


__all__ = [
    "Canvas",
    "canvas",
    "canvas_equirectangular_panorama",
    "draw_point_cloud_on_equirectangular",
]
