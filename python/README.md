# The ARFlow Python Server

[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](https://www.gnu.org/licenses/gpl-3.0)
[![Ruff](https://img.shields.io/endpoint?url=https://raw.githubusercontent.com/astral-sh/ruff/main/assets/badge/v2.json)](https://github.com/astral-sh/ruff)
[![Checked with pyright](https://microsoft.github.io/pyright/img/pyright_badge.svg)](https://microsoft.github.io/pyright/)

The ARFlow Python server collects streaming data from your ARFlow clients. The server is designed to be easily extensible and can be integrated with your own research prototype. Data is streamed to the `rerun` logger and saved to a `pickle` file at the end of a session, which can be visualized later using the ARFlow Player.

<!-- TODO: Insert demo image -->

## Installation

The ARFlow server can be simply installed via `pip`:

```bash
pip install arflow
```

## Examples

Next, you may integrate ARFlow with your own research prototype via the Python API:

<!-- TODO: Figure out how to sync this with example scripts -->

```python
"""A simple example of extending the ARFlow server."""

import arflow


class CustomService(arflow.ARFlowService):
    def on_frame_received(self, frame: arflow.DataFrameRequest):
        """Called when a frame is received."""
        print("Frame received!")


def main():
    arflow.create_server(CustomService, port=8500, path_to_save="./")


if __name__ == "__main__":
    main()

```

Save the above code to a file, e.g., `simple_server.py`, and run it:

```bash
python3 simple_server.py
```

Once you have your server running, you can start your ARFlow clients and connect them to the server. The server will start collecting data from the clients and save it to a `pickle` file at the end of the session.

You can visualize the data using the ARFlow Player:

```python
"""A simple example of replaying saved ARFlow data."""

import arflow
from .simple_server import CustomService


def main():
    """Run the example."""
    player = arflow.ARFlowPlayer(
        CustomService, frame_data_path="FRAME_DATA_PATH.pkl"
    )
    player.run()


if __name__ == "__main__":
    main()

```

Save the above code to a file, e.g., `simple_replay.py`, replace `FRAME_DATA_PATH` with the path to your saved `pickle` file, and run it:


```bash
python3 simple_replay.py
```

For more examples, check out the [examples](examples) directory.

## Contributing

We welcome contributions to ARFlow! Please refer to the [`CONTRIBUTING.md`](https://github.com/cake-lab/ARFlow/blob/main/CONTRIBUTING.md) file for more information.
