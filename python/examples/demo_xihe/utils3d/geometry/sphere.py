import math
import numpy as np


def fibonacci_sphere(samples=1):

    points = np.zeros((samples, 3), dtype=np.float32)
    phi = math.pi * (3. - math.sqrt(5.))  # golden angle in radians

    for i in range(samples):
        y = 1 - (i / float(samples - 1)) * 2  # y goes from 1 to -1
        radius = math.sqrt(1 - y * y)  # radius at y

        theta = phi * i  # golden angle increment

        x = math.cos(theta) * radius
        z = math.sin(theta) * radius

        points[i] = [x, y, z]

    return points
