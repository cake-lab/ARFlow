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

## Development

### Important Packages & Tools

#### [`poetry`](https://python-poetry.org)

Python dependency management. 

ARFlow uses `poetry` to manage dependencies and run commands. Commands can be found in the `pyproject.toml` file in the `[tool.poetry.scripts]` section and can be run via `poetry run <command>`.

#### [`protobuf`](https://protobuf.dev)

A language-neutral, platform-neutral, extensible mechanism for serializing structured data.

ARFlow uses `protobuf` to define the communication protocol between the server and the client. The protocol is defined in [`service.proto`](../protos/arflow/service.proto) and can be compiled using [`compile.sh`](../protos/scripts/compile.sh).

#### [`pickle`](https://docs.python.org/3/library/pickle.html)

Implements binary protocols for serializing and deserializing Python objects. Pickling is the same as serialization, marshalling, or flattening in other languages. The inverse operation is called unpickling.

#### [`asyncio`](https://docs.python.org/3/library/asyncio.html)

A library to write **concurrent** code using using the `async` and `await` syntax. Perfect for writing IO-bound and high-level structured network code.

#### [`rerun.io`](https://github.com/rerun-io/rerun)

A tool to build time aware visualizations of multimodal data. 

ARFlow uses the Rerun Python SDK to visualize the data collected by the ARFlow server.

### Documentation

ARFlow uses [`pdoc`](https://pdoc.dev). You can refer to their documentation for more information on how to generate documentation. If you create a new submodule, make sure to add it to the `__all__` list defined in the `_init__.py` file of the `arflow` package.

To preview the documentation locally, run:

```bash
poetry run pdoc <module_name>
```
