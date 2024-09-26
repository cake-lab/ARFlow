"""Simple server for ARFlow service."""

import logging

from arflow._core import ARFlowServicer, run_server


def serve():
    """Run a simple ARFlow server."""
    run_server(ARFlowServicer)


if __name__ == "__main__":
    logging.basicConfig()  # TODO: Replace print with logging
    serve()
