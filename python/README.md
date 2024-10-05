# The ARFlow Python Server

[![image](https://img.shields.io/pypi/v/arflow.svg)](https://pypi.python.org/pypi/arflow)
[![image](https://img.shields.io/pypi/l/arflow.svg)](https://github.com/cake-lab/ARFlow/blob/main/LICENSE)
[![image](https://img.shields.io/pypi/pyversions/arflow.svg)](https://pypi.python.org/pypi/arflow)
[![Ruff](https://img.shields.io/endpoint?url=https://raw.githubusercontent.com/astral-sh/ruff/main/assets/badge/v2.json)](https://github.com/astral-sh/ruff)
[![Checked with pyright](https://microsoft.github.io/pyright/img/pyright_badge.svg)](https://microsoft.github.io/pyright/)
[![CI status](https://github.com/cake-lab/ARFlow/actions/workflows/ci.yml/badge.svg)](https://github.com/cake-lab/ARFlow/actions)
[![Release status](https://github.com/cake-lab/ARFlow/actions/workflows/release.yml/badge.svg)](https://github.com/cake-lab/ARFlow/actions)

The ARFlow Python server collects streaming data from your ARFlow clients. The server is designed to be easily extensible and can be integrated with your own research prototype. Data is streamed to the `rerun` logger and saved to a `pickle` file at the end of a session, which can be visualized later using the ARFlow Player.

<!-- TODO: Insert demo image -->

## Installation

The ARFlow server can be simply installed via `pip`:

```shell
# Create a python environment using your favorite tool, then
pip install arflow
```

## Server CLI

Here are some example usages of the ARFlow server CLI:

```shell
arflow serve # ARFlow port 8500, no save file

arflow serve -p 1234 -s ./ # ARFlow port 1234, save to current working directory

arflow replay ./FRAME_DATA_PATH.pkl # replay ARFlow data file

arflow -h # show help
```

## Examples

Check out the [examples](https://github.com/cake-lab/ARFlow/tree/main/python/examples). We recommend starting with the [`simple`](examples/simple/README.md) example.

## Contributing

We welcome contributions to ARFlow! Please refer to the [`CONTRIBUTING.md`](https://github.com/cake-lab/ARFlow/blob/main/CONTRIBUTING.md) file for more information.
