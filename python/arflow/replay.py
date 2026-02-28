"""A library for replaying ARFlow data."""

import pickle
import threading
import time
from typing import List

from arflow.core import ARFlowWebSocketServer


class ARFlowPlayer(threading.Thread):
    """A class for replaying ARFlow data (Pending Refactor for WebSocket)."""

    service: ARFlowWebSocketServer
    frame_data: List
    n_frame: int

    def __init__(self, service: ARFlowService, frame_data_path: str) -> None:
        super().__init__()
        self.start()

    # The original implementation using RegisterRequest and DataFrameRequest
    # from service_pb2 has been temporarily commented out to resolve import crashes.
    # It should be refactored to use `SessionMsg` and MsgPack in the future.

        self.period = 0.001  # Simulate a 1ms loop.
        self.n_frame = 0

        self.i = 0
        self.t0 = time.time()
        self.start()

    def sleep(self):
        self.i += 1
        delta = self.t0 + self.period * self.i - time.time()
        if delta > 0:
            time.sleep(delta)

    def run(self):
        print("Reply (WebSocket Version) not yet implemented.")
        exit()
        exit()
