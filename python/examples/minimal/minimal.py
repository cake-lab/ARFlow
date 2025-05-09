#!/usr/bin/env python3
"""Demonstrates the most barebone usage of ARFlow."""

from __future__ import annotations

import sys

import numpy as np

import arflow


class MinimalService(arflow.ARFlowService):
    def on_register(self, request: arflow.RegisterRequest):
        positions = np.vstack(
            [xyz.ravel() for xyz in np.mgrid[3 * [slice(-10, 10, 10j)]]]
        ).T
        colors = (
            np.vstack([rgb.ravel() for rgb in np.mgrid[3 * [slice(0, 255, 10j)]]])
            .astype(np.uint8)
            .T
        )

        self.recorder.log(
            "my_points", self.recorder.Points3D(positions, colors=colors, radii=0.5)
        )
        pass

    def on_frame_received(self, frame_data: arflow.DataFrameRequest):
        print("Received a frame")


def main() -> None:
    # sanity-check since all other example scripts take arguments:
    assert len(sys.argv) == 1, f"{sys.argv[0]} does not take any arguments"
    arflow.create_server(MinimalService, port=8500, path_to_save=None)


if __name__ == "__main__":
    main()
