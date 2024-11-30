""".. include:: ../README.md"""  # noqa: D415

# Imported symbols are private by default. By aliasing them here, we make it clear that they are part of the public API.
from arflow._core import ARFlowServicer as ARFlowServicer
from arflow._core import run_server as run_server
from arflow._session_stream import (
    SessionStream as SessionStream,
)
from arflow._types import (
    DecodedARFrames as DecodedARFrames,
)
from cakelab.arflow_grpc.v1.device_pb2 import Device as Device
from cakelab.arflow_grpc.v1.session_pb2 import Session as Session

__docformat__ = "google"  # Should match Ruff docstring format in ../pyproject.toml

# https://pdoc.dev/docs/pdoc.html#exclude-submodules-from-being-documented
__all__ = [
    "run_server",
    "ARFlowServicer",
    "DecodedARFrames",
    "DecodedARFrames",
    "SessionStream",
    "Session",
    "Device",
]
