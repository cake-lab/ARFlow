"""A simple example of replaying saved ARFlow data."""
import arflow
from examples.simple_server import CustomService


def main():
    """Run the example."""
    player = arflow.ARFlowPlayer(
        CustomService, frame_data_path="./frames_2024_01_27_02_34_32.pkl"
    )
    player.run()


if __name__ == "__main__":
    main()
