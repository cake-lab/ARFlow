#!/usr/bin/env python3
"""Demonstrates the usage of ARFlow with Depth Anything v2."""

from __future__ import annotations

import sys
from threading import Thread
from typing import Any, Dict

import numpy as np
import torch
from PIL import Image
from transformers import pipeline

import arflow


class DepthAnythingV2Service(arflow.ARFlowService):
    def __init__(self, *args, **kwargs) -> None:
        super().__init__(*args, **kwargs)
        self.device = "cuda" if torch.cuda.is_available() else "cpu"
        self.pipe = pipeline(
            "depth-estimation",
            model="depth-anything/Depth-Anything-V2-base-hf",
            device=self.device,
        )

    def on_register(self):
        self.num_frame = 0

    def on_frame_received(self, frame_data: Dict[str, Any]):
        color_rgb = frame_data["color_rgb"]
        if self.num_frame % 100 == 0:
            thread = Thread(target=self.run_depth_estimation, args=(color_rgb.copy()))
            thread.start()

        self.num_frame = self.num_frame + 1

    def run_depth_estimation(self, color_rgb: np.ndarray):
        """Run depth estimation on the given image. The pipeline returns a dictionary with two entries.
        The first one, called predicted_depth, is a tensor with the values being the depth expressed in
        meters for each pixel. The second one, depth, is a PIL image that visualizes the depth estimation result."""

        image = Image.fromarray(np.flipud(color_rgb))

        predictions = self.pipe(image)
        print(predictions)
        self.record_predictions(predictions)
        return predictions

    def record_predictions(self, predictions: dict):
        self.recorder.log(
            "DepthAnythingV2/predicted_depth",
            self.recorder.Tensor(predictions["predicted_depth"]),
        )
        self.recorder.log(
            "DepthAnythingV2/depth", self.recorder.Image(predictions["depth"])
        )
        self.recorder.log(
            "DepthAnythingV2/predicted_depth",
            self.recorder.Image(predictions["predicted_depth"]),
        )


def main() -> None:
    # sanity-check since all other example scripts take arguments:
    assert len(sys.argv) == 1, f"{sys.argv[0]} does not take any arguments"
    arflow.create_server(DepthAnythingV2Service, port=8500, path_to_save=None)


if __name__ == "__main__":
    main()
