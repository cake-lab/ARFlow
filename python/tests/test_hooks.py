"""User-extension hooks tests."""

# ruff:noqa: D101,D102,D103,D107
# pyright: reportPrivateUsage=false

import tempfile
from pathlib import Path

import pytest

from arflow import ARFlowServicer, DecodedDataFrame, RegisterClientRequest
from arflow._types import EnrichedARFlowRequest, HashableClientIdentifier
from arflow_grpc.service_pb2 import ProcessFrameRequest


class UserExtendedService(ARFlowServicer):
    def __init__(self):
        super().__init__()
        self.num_clients = 0
        self.num_frames = 0

    def on_register(self, request: RegisterClientRequest) -> None:
        self.num_clients += 1

    def on_frame_received(self, decoded_data_frame: DecodedDataFrame) -> None:
        self.num_frames += 1


@pytest.fixture
def user_service():
    """A user-extended ARFlow service that can be shared across tests."""
    return UserExtendedService()


def test_on_register(user_service: UserExtendedService):
    request = RegisterClientRequest()
    for i in range(3):
        assert user_service.num_clients == i
        user_service.RegisterClient(request)


def test_on_frame_received(user_service: UserExtendedService):
    config = RegisterClientRequest()
    response = user_service.RegisterClient(config)
    request = ProcessFrameRequest(uid=response.uid)
    for i in range(3):
        assert user_service.num_frames == i
        user_service.ProcessFrame(request)


def test_on_program_exit(user_service: UserExtendedService):
    # Add some mock data to the service
    enriched_request = EnrichedARFlowRequest(timestamp=1, data=ProcessFrameRequest())
    user_service._requests_history.append(enriched_request)
    client_id = HashableClientIdentifier("test_client")
    user_service._client_configurations[client_id] = RegisterClientRequest()

    # Use tempfile to create a temporary directory
    with tempfile.TemporaryDirectory() as temp_dir:
        temp_path = Path(temp_dir)

        # Call on_program_exit
        user_service.on_program_exit(temp_path)

        # Check the results
        pkl_files = list(temp_path.glob("*.pkl"))
        assert len(pkl_files) == 1
        pkl_file = pkl_files[0]
        assert pkl_file.exists()
        assert pkl_file.stat().st_size > 0

    # No need for manual cleanup - the TemporaryDirectory context manager handles it
