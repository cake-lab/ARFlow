"""Simple server for ARFlow service."""

from arflow.core import ARFlowService, create_server


def serve():
    """Run a simple ARFlow server."""
    create_server(ARFlowService)


if __name__ == "__main__":
    serve()
