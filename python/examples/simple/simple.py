#!/usr/bin/env python3
"""A simple example of extending the ARFlow server."""

from pathlib import Path

import arflow


class CustomService(arflow.ARFlowServicer):
    def on_register(self, request: arflow.RegisterClientRequest):
        """Called when a client registers."""
        print("Client registered!")

    def on_frame_received(self, decoded_data_frame: arflow.DecodedDataFrame):
        """Called when a frame is received."""
        print("Frame received!")


def main():
    arflow.run_server(CustomService, port=8500, path_to_save=Path("./"))


if __name__ == "__main__":
    main()
