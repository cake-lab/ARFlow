#!/usr/bin/env python3
# type: ignore
"""Demonstrates the usage of ARFlow with Depth Anything v2.

Note: `# type: ignore` is added to the first line to suppress typecheck errors.
In case you want to copy this code, please remove the first line if you are using typecheck.
"""

from __future__ import annotations

import sys
from threading import Thread

import numpy as np
import torch
import numpy.typing as npt
from PIL import Image
from transformers import pipeline

import arflow


class DepthAnythingV2Service(arflow.ARFlowServicer):
    def __init__(self) -> None:
        super().__init__()
        self.device = "cuda" if torch.cuda.is_available() else "cpu"
        self.pipe = pipeline(
            "depth-estimation",
            model="depth-anything/Depth-Anything-V2-base-hf",
            device=self.device,
        )

    def on_register(self, request: arflow.RegisterClientRequest):
        self.num_frame = 0

    def on_frame_received(self, decoded_data_frame: arflow.DecodedDataFrame):
        if self.num_frame % 50 == 0:
            thread = Thread(
                target=lambda: (self.run_depth_estimation(decoded_data_frame.color_rgb.copy()))
            )
            thread.start()

        self.num_frame = self.num_frame + 1

    def run_depth_estimation(self, color_rgb: npt.NDArray[np.uint8]):
        """Run depth estimation on the given image. The pipeline returns a dictionary with two entries.
        The first one, called predicted_depth, is a tensor with the values being the depth expressed in
        meters for each pixel. The second one, depth, is a PIL image that visualizes the depth estimation result."""

        image = Image.fromarray(np.flipud(color_rgb))

        predictions = self.pipe(image)
        self.record_predictions(predictions)
        return predictions

    def record_predictions(self, predictions):
        self.recorder.log(
            "DepthAnythingV2/depth", self.recorder.Image(predictions["depth"])
        )


def main() -> None:
    # sanity-check since all other example scripts take arguments:
    assert len(sys.argv) == 1, f"{sys.argv[0]} does not take any arguments"
    arflow.run_server(DepthAnythingV2Service, port=8500, path_to_save=None)


if __name__ == "__main__":
    main()
