from __future__ import annotations

import math
import PIL.Image
import numpy as np
import utils3d as u3d


class SphericalHarmonics:
    degrees: int
    cef: np.ndarray
    channel_order: str

    def __init__(self, degrees=2, channels=3, channel_order='first'):
        if degrees > 2:
            raise NotImplementedError('Only support degree <= 2')

        self.degrees = degrees
        self.cef = np.zeros(((degrees + 1) ** 2, channels), dtype=np.float32)
        self.channel_order = channel_order

    @property
    def coefficients(self):
        if self.channel_order == 'first':
            return np.moveaxis(self.cef, 0, -1)
        elif self.channel_order == 'last':
            return self.cef
        else:
            raise ValueError('channel_order must be "first" or "last"')

    @coefficients.setter
    def coefficients(self, value):
        self.cef = value

    @staticmethod
    def from_array(sh_coefficients: np.ndarray, channel_order='first'):
        if len(sh_coefficients.shape) == 1:
            if channel_order == 'first':
                sh_coefficients = sh_coefficients.reshape((3, -1))
            else:
                sh_coefficients = sh_coefficients.reshape((-1, 3))

        s_0, s_1 = sh_coefficients.shape

        n_channels = s_0 if channel_order == 'first' else s_1
        n_components = s_1 if channel_order == 'first' else s_0

        degrees = int(math.sqrt(n_components)) - 1
        sh = SphericalHarmonics(degrees=degrees)
        sh.coefficients = sh_coefficients
        sh.channel_order = channel_order

        return sh

    @staticmethod
    def from_sphere_points(points: u3d.PointCloud, degrees=2):
        sh = SphericalHarmonics(degrees=degrees)
        sh.project_sphere_points(points)

        return sh

    def get_batched_basis_at(self, normal_matrix):
        matrix_length = normal_matrix.shape[0]
        x, y, z = normal_matrix[:, 0], normal_matrix[:, 1], normal_matrix[:, 2]
        sh_basis = np.zeros((matrix_length, 9))

        # degree 0
        if self.degrees >= 0:
            sh_basis[:, 0] = 0.282095

        # degree 1
        if self.degrees >= 1:
            sh_basis[:, 1] = 0.488603 * y
            sh_basis[:, 2] = 0.488603 * z
            sh_basis[:, 3] = 0.488603 * x

        # degree 2
        if self.degrees >= 2:
            sh_basis[:, 4] = 1.092548 * x * y
            sh_basis[:, 5] = 1.092548 * y * z
            sh_basis[:, 6] = 0.315392 * (3 * z * z - 1)
            sh_basis[:, 7] = 1.092548 * x * z
            sh_basis[:, 8] = 0.546274 * (x * x - y * y)

        return sh_basis

    def project_sphere_points(self, sphere_points: u3d.PointCloud):
        matrix_sh_basis = self.get_batched_basis_at(
            sphere_points.positions)

        c = np.einsum(
            'ij,ik->ijk',
            matrix_sh_basis,
            sphere_points.features).sum(axis=0)

        norm = (4 * math.pi) / sphere_points.positions.shape[0]
        self.coefficients = c * norm

    def reconstruct(self, canvas_norm: np.ndarray):
        s = canvas_norm.shape

        canvas_norm = canvas_norm.reshape((-1, 3))

        x = canvas_norm[:, 0, np.newaxis]
        y = canvas_norm[:, 1, np.newaxis]
        z = canvas_norm[:, 2, np.newaxis]

        canvas = np.zeros_like(canvas_norm, dtype=np.float32)

        if self.degrees >= 0:
            canvas += self.coefficients[0, :] * 0.886227

        if self.degrees >= 1:
            canvas += self.coefficients[1, :] * 2.0 * 0.511664 * y
            canvas += self.coefficients[2, :] * 2.0 * 0.511664 * z
            canvas += self.coefficients[3, :] * 2.0 * 0.511664 * x

        if self.degrees >= 2:
            canvas += self.coefficients[4, :] * 2.0 * 0.429043 * x * y
            canvas += self.coefficients[5, :] * 2.0 * 0.429043 * y * z
            canvas += self.coefficients[6, :] * 0.743125 * z * z - 0.247708
            canvas += self.coefficients[7, :] * 2.0 * 0.429043 * x * z
            canvas += self.coefficients[8, :] * 0.429043 * (x * x - y * y)

        canvas = canvas.reshape(s)

        return canvas

    def reconstruct_to_canvas(self, canvas: u3d.Canvas = None) -> u3d.Canvas:
        if canvas is None:
            canvas = u3d.canvas_equirectangular_panorama(
                height=128)

        canvas_res = u3d.canvas_equirectangular_panorama(
            height=canvas.data.shape[0])
        canvas_res.data = self.reconstruct(canvas.data)

        return canvas_res

    def vis_as_pil_image(self, canvas: u3d.Canvas = None) -> PIL.Image:
        if canvas is None:
            canvas = u3d.canvas_equirectangular_panorama(
                height=128)

        arr = self.reconstruct(canvas.data)
        img = PIL.Image.fromarray((arr * 255).astype(np.uint8))

        return img


def draw_equirectangular_panorama(sh, canvas_height=128):
    norm_canvas = u3d.canvas_equirectangular_panorama(
        height=canvas_height)
    canvas = sh.reconstruct(norm_canvas.data)

    return canvas


# Convenience
spherical_harmonics = SphericalHarmonics.from_array
spherical_harmonics_from_sphere_points = SphericalHarmonics.from_sphere_points

__all__ = [
    'SphericalHarmonics', 'spherical_harmonics',
    'draw_equirectangular_panorama'
]
