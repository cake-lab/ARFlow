"""Simple WebSocket server for ARFlow service."""

import sys
import asyncio
import logging
from concurrent import futures

from arflow.core import ARFlowWebSocketServer

logger = logging.getLogger(__name__)

async def create_server(
    service_class, port: int = 8500, path_to_save: str | None = "./"
):
    """Run WebSocket server."""
    service = service_class()
    try:
        from websockets.asyncio.server import serve
        async with serve(service.handle_client, "0.0.0.0", port):
            print(f"ARFlow WebSocket server started on port {port}")
            await asyncio.Future()  # run forever
            
    except asyncio.CancelledError:
        pass
    except KeyboardInterrupt:
        if path_to_save is not None:
            service.on_program_exit(path_to_save)
        sys.exit(0)

def serve():
    """Run a simple ARFlow WebSocket server."""
    logging.basicConfig(level=logging.INFO)
    try:
        asyncio.run(create_server(ARFlowWebSocketServer))
    except KeyboardInterrupt:
        print("Server stopped.")

if __name__ == "__main__":
    serve()

