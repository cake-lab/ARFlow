"""Global fixtures for ARFlow tests."""


# ruff:noqa: D101,D102,D103,D107
# pyright: reportPrivateUsage=false

from pathlib import Path

import pytest

from arflow import (
    ARFlowServicer,
    DecodedDataFrame,
    JoinSessionRequest,
    RegisterClientRequest,
)


@pytest.fixture
def default_service_fixture():
    """A default ARFlow service fixture that can be shared across tests."""
    return ARFlowServicer()


@pytest.fixture
def service_fixture_with_save_dir(tmp_path: Path):
    """A ARFlow service fixture with a configured save path that can be shared across tests."""
    return ARFlowServicer(spawn_viewer=False, save_dir=tmp_path)


class UserExtendedService(ARFlowServicer):
    def __init__(self):
        super().__init__()
        self.num_clients = 0
        self.num_frames = 0
        self.num_sessions = 0

    def on_register(self, request: RegisterClientRequest) -> None:
        self.num_clients += 1

    def on_frame_received(self, decoded_data_frame: DecodedDataFrame) -> None:
        self.num_frames += 1

    def on_join_session(self, request: JoinSessionRequest) -> None:
        self.num_sessions += 1
        self.num_clients += 1


@pytest.fixture
def user_service_fixture():
    """A user-extended ARFlow service fixture that can be shared across tests."""
    return UserExtendedService()
