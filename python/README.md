# The ARFlow Python Server

[![Ruff](https://img.shields.io/endpoint?url=https://raw.githubusercontent.com/astral-sh/ruff/main/assets/badge/v2.json)](https://github.com/astral-sh/ruff)
[![Checked with pyright](https://microsoft.github.io/pyright/img/pyright_badge.svg)](https://microsoft.github.io/pyright/)

The ARFlow Python server collects streaming data from your ARFlow clients. The
server is designed to be easily extensible and can be integrated with your own
research prototype. Data is streamed to a
[Rerun logger](https://rerun.io/docs/getting-started/data-in/python#logging-our-first-points)
and saved to [RRD](https://rerun.io/docs/getting-started/data-in/open-any-file)
files, which can be visualized later using the Rerun Viewer.

<!-- TODO: Insert demo image -->

## Installation

The ARFlow server can be simply installed via
[pip](https://pypi.org/project/pip/):

```shell
# Create a python environment using your favorite tool, then
pip install arflow
```

## Server CLI

Here are some example usages of the ARFlow server CLI:

```shell
arflow view # ARFlow port 8500, view mode, no save to files

arflow save -p 1234 -s ./ # ARFlow port 1234, save to current working directory

arflow rerun ./FRAME_DATA_PATH.rrd # replay ARFlow data file

arflow rerun *.rrd # replay multiple ARFlow data files

arflow -h # show help
```

## Examples

Check out the
[examples](https://github.com/cake-lab/ARFlow/tree/main/python/examples). We
recommend starting with the [simple](examples/simple/README.md) example.

## Contributing

We welcome contributions to ARFlow! Please refer to the
[CONTRIBUTING.md](https://github.com/cake-lab/ARFlow/blob/main/CONTRIBUTING.md)
file for more information.
