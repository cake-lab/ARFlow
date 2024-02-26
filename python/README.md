# Get Started With the ARFlow Server

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
