# ARFlow

[ARFlow: A Framework for Simplifying AR Experimentation Workflow](https://doi.org/10.1145/3638550.3643617)

[Paper](https://doi.org/10.1145/3638550.3643617) | [BibTeX](#bibtex) | [Project Page](https://cake.wpi.edu/ARFlow/) | [Video](https://youtu.be/mml8YrCgfTk)

Watch our demo video:

[![Demo video](https://img.youtube.com/vi/mml8YrCgfTk/maxresdefault.jpg)](https://youtu.be/mml8YrCgfTk)

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
