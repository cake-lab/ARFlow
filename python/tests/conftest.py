"""Global fixtures for ARFlow tests."""


# ruff:noqa: D101,D102,D103,D107
# pyright: reportPrivateUsage=false

from collections import defaultdict
from collections.abc import Sequence
from pathlib import Path
from typing import DefaultDict

import pytest

from arflow import (
    ARFlowServicer,
    ARFrame,
    Device,
)
from arflow._session_stream import SessionStream

TEST_APP_ID = "arflow-test"


@pytest.fixture
def default_service_fixture():
    """A default ARFlow service fixture that can be shared across tests."""
    return ARFlowServicer(application_id=TEST_APP_ID)


@pytest.fixture
def service_fixture_with_save_dir(tmp_path: Path):
    """A ARFlow service fixture with a configured save path that can be shared across tests."""
    return ARFlowServicer(
        spawn_viewer=False, save_dir=tmp_path, application_id=TEST_APP_ID
    )


@pytest.fixture
def device_fixture():
    """A ARFlow device fixture that can be shared across tests."""
    return Device(
        model="ARPhone 12 Pro Max",
        name="ARFlow test device",
        type=Device.Type.TYPE_HANDHELD,
        uid="test-device",
    )


class UserExtendedService(ARFlowServicer):
    def __init__(self):
        super().__init__(spawn_viewer=True, application_id=TEST_APP_ID)
        self.num_sessions = 0
        self.num_clients = 0
        self.num_frames_by_client: DefaultDict[str, int] = defaultdict(int)
        """Key is device UID, which is unique across all devices"""

    def on_create_session(self, session_stream: SessionStream, device: Device) -> None:
        self.num_sessions += 1

    def on_delete_session(self, session_stream: SessionStream) -> None:
        self.num_sessions -= 1

    def on_join_session(self, session_stream: SessionStream, device: Device) -> None:
        self.num_clients += 1

    def on_leave_session(self, session_stream: SessionStream, device: Device) -> None:
        self.num_clients -= 1

    def on_save_ar_frames(
        self, frames: Sequence[ARFrame], session_stream: SessionStream, device: Device
    ) -> None:
        self.num_frames_by_client[device.uid] += len(frames)


@pytest.fixture
def user_service_fixture():
    """A user-extended ARFlow service fixture that can be shared across tests."""
    return UserExtendedService()
