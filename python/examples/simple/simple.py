#!/usr/bin/env python3
"""A simple example of extending the ARFlow server."""

from pathlib import Path

import arflow

class CustomService(arflow.ARFlowServicer):
    def on_save_ar_frames(self, frames, session_stream, device):
        print("AR frame received")
    def on_save_transform_frames(self, frames, session_stream, device):
        print("Transform frame received")
    def on_save_color_frames(self, frames, session_stream, device):
        print("Color frame received")
    def on_save_depth_frames(self, frames, session_stream, device):
        print("Depth frame received")
    def on_save_gyroscope_frames(self, frames, session_stream, device):
        print("Gyroscope frame received")
    def on_save_audio_frames(self, frames, session_stream, device):
        print("Audio frame received")
    def on_save_plane_detection_frames(self, frames, session_stream, device):
        print("Plane detection frame received")
    def on_save_point_cloud_frames(self, frames, session_stream, device):
        print("Point cloud frame received")
    def on_save_mesh_detection_frames(self, frames, session_stream, device):
        print("Mesh detection frame received")

def main():
    arflow.run_server(CustomService, spawn_viewer = True, port=8500)


if __name__ == "__main__":
    main()
