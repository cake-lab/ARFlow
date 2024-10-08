# Contributing to ARFlow

## Quick Start

### Setup

Fork the ARFlow repository into your account: [https://github.com/cake-lab/ARFlow/fork](https://github.com/cake-lab/ARFlow/fork)

ARFlow uses [`poetry`](https://python-poetry.org) for dependency management. Install it [here](https://python-poetry.org/docs/).

Clone the forked repository:

```shell
git clone https://github.com/{your-account}/ARFlow.git
cd ARFlow/python
poetry install
```

### Code Style

ARFlow uses [`ruff`](https://docs.astral.sh/ruff/) for linting and formatting. We also use [`pyright`](https://github.com/microsoft/pyright) for type checking. Make sure you have the appropriate extensions or corresponding LSPs installed in your editor.

These tools should run automatically in your editor. If you want to run them manually, you can also use the following commands:

```shell
poetry run ruff check # check for linting errors

poetry run ruff check --fix # check for linting errors and fix them

poetry run ruff format # format the code
```

All of these quality checks are run automatically before every commit using [`pre-commit`](https://pre-commit.com). To install the pre-commit hooks, run:

```shell
poetry run pre-commit install
```

To manually invoke the pre-commit checks, run:

```shell
poetry run pre-commit run --all-files
```

### Type Completeness

Library authors are encouraged to prioritize bringing their public API to 100% type coverage. Although this is very hard in ARFlow's case due to our dependency on `gRPC`, we should still strive to achieve this goal. To check for type completeness, run:

```shell
poetry run pyright --ignoreexternal --verifytypes arflow
```

To read more about formalizing libraries' public APIs, please refer to this excellent [blog post](https://dagster.io/blog/adding-python-types#-step-3-formalize-public-api) by Dagster.

### Testing

ARFlow uses [`pytest`](https://pytest.org). Make sure you are in the `python` directory and then run tests with:

```shell
poetry run pytest
```

### Logging

- Log key events for debugging and tracking.
- Avoid logging sensitive information (e.g., user data).
- Initialize a logger in each module using `logger = logging.getLogger(__name__)`. This enables granular logging and gives users control over logs from specific parts of the library.
- Use appropriate log levels:

| Level       | Usage                        |
| ----------- | ---------------------------- |
| `debug()`   | Detailed internal state info |
| `info()`    | General operational events   |
| `warning()` | Unexpected events, non-fatal |
| `error()`   | Errors, exceptions           |

Example:

```python
logger = logging.getLogger(__name__)
logger.debug("Processing request: %s", request_id)
```

### Continuous Integration

ARFlow uses GitHub Actions for continuous integration. The CI pipeline runs the following checks:

```shell
poetry run ruff check # linting
poetry run pyright arflow # type checking
poetry run pytest # testing
```

## Packages & Tools

### [`poetry`](https://python-poetry.org)

Python dependency management.

ARFlow uses `poetry` to manage dependencies and run commands. Commands can be found in the `pyproject.toml` file in the `[tool.poetry.scripts]` section and can be run via `poetry run <command>`.

### [`protobuf`](https://protobuf.dev)

A language-neutral, platform-neutral, extensible mechanism for serializing structured data.

ARFlow uses `protobuf` to define the communication protocol between the server and the client. The protocol is defined in [`service.proto`](./protos/arflow/_grpc/service.proto) and can be compiled using [`compile.sh`](./protos/compile.sh).

### [`pickle`](https://docs.python.org/3/library/pickle.html)

Implements binary protocols for serializing and deserializing Python objects. Pickling is the same as serialization, marshalling, or flattening in other languages. The inverse operation is called unpickling.

### [`asyncio`](https://docs.python.org/3/library/asyncio.html)

A library to write **concurrent** code using using the `async` and `await` syntax. Perfect for writing IO-bound and high-level structured network code.

### [`rerun.io`](https://github.com/rerun-io/rerun)

A tool to build time aware visualizations of multimodal data.

ARFlow uses the Rerun Python SDK to visualize the data collected by the ARFlow server.

## Documentation

ARFlow uses [`pdoc`](https://pdoc.dev). You can refer to their documentation for more information on how to generate documentation.

To preview the documentation locally, run:

```shell
poetry run pdoc arflow examples # or replace with module_name that you want to preview
```

## gRPC Best Practices

The ARFlow server and client communicates through gRPC. Here are some best practices to keep in mind when working with gRPC:

### Input validation

All fields in `proto3` are optional, so you’ll need to validate that they’re all set. If you leave one unset, then it’ll default to zero for numeric types or to an empty string for strings.

### Error handling

gRPC is built on top of HTTP/2, the status code is like the standard HTTP status code. This allows clients to take different actions based on the code they receive. Proper error handling also allows middleware, like monitoring systems, to log how many requests have errors.

ARFlow uses the `grpc_interceptor` library to handle exceptions. This library provides a way to raise exceptions in your service handlers, and have them automatically converted to gRPC status codes. Check out an example usage [here](https://github.com/d5h-foss/grpc-interceptor/tree/master?tab=readme-ov-file#server-interceptor).

`grpc_interceptor` also provides a testing framework to run a gRPC service with interceptors. You can check out the example usage [here](./python/tests/test_interceptor.py).

### Protobuf versioning

To achieve **backward compatibility**, you should never remove a field from a message. Instead, mark it as deprecated and add a new field with the new name. This way, clients that use the old field will still work.

### Protobuf linting

We use `buf` to lint our protobuf files. You can install it by following the instructions [here](https://buf.build/docs/installation).

### Type checking Protobuf-generated code

We use `pyright` and `grpc-stubs` to type check our Protobuf-generated code.

### Graceful shutdown

When the server is shutting down, it should wait for all in-flight requests to complete before shutting down. This is to prevent data loss or corruption. We have done this in the ARFlow server.

### Securing channels

gRPC supports TLS encryption out of the box. We have not implemented this in the ARFlow server yet. If you are interested in working on this, please let us know.

## Common Issues

### VSCode Force Changes Locale

VSCode may force changes the locale to `en_US.UTF-8` for git commit hooks. To fix this, run:

```shell
sudo locale-gen en_US.UTF-8
```

### Running Rerun on WSL2

Please refer to their documentation [documentation](https://rerun.io/docs/getting-started/troubleshooting#wsl2).
