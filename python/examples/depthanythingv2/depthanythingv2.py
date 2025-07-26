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
        super().__init__()
        self.device = "cuda" if torch.cuda.is_available() else "cpu"
        self.pipe = pipeline(
            "depth-estimation",
            model="depth-anything/Depth-Anything-V2-base-hf",
            device=self.device,
        )

    def on_register(self, request: arflow.RegisterRequest):
        self.num_frame = 0
        # Log the actual dimensions received
        print(f"Registered device: {request.device_name}")
        print(f"Depth resolution: {request.camera_depth.resolution_x}x{request.camera_depth.resolution_y}")
        
        # Store expected dimensions
        self.depth_width = request.camera_depth.resolution_x
        self.depth_height = request.camera_depth.resolution_y
        
        if self.depth_width == 0 or self.depth_height == 0:
            print("WARNING: Depth resolution is 0x0, depth data may not be properly configured")

    def on_frame_received(self, frame_data: Dict[str, Any]):
        color_rgb = frame_data.get("color_rgb")
        
        if color_rgb is None:
            print("WARNING: No color_rgb data in frame")
            return
            
        # Check if the image has valid dimensions
        if color_rgb.size == 0 or color_rgb.shape[0] == 0 or color_rgb.shape[1] == 0:
            print(f"WARNING: Invalid color_rgb dimensions: {color_rgb.shape}")
            return
        
        if self.num_frame % 50 == 0:
            thread = Thread(target=lambda: (self.run_depth_estimation(color_rgb.copy())))
            thread.start()
        self.num_frame = self.num_frame + 1

    def run_depth_estimation(self, color_rgb: np.ndarray):
        """Run depth estimation on the given image. The pipeline returns a dictionary with two entries.
        The first one, called predicted_depth, is a tensor with the values being the depth expressed in
        meters for each pixel. The second one, depth, is a PIL image that visualizes the depth estimation result."""
        image = Image.fromarray(np.flipud(color_rgb))
        predictions = self.pipe(image)
        self.record_predictions(predictions)
        return predictions

    def record_predictions(self, predictions: dict):
        # Resize the depth image before recording to avoid WGPU texture size issues
        depth_image = predictions["depth"]
        
        # If the image is not a power of 2 or too large, resize it
        width, height = depth_image.size
        
        # Find appropriate size (power of 2, max 512)
        def next_power_of_2(n):
            if n <= 64:
                return 64
            if n <= 128:
                return 128
            if n <= 256:
                return 256
            return 512
        
        new_width = next_power_of_2(width)
        new_height = next_power_of_2(height)
        
        # Only resize if necessary
        if (width, height) != (new_width, new_height):
            depth_image = depth_image.resize((new_width, new_height), Image.Resampling.LANCZOS)
            print(f"Resized depth image from {width}x{height} to {new_width}x{new_height} for recording")
        
        self.recorder.log(
            "DepthAnythingV2/depth", self.recorder.Image(depth_image)
        )

def main() -> None:
    # sanity-check since all other example scripts take arguments:
    assert len(sys.argv) == 1, f"{sys.argv[0]} does not take any arguments"
    arflow.create_server(DepthAnythingV2Service, port=8500, path_to_save=None)

if __name__ == "__main__":
    main()