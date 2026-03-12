"""A utility library for XiheNet inference."""

import math
from typing import List

import numpy as np
import torch

n_min = torch.tensor(
    [
        -0.0191,
        0.5743,
        0.5416,
        0.5087,
        0.4995,
        0.5069,
        0.5359,
        0.4120,
        0.6148,
        -0.0133,
        0.5700,
        0.5561,
        0.4761,
        0.5743,
        0.4778,
        0.5132,
        0.3798,
        0.5818,
        -0.0052,
        0.5575,
        0.5294,
        0.4674,
        0.5912,
        0.4608,
        0.5425,
        0.4184,
        0.5450,
    ],
    dtype=torch.float32,
)

n_scale = torch.tensor(
    [
        0.4584,
        0.5124,
        0.8311,
        0.7338,
        0.9732,
        0.8520,
        1.2035,
        1.0601,
        0.9898,
        0.4693,
        0.5048,
        0.7423,
        0.6536,
        0.8798,
        0.8204,
        1.0771,
        1.1001,
        0.9555,
        0.4410,
        0.4517,
        0.6117,
        0.6044,
        0.7203,
        0.7161,
        0.9054,
        1.0782,
        0.8091,
    ],
    dtype=torch.float32,
)


def fibonacci_sphere(samples=1):
    points = np.zeros((samples, 3), dtype=np.float32)
    phi = math.pi * (3.0 - math.sqrt(5.0))  # golden angle in radians

    for i in range(samples):
        y = 1 - (i / float(samples - 1)) * 2  # y goes from 1 to -1
        radius = math.sqrt(1 - y * y)  # radius at y

        theta = phi * i  # golden angle increment

        x = math.cos(theta) * radius
        z = math.sin(theta) * radius

        points[i] = [x, y, z]

    return points


class JointEntropyCalculator:
    def __init__(
        self, anchor_levels: List[int] = [512, 768, 1024, 1280, 1536, 1792, 2048]
    ):
        self.anchor_group = [
            torch.from_numpy(fibonacci_sphere(v).transpose()) for v in anchor_levels
        ]

        self.anchor_dist_group = [
            torch.zeros((v), dtype=torch.long) for v in anchor_levels
        ]

    def forward(self, points):
        entropy = 0
        pn = torch.linalg.norm(points, dim=-1, keepdims=True)
        pt = points / pn

        for a_idx, anchors in enumerate(self.anchor_group):
            idx = torch.argmax(pt @ anchors, dim=-1)

            i, c = torch.unique(idx, return_counts=True)
            t = self.anchor_dist_group[a_idx]
            t *= 0
            t[i] = c
            anchor_dist_valued = t[t > 0]

            p = anchor_dist_valued / anchor_dist_valued.sum()
            entropy += -1 * torch.sum(p * torch.log2(p))

        return entropy.item()
