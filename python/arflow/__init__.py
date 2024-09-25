""".. include:: ../README.md"""  # noqa: D415

from arflow.core import ARFlowServicer, create_server
from arflow.replay import ARFlowPlayer
from arflow.service_pb2 import ClientConfiguration
from arflow.types import DecodedDataFrame

__docformat__ = "google"  # Should match Ruff docstring format in ../pyproject.toml

# https://pdoc.dev/docs/pdoc.html#exclude-submodules-from-being-documented
__all__ = [
    "create_server",
    "ARFlowServicer",
    "ARFlowPlayer",
    "DecodedDataFrame",
    "ClientConfiguration",
]
