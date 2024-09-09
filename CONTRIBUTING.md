# Contributing to ARFlow

## Quick Start

### Setup

Fork the ARFlow repository into your account: [https://github.com/cake-lab/ARFlow/fork](https://github.com/cake-lab/ARFlow/fork)

ARFlow uses [`poetry`](https://python-poetry.org) for dependency management. Install it [here](https://python-poetry.org/docs/).

Clone the forked repository:

```bash
git clone https://github.com/{your-account}/ARFlow.git
cd ARFlow/python
poetry install
```

### Code Style

ARFlow uses [`ruff`](https://docs.astral.sh/ruff/) for linting and formatting. We also use [`pyright`](https://github.com/microsoft/pyright) for type checking. Make sure you have the appropriate extensions or corresponding LSPs installed in your editor.

These tools should run automatically in your editor. If you want to run them manually, you can also use the following commands:

```bash
poetry run ruff check # check for linting errors

poetry run ruff check --fix # check for linting errors and fix them

poetry run ruff format # format the code

poetry run pyright # type check
```

All of these quality checks are run automatically before every commit using [`pre-commit`](https://pre-commit.com). To install the pre-commit hooks, run:

```bash
poetry run pre-commit install
```

### Testing

ARFlow uses [`pytest`](https://pytest.org). Make sure you are in the `python` directory and then run tests with:

```bash
poetry run pytest
```

###

## Packages & Tools

### [`poetry`](https://python-poetry.org)

Python dependency management.

ARFlow uses `poetry` to manage dependencies and run commands. Commands can be found in the `pyproject.toml` file in the `[tool.poetry.scripts]` section and can be run via `poetry run <command>`.

### [`protobuf`](https://protobuf.dev)

A language-neutral, platform-neutral, extensible mechanism for serializing structured data.

ARFlow uses `protobuf` to define the communication protocol between the server and the client. The protocol is defined in [`service.proto`](../protos/arflow/service.proto) and can be compiled using [`compile.sh`](../protos/scripts/compile.sh).

### [`pickle`](https://docs.python.org/3/library/pickle.html)

Implements binary protocols for serializing and deserializing Python objects. Pickling is the same as serialization, marshalling, or flattening in other languages. The inverse operation is called unpickling.

### [`asyncio`](https://docs.python.org/3/library/asyncio.html)

A library to write **concurrent** code using using the `async` and `await` syntax. Perfect for writing IO-bound and high-level structured network code.

### [`rerun.io`](https://github.com/rerun-io/rerun)

A tool to build time aware visualizations of multimodal data.

ARFlow uses the Rerun Python SDK to visualize the data collected by the ARFlow server.

## Documentation

ARFlow uses [`pdoc`](https://pdoc.dev). You can refer to their documentation for more information on how to generate documentation. If you create a new submodule, make sure to add it to the `__all__` list defined in the `_init__.py` file of the `arflow` package.

To preview the documentation locally, run:

```bash
poetry run pdoc arflow # or replace with module_name that you want to preview
```

## Common Issues

### VSCode Force Changes Locale

VSCode may force changes the locale to `en_US.UTF-8` for git commit hooks. To fix this, run:

```bash
sudo locale-gen en_US.UTF-8
```
