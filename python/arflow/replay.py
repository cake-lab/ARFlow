"""A library for replaying ARFlow data."""

import pickle
import threading
import time
from pathlib import Path
from typing import Type

from arflow.core import ARFlowServicer
from arflow.service_pb2 import ClientConfiguration, DataFrame
from arflow.types import EnrichedARFlowRequest, RequestsHistory


class ARFlowPlayer(threading.Thread):
    """A class for replaying ARFlow data."""

    def __init__(self, service: Type[ARFlowServicer], frame_data_path: Path) -> None:
        """Initialize the ARFlowPlayer."""
        super().__init__()
        self._service = service()
        self._requests_history: RequestsHistory = []
        with open(frame_data_path, "rb") as f:
            raw_data: RequestsHistory = pickle.load(f)

        start_delta = 0
        for i, data in enumerate(raw_data):
            if i == 0:
                start_delta = data.timestamp - 3
                self._requests_history.append(
                    EnrichedARFlowRequest(
                        timestamp=data.timestamp - start_delta,
                        data=data.data,
                    )
                )
            else:
                self._requests_history.append(
                    EnrichedARFlowRequest(
                        timestamp=data.timestamp - start_delta,
                        data=data.data,
                    )
                )

        sent_dataframe = self._requests_history[1].data
        if not isinstance(sent_dataframe, DataFrame):
            raise ValueError("The second request should be a DataFrame.")
        else:
            self._uid = sent_dataframe.uid

        self._period = 0.001  # Simulate a 1ms loop.
        self._n_frame = 0

        self._i = 0
        self._t0 = time.time()
        self.start()

    def _sleep(self):
        self._i += 1
        delta = self._t0 + self._period * self._i - time.time()
        if delta > 0:
            time.sleep(delta)

    def run(self):
        """Run the replay."""
        while True:
            current_time = time.time() - self._t0

            t = self._requests_history[self._n_frame].timestamp

            if t - current_time < 0.001:
                data = self._requests_history[self._n_frame].data
                if self._n_frame == 0 and isinstance(data, ClientConfiguration):
                    self._service.RegisterClient(data, None, init_uid=self._uid)
                elif isinstance(data, DataFrame):
                    self._service.ProcessFrame(data, None)
                else:
                    raise ValueError("Unknown request data type.")

                self._n_frame += 1

            if self._n_frame > len(self._requests_history) - 1:
                break

            self._sleep()

        print("Reply finished.")
        exit()
