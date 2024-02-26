from __future__ import annotations

import torch
import numpy as np
from typing import Tuple


class Tensor:
    _data: np.ndarray

    def __init__(self, source):
        if isinstance(source, np.ndarray):
            self._data = source

    @staticmethod
    def zeros(shape: Tuple) -> Tensor:
        data = np.zeros(shape)
        return Tensor(data)

    def __array__(self):
        return self._data

    def flip_feature_channel(self):
        self._data = np.moveaxis(self._data, 0, -1)

    def numpy(self):
        return self._data

    def torch(self):
        return torch.from_numpy(self._data)


zeros_tensor = Tensor.zeros

__all__ = ['Tensor', 'zeros_tensor']
