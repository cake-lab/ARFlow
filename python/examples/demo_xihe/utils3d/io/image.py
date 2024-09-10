from __future__ import annotations

import base64
import imageio

import numpy as np
import utils3d as u3d

from typing import Tuple


class Image:
    _data: np.ndarray

    def __init__(self, data=None):
        self._data = data

    def __array__(self):
        return self._data

    @staticmethod
    def from_file_path(file_path) -> Image:
        return Image(imageio.imread(file_path))

    @staticmethod
    def from_base64(b64_string: str, dims: Tuple, channels: int, dtype=np.float32) -> Image:
        data = np.frombuffer(base64.decodebytes(
            b64_string.encode('utf-8')), dtype=dtype)
        data = data.reshape((dims[1], dims[0], channels))
        return Image(data)

    @property
    def size(self) -> u3d.Vector2:
        return u3d.Vector2((self._data.shape[1], self._data.shape[0]))

    def normalize(self) -> Image:
        return Image(np.array(self._data / np.iinfo(self._data.dtype).max, dtype=np.float32))

    def flip_x(self) -> Image:
        return Image(np.flip(self._data, axis=1))


# Convenience methods
image = Image
image_from_base64 = Image.from_base64

__all__ = ['Image', 'image', 'image_from_base64']
