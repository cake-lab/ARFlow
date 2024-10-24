""".. include:: ../README.md"""  # noqa: D415

# Imported symbols are private by default. By aliasing them here, we make it clear that they are part of the public API.
from arflow._core import ARFlowServicer as ARFlowServicer
from arflow._core import run_server as run_server
from arflow._replay import ARFlowPlayer as ARFlowPlayer
from arflow._types import DecodedDataFrame as DecodedDataFrame
from arflow_grpc.service_pb2 import RegisterClientRequest as RegisterClientRequest

__docformat__ = "google"  # Should match Ruff docstring format in ../pyproject.toml

# https://pdoc.dev/docs/pdoc.html#exclude-submodules-from-being-documented
__all__ = [
    "run_server",
    "ARFlowServicer",
    "ARFlowPlayer",
    "DecodedDataFrame",
    "RegisterClientRequest",
]
