from typing import Sequence

import numpy as np


class Vector:
    data: np.ndarray

    def __init__(self, components: Sequence):
        self.data = np.array(components)
        assert len(self.data.shape) == 1

    def __len__(self):
        return self.data.shape[0]

    def normalize(self):
        return self.data / np.linalg.norm(self.data, axis=-1)[:, np.newaxis]

    def __array__(self):
        return self.data

    def __getitem__(self, key):
        return self.data[key]


class Vector2(Vector):
    def __init__(self, components: Sequence):
        super().__init__(components)
        assert len(self) == 2

    @property
    def x(self):
        return self.data[0]

    @property
    def y(self):
        return self.data[1]


class Vector3(Vector):
    def __init__(self, components: Sequence):
        super().__init__(components)
        assert len(self) == 3

    @property
    def x(self):
        return self.data[0]

    @property
    def y(self):
        return self.data[1]

    @property
    def z(self):
        return self.data[2]


__all__ = ['Vector', 'Vector2', 'Vector3']
