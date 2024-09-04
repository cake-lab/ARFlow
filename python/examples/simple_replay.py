"""A simple example of replaying saved ARFlow data."""

import arflow


class CustomService(arflow.ARFlowService):
    def on_frame_received(self, frame: dict):
        """Called when a frame is received."""
        print(frame)


def main():
    """Run the example."""
    player = arflow.ARFlowPlayer(
        CustomService, frame_data_path="frames_2024_08_30_03_08_21.pkl"
    )
    player.run()


if __name__ == "__main__":
    main()
