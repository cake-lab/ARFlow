"""A library for replaying ARFlow data."""

import logging
import pickle
import threading
import time
from pathlib import Path
from typing import Type

from arflow._core import ARFlowServicer
from arflow._types import EnrichedARFlowRequest, RequestsHistory
from arflow_grpc.service_pb2 import ProcessFrameRequest, RegisterClientRequest

logger = logging.getLogger(__name__)


class ARFlowPlayer(threading.Thread):
    """A class for replaying ARFlow data."""

    def __init__(self, service: Type[ARFlowServicer], frame_data_path: Path) -> None:
        """Initialize the ARFlowPlayer."""
        super().__init__()
        self._service = service()
        self._requests_history: RequestsHistory = []

        with open(frame_data_path, "rb") as f:
            raw_data: RequestsHistory = pickle.load(f)

        if not raw_data:
            raise ValueError("No data to replay.")
        if not isinstance(raw_data[0].data, RegisterClientRequest):
            raise ValueError("The first request should be a RegisterClientRequest.")
        if not isinstance(raw_data[1].data, ProcessFrameRequest):
            raise ValueError("The second request should be a ProcessFrameRequest.")

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
        if not isinstance(sent_dataframe, ProcessFrameRequest):
            raise ValueError("The second request should be a ProcessFrameRequest.")
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

    def run(self) -> None:
        """Run the replay."""
        while True:
            current_time = time.time() - self._t0

            t = self._requests_history[self._n_frame].timestamp

            if t - current_time < 0.001:
                data = self._requests_history[self._n_frame].data
                if self._n_frame == 0 and isinstance(data, RegisterClientRequest):
                    self._service.RegisterClient(data, None, init_uid=self._uid)
                elif isinstance(data, ProcessFrameRequest):
                    self._service.ProcessFrame(data, None)
                else:
                    raise ValueError("Unknown request data type.")

                self._n_frame += 1

            if self._n_frame > len(self._requests_history) - 1:
                break

            self._sleep()

        logger.debug("Reply finished.")
        exit()
