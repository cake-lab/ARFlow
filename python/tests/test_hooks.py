"""User-extension and internal hooks tests."""

# ruff:noqa: D101,D102,D103,D107
# pyright: reportPrivateUsage=false

from unittest.mock import MagicMock, patch

import rerun as rr
from google.protobuf.timestamp_pb2 import Timestamp

from arflow import Device
from arflow._session_stream import SessionStream
from cakelab.arflow_grpc.v1.ar_frame_pb2 import ARFrame
from cakelab.arflow_grpc.v1.audio_frame_pb2 import AudioFrame
from cakelab.arflow_grpc.v1.create_session_request_pb2 import CreateSessionRequest
from cakelab.arflow_grpc.v1.delete_session_request_pb2 import DeleteSessionRequest
from cakelab.arflow_grpc.v1.join_session_request_pb2 import JoinSessionRequest
from cakelab.arflow_grpc.v1.save_ar_frames_request_pb2 import SaveARFramesRequest
from cakelab.arflow_grpc.v1.session_pb2 import Session, SessionUuid
from tests.conftest import TEST_APP_ID, UserExtendedService


def test_on_create_session(user_service_fixture: UserExtendedService):
    request = CreateSessionRequest()
    response1 = user_service_fixture.CreateSession(request)
    assert user_service_fixture.num_sessions == 1
    request = CreateSessionRequest()
    response2 = user_service_fixture.CreateSession(request)
    assert user_service_fixture.num_sessions == 2

    # can have multiple sessions with same name (their ID will be different)
    assert response1.session.metadata.name == response2.session.metadata.name
    assert response1.session.id != response2.session.id


def test_on_delete_session(user_service_fixture: UserExtendedService):
    with patch("rerun.disconnect"):
        user_service_fixture.num_sessions = 2
        user_service_fixture.client_sessions = {
            "session1": SessionStream(
                info=Session(id=SessionUuid(value="session1")), stream=MagicMock()
            ),
            "session2": SessionStream(
                info=Session(id=SessionUuid(value="session2")), stream=MagicMock()
            ),
        }
        request = DeleteSessionRequest(session_id=SessionUuid(value="session1"))
        user_service_fixture.DeleteSession(request)
        assert user_service_fixture.num_sessions == 1
        request = DeleteSessionRequest(session_id=SessionUuid(value="session2"))
        user_service_fixture.DeleteSession(request)
        assert user_service_fixture.num_sessions == 0


def test_on_join_session(user_service_fixture: UserExtendedService):
    user_service_fixture.client_sessions = {
        "session1": SessionStream(
            info=Session(id=SessionUuid(value="session1")), stream=MagicMock()
        ),
    }
    for i in range(3):
        device = Device(
            model=f"ARPhone 12 Pro Max {i}",
            name=f"ARFlow test device {i}",
            type=Device.Type.TYPE_HANDHELD,
            uid=f"test-device-{i}",
        )
        user_service_fixture.JoinSession(
            JoinSessionRequest(session_id=SessionUuid(value="session1"), device=device)
        )
        assert user_service_fixture.num_clients == i + 1


def test_on_save_ar_frames(
    user_service_fixture: UserExtendedService, device_fixture: Device
):
    recording_stream = rr.new_recording(
        application_id=TEST_APP_ID, recording_id="session1", spawn=True
    )
    user_service_fixture.client_sessions = {
        "session1": SessionStream(
            info=Session(
                id=SessionUuid(value="session1"),
                devices=[device_fixture],
            ),
            stream=recording_stream,
        ),
    }
    for i in range(3):
        user_service_fixture.SaveARFrames(
            SaveARFramesRequest(
                session_id=SessionUuid(value="session1"),
                device=device_fixture,
                frames=[
                    ARFrame(
                        audio_frame=AudioFrame(
                            device_timestamp=Timestamp(seconds=1234, nanos=1234),
                            data=[1, 2, 3],
                        ),
                    ),
                    ARFrame(
                        audio_frame=AudioFrame(
                            device_timestamp=Timestamp(seconds=1234, nanos=1235),
                            data=[1, 2, 3],
                        ),
                    ),
                ],
            )
        )
        assert (
            user_service_fixture.num_frames_by_client[device_fixture.uid] == (i + 1) * 2
        )


def test_on_server_exit(user_service_fixture: UserExtendedService):
    with patch("rerun.disconnect") as mock_disconnect:
        mock_stream1 = MagicMock()
        mock_stream2 = MagicMock()
        user_service_fixture.client_sessions = {
            "client1": MagicMock(stream=mock_stream1),
            "client2": MagicMock(stream=mock_stream2),
        }
        user_service_fixture.on_server_exit()

        # Verify global disconnect was called
        mock_disconnect.assert_any_call()

        # Verify disconnect was called for each client stream
        mock_disconnect.assert_any_call(mock_stream1)
        mock_disconnect.assert_any_call(mock_stream2)

        # Verify total number of calls (global + number of clients)
        assert mock_disconnect.call_count == 3
