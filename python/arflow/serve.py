"""Simple server for ARFlow service."""

import logging
import sys
from concurrent import futures
from pathlib import Path

import grpc

from arflow import service_pb2_grpc
from arflow.core import ARFlowServicer


def create_server(port: int = 8500, path_to_save: Path | None = None):
    """Run gRPC server."""
    try:
        servicer = ARFlowServicer()
        server = grpc.server(
            futures.ThreadPoolExecutor(max_workers=10),
            options=[
                ("grpc.max_send_message_length", -1),
                ("grpc.max_receive_message_length", -1),
            ],
        )
        service_pb2_grpc.add_ARFlowServicer_to_server(servicer, server)
        server.add_insecure_port("[::]:%s" % port)
        server.start()
        print(f"ARFlow server started, listening on {port}")
        server.wait_for_termination()
    except KeyboardInterrupt:
        if path_to_save is not None:
            servicer._on_program_exit(path_to_save)
        sys.exit(0)


def serve():
    """Run a simple ARFlow server."""
    create_server()


if __name__ == "__main__":
    logging.basicConfig()  # TODO: Replace print with logging
    serve()
