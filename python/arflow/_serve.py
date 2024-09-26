"""Simple server for ARFlow service."""

import logging

from arflow._core import ARFlowServicer, create_server


def serve():
    """Run a simple ARFlow server."""
    create_server(ARFlowServicer)


if __name__ == "__main__":
    logging.basicConfig()  # TODO: Replace print with logging
    serve()
