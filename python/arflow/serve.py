"""Simple server for ARFlow service."""

import logging
from concurrent import futures
from pathlib import Path
from signal import SIGINT, SIGTERM, signal
from typing import Any

import grpc

from arflow import service_pb2_grpc
from arflow.core import ARFlowServicer


def create_server(port: int = 8500, path_to_save: Path | None = None):
    """Run gRPC server."""
    servicer = ARFlowServicer()
    server = grpc.server(  # type: ignore
        futures.ThreadPoolExecutor(max_workers=10),
        options=[
            ("grpc.max_send_message_length", -1),
            ("grpc.max_receive_message_length", -1),
        ],
    )
    service_pb2_grpc.add_ARFlowServicer_to_server(servicer, server)  # type: ignore
    server.add_insecure_port("[::]:%s" % port)
    server.start()
    print(f"ARFlow server started, listening on {port}")

    def handle_shutdown(*_: Any) -> None:
        """Shutdown gracefully."""
        print("Received shutdown signal")
        # shutdows gracefully, refuse new requests, and wait for all RPCs to finish
        # returns a `threading.Event` object on which to wait
        all_rpcs_done_event = server.stop(30)
        # block/wait the object so Python doesn't exit prematurely
        all_rpcs_done_event.wait(30)

        if path_to_save is not None:
            print("Saving data...")
            servicer.on_program_exit(path_to_save)

        print("Shut down gracefully")

    signal(SIGTERM, handle_shutdown)  # request to terminate the process
    signal(SIGINT, handle_shutdown)  # request to interrupt the process
    server.wait_for_termination()


def serve():
    """Run a simple ARFlow server."""
    create_server()


if __name__ == "__main__":
    logging.basicConfig()  # TODO: Replace print with logging
    serve()
