#!/usr/bin/env python3
"""Demonstrates the most barebone usage of ARFlow."""

from __future__ import annotations

import sys

import numpy as np

import arflow

import rerun as rr
import rerun.datatypes as rrdt


class MinimalService(arflow.ARFlowService):
    def __init__(self):
        super().__init__()
        attitude = rrdt.Quaternion(xyzw=[1, 0, 0, 0])
        rr.log("attitude", rr.Arrows3D(attitude=attitude))


                    # rr.log(
            #     "world/gyroscope/acceleration",
            #                 #     rr.Arrows3D(
            # #         vectors=[[1, 0, 0], [0, 1, 0], [0, 0, 1]],
            # #         colors=[[255, 0, 0], [0, 255, 0], [0, 0, 255]],
            # #     ),
            #     rr.Arrows3D([gravity, acceleration], 
            #                 colors=[[0, 0, 255], [255, 255, 0]]),
            # )
                        # self.logger.log(
            #     "world/xyz",
            #     rr.Arrows3D(
            #         vectors=[[1, 0, 0], [0, 1, 0], [0, 0, 1]],
            #         colors=[[255, 0, 0], [0, 255, 0], [0, 0, 255]],
            #     ),
            # )


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
