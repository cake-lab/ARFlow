"""Simple server for ARFlow service."""

import sys
from concurrent import futures

import grpc

from arflow import service_pb2_grpc
from arflow.core import ARFlowService


def create_server(
    service: ARFlowService, port: int = 8500, path_to_save: str | None = "./"
):
    """Run gRPC server."""
    try:
        service = service()
        server = grpc.server(
            futures.ThreadPoolExecutor(max_workers=10),
            options=[
                ("grpc.max_send_message_length", -1),
                ("grpc.max_receive_message_length", -1),
            ],
        )
        service_pb2_grpc.add_ARFlowServiceServicer_to_server(service, server)
        server.add_insecure_port("[::]:%s" % port)
        server.start()

        print(f"ARFlow server started on port {port}")
        server.wait_for_termination()
    except KeyboardInterrupt:
        if path_to_save is not None:
            service.on_program_exit(path_to_save)
        sys.exit(0)

    # except Exception as e:
    #     print(e)


def serve():
    """Run a simple ARFlow server."""
    create_server(ARFlowService)


if __name__ == "__main__":
    serve()
