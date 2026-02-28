# ARFlow Server Examples

The simplest example is [`minimal`](minimal/minimal.py). You may want to start there!

## Setup

If you're using `pip`, you should create and activate a virtual environment before installing any example's dependencies:

```sh
python3 -m venv .venv
source .venv/bin/activate
```

If you're using `poetry` instead, you can just install the dependencies directly, as shown below.

## Installing the example

Each example is packaged as a regular Python package, with a `pyproject.toml` file specifying its required dependencies. To run an example, it must first be installed.

For example, to install dependencies and run the toy `minimal` example (which doesn't need to download any data) run:

```sh
# Using pip:
pip install -e python/examples/minimal

# Using poetry:
cd python/examples/minimal
poetry install
```

**Note**: it is import to install example in editable mode, which is done using the `-e` flag (short for `--editable`).

Once installed, the example can be run as a regular Python module:

```shell
python3 -m minimal

# or, if you're using poetry:
poetry run minimal
```

Examples also declare console script, so they can also be run directly:

```shell
minimal
```

## Contributions welcome
Feel free to open a PR to add a new example!

See the [`CONTRIBUTING.md`](https://github.com/cake-lab/ARFlow/blob/main/CONTRIBUTING.md) file  for details on how to contribute.
