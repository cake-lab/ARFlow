"""A simple example of replaying saved ARFlow data."""

import arflow
from examples.simple_server import CustomService


def main():
    """Run the example."""
    player = arflow.ARFlowPlayer(
        CustomService, frame_data_path="frames_2024_09_06_18_21_17.pkl"
    )
    player.run()


if __name__ == "__main__":
    main()
