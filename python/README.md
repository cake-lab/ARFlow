# The ARFlow Python Server

[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](https://www.gnu.org/licenses/gpl-3.0)
[![Ruff](https://img.shields.io/endpoint?url=https://raw.githubusercontent.com/astral-sh/ruff/main/assets/badge/v2.json)](https://github.com/astral-sh/ruff)
[![Checked with pyright](https://microsoft.github.io/pyright/img/pyright_badge.svg)](https://microsoft.github.io/pyright/)

ARFlow server can be simply installed via `pip`:

```bash
pip install arflow
```

Next, you may integrate ARFlow with your own research prototype via the Python API:

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
