"""User-extension and internal hooks tests."""

# ruff:noqa: D101,D102,D103,D107
# pyright: reportPrivateUsage=false

from unittest.mock import MagicMock, patch

from arflow import RegisterClientRequest
from arflow._types import HashableClientIdentifier
from arflow_grpc.service_pb2 import JoinSessionRequest, ProcessFrameRequest
from tests.conftest import UserExtendedService


def test_on_register(user_service_fixture: UserExtendedService):
    request = RegisterClientRequest()
    for i in range(3):
        assert user_service_fixture.num_clients == i
        user_service_fixture.RegisterClient(request)


def test_on_frame_received(user_service_fixture: UserExtendedService):
    config = RegisterClientRequest()
    response = user_service_fixture.RegisterClient(config)
    request = ProcessFrameRequest(uid=response.uid)
    for i in range(3):
        assert user_service_fixture.num_frames == i
        user_service_fixture.ProcessFrame(request)


def test_on_join_session(user_service_fixture: UserExtendedService):
    request = RegisterClientRequest()
    response = user_service_fixture.RegisterClient(request)
    assert user_service_fixture.num_sessions == 0
    assert user_service_fixture.num_clients == 1
    join_request = JoinSessionRequest(session_uid=response.uid)
    user_service_fixture.JoinSession(join_request)
    assert user_service_fixture.num_sessions == 1
    assert user_service_fixture.num_clients == 2


def test_on_program_exit(user_service_fixture: UserExtendedService):
    with patch("rerun.disconnect") as mock_disconnect:
        mock_stream1 = MagicMock()
        mock_stream2 = MagicMock()
        user_service_fixture._client_registry = {
            HashableClientIdentifier("client1"): MagicMock(rerun_stream=mock_stream1),
            HashableClientIdentifier("client2"): MagicMock(rerun_stream=mock_stream2),
        }
        user_service_fixture.on_program_exit()

        # Verify global disconnect was called
        mock_disconnect.assert_any_call()

        # Verify disconnect was called for each client stream
        mock_disconnect.assert_any_call(mock_stream1)
        mock_disconnect.assert_any_call(mock_stream2)

        # Verify total number of calls (global + number of clients)
        assert mock_disconnect.call_count == 3
