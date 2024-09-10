from __future__ import annotations

import numpy as np
import utils3d as u3d
from utils3d.utils.typing import is_sequence_instance

from typing import Sequence


class Matrix:
    _data: np.ndarray

    def __init__(self, source, copy=False):
        if type(source) is np.ndarray:
            self._data = np.array(source, copy=copy)
        elif is_sequence_instance(source, u3d.Vector):
            self._data = np.concatenate(source)

    @staticmethod
    def zeros(shape: tuple, dtype=np.float32) -> Matrix:
        data = np.zeros(shape, dtype=dtype)
        return Matrix(data)

    def __array__(self) -> np.ndarray:
        return self._data

    @property
    def shape(self) -> Sequence[int]:
        return self._data.shape

    def transpose(self):
        return self._data.T


zeros_matrix = Matrix.zeros

__all__ = ['Matrix', 'zeros_matrix']
