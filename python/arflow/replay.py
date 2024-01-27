"""A library for replaying ARFlow data."""
import pickle
import threading
import time
from typing import List

from .serve import ARFlowService
from .service_pb2 import DataFrameRequest, RegisterRequest


class ARFlowPlayer(threading.Thread):
    """A class for replaying ARFlow data."""

    service: ARFlowService
    frame_data: List
    n_frame: int

    def __init__(self, service: ARFlowService, frame_data_path: str) -> None:
        super().__init__()
        self.service = service()
        with open(frame_data_path, "rb") as f:
            raw_data = pickle.load(f)

        self.frame_data = []
        start_delta = 0
        for i, data in enumerate(raw_data):
            if i == 0:
                start_delta = data["time_stamp"] - 3
                self.frame_data.append(
                    {
                        "time_stamp": data["time_stamp"] - start_delta,
                        "data": RegisterRequest.FromString(data["data"]),
                    }
                )
            else:
                self.frame_data.append(
                    {
                        "time_stamp": data["time_stamp"] - start_delta,
                        "data": DataFrameRequest.FromString(data["data"]),
                    }
                )

        self.uid = self.frame_data[1]["data"].uid

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
        while True:
            current_time = time.time() - self.t0

            t = self.frame_data[self.n_frame]["time_stamp"]

            if t - current_time < 0.001:
                data = self.frame_data[self.n_frame]["data"]
                if self.n_frame == 0:
                    self.service.register(data, None, uid=self.uid)
                else:
                    self.service.data_frame(data, None)

                self.n_frame += 1

            if self.n_frame > len(self.frame_data) - 1:
                break

            self.sleep()

        print("Reply finished.")
        exit()
