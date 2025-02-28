"""gRPC service tests."""

# ruff:noqa: D103
# pyright: reportPrivateUsage=false

from pathlib import Path
from unittest.mock import MagicMock, patch

import DracoPy
import grpc
import grpc_interceptor
import grpc_interceptor.exceptions
import numpy as np
import pytest
import rerun as rr
from google.protobuf.timestamp_pb2 import Timestamp

from arflow import ARFlowServicer
from arflow._session_stream import SessionStream
from cakelab.arflow_grpc.v1.ar_frame_pb2 import ARFrame
from cakelab.arflow_grpc.v1.ar_plane_pb2 import ARPlane
from cakelab.arflow_grpc.v1.audio_frame_pb2 import AudioFrame
from cakelab.arflow_grpc.v1.color_frame_pb2 import ColorFrame
from cakelab.arflow_grpc.v1.create_session_request_pb2 import CreateSessionRequest
from cakelab.arflow_grpc.v1.delete_session_request_pb2 import DeleteSessionRequest
from cakelab.arflow_grpc.v1.depth_frame_pb2 import DepthFrame
from cakelab.arflow_grpc.v1.device_pb2 import Device
from cakelab.arflow_grpc.v1.get_session_request_pb2 import GetSessionRequest
from cakelab.arflow_grpc.v1.gyroscope_frame_pb2 import GyroscopeFrame
from cakelab.arflow_grpc.v1.intrinsics_pb2 import Intrinsics
from cakelab.arflow_grpc.v1.join_session_request_pb2 import JoinSessionRequest
from cakelab.arflow_grpc.v1.leave_session_request_pb2 import LeaveSessionRequest
from cakelab.arflow_grpc.v1.list_sessions_request_pb2 import ListSessionsRequest
from cakelab.arflow_grpc.v1.mesh_detection_frame_pb2 import MeshDetectionFrame
from cakelab.arflow_grpc.v1.mesh_filter_pb2 import MeshFilter
from cakelab.arflow_grpc.v1.plane_detection_frame_pb2 import PlaneDetectionFrame
from cakelab.arflow_grpc.v1.quaternion_pb2 import Quaternion
from cakelab.arflow_grpc.v1.save_ar_frames_request_pb2 import SaveARFramesRequest
from cakelab.arflow_grpc.v1.session_pb2 import Session, SessionUuid
from cakelab.arflow_grpc.v1.transform_frame_pb2 import TransformFrame
from cakelab.arflow_grpc.v1.vector2_int_pb2 import Vector2Int
from cakelab.arflow_grpc.v1.vector2_pb2 import Vector2
from cakelab.arflow_grpc.v1.vector3_pb2 import Vector3
from cakelab.arflow_grpc.v1.xr_cpu_image_pb2 import XRCpuImage
from tests.conftest import TEST_APP_ID


@pytest.mark.parametrize(
    "spawn_viewer, save_dir",
    [(False, None), (True, Path())],
)
def test_invalid_servicer_init(spawn_viewer: bool, save_dir: Path | None):
    with pytest.raises(ValueError):
        ARFlowServicer(spawn_viewer=spawn_viewer, save_dir=save_dir)


def test_create_session(default_service_fixture: ARFlowServicer):
    request = CreateSessionRequest()
    response = default_service_fixture.CreateSession(request)
    assert len(response.session.id.value) == 36


def test_create_session_with_save_dir(
    service_fixture_with_save_dir: ARFlowServicer,
):
    request = CreateSessionRequest()
    with patch("rerun.save") as mock_save:
        service_fixture_with_save_dir.CreateSession(request)
        mock_save.assert_called_once()


def test_delete_session(default_service_fixture: ARFlowServicer):
    with patch("rerun.disconnect"):
        default_service_fixture.client_sessions = {
            "session1": SessionStream(
                info=Session(id=SessionUuid(value="session1")), stream=MagicMock()
            ),
        }
        request = DeleteSessionRequest(session_id=SessionUuid(value="session1"))
        default_service_fixture.DeleteSession(request)
        assert default_service_fixture.client_sessions == {}
        with pytest.raises(grpc_interceptor.exceptions.GrpcException) as excinfo:
            default_service_fixture.DeleteSession(
                DeleteSessionRequest(session_id=SessionUuid(value="nonexistent"))
            )
        assert excinfo.value.status_code == grpc.StatusCode.NOT_FOUND


def test_get_session(default_service_fixture: ARFlowServicer):
    default_service_fixture.client_sessions = {
        "session1": SessionStream(
            info=Session(id=SessionUuid(value="session1")), stream=MagicMock()
        ),
    }
    request = GetSessionRequest(session_id=SessionUuid(value="session1"))
    response = default_service_fixture.GetSession(request)
    assert response.session.id.value == "session1"
    with pytest.raises(grpc_interceptor.exceptions.GrpcException) as excinfo:
        default_service_fixture.GetSession(
            GetSessionRequest(session_id=SessionUuid(value="nonexistent"))
        )
    assert excinfo.value.status_code == grpc.StatusCode.NOT_FOUND


def test_list_sessions(default_service_fixture: ARFlowServicer):
    sessions = {
        "session1": SessionStream(
            info=Session(id=SessionUuid(value="session1")), stream=MagicMock()
        ),
        "session2": SessionStream(
            info=Session(id=SessionUuid(value="session2")), stream=MagicMock()
        ),
        "session3": SessionStream(
            info=Session(id=SessionUuid(value="session3")), stream=MagicMock()
        ),
    }
    default_service_fixture.client_sessions = sessions
    request = ListSessionsRequest()
    response = default_service_fixture.ListSessions(request)
    assert response.sessions == [s.info for s in sessions.values()]


def test_join_session(default_service_fixture: ARFlowServicer):
    default_service_fixture.client_sessions = {
        "session1": SessionStream(
            info=Session(id=SessionUuid(value="session1")), stream=MagicMock()
        ),
    }
    request = JoinSessionRequest(session_id=SessionUuid(value="session1"))
    response = default_service_fixture.JoinSession(request)
    assert response.session.id.value == "session1"


def test_join_nonexistent_session(default_service_fixture: ARFlowServicer):
    request = JoinSessionRequest(session_id=SessionUuid(value="nonexistent"))
    with pytest.raises(grpc_interceptor.exceptions.GrpcException) as excinfo:
        default_service_fixture.JoinSession(request)
    assert excinfo.value.status_code == grpc.StatusCode.NOT_FOUND


def test_join_session_multiple_devices(default_service_fixture: ARFlowServicer):
    default_service_fixture.client_sessions = {
        "session1": SessionStream(
            info=Session(id=SessionUuid(value="session1")), stream=MagicMock()
        ),
    }
    for i in range(3):
        join_request = JoinSessionRequest(
            session_id=SessionUuid(value="session1"),
            device=Device(name=f"name_{i}"),
        )
        default_service_fixture.JoinSession(join_request)
        assert (
            len(default_service_fixture.client_sessions["session1"].info.devices)
            == i + 1
        )


def test_leave_session(default_service_fixture: ARFlowServicer, device_fixture: Device):
    default_service_fixture.client_sessions = {
        "session1": SessionStream(
            info=Session(id=SessionUuid(value="session1"), devices=[device_fixture]),
            stream=MagicMock(),
        ),
    }
    request = LeaveSessionRequest(
        session_id=SessionUuid(value="session1"), device=device_fixture
    )
    default_service_fixture.LeaveSession(request)
    assert default_service_fixture.client_sessions["session1"].info.devices == []


def test_leave_nonexistent_session(
    default_service_fixture: ARFlowServicer, device_fixture: Device
):
    request = LeaveSessionRequest(
        session_id=SessionUuid(value="nonexistent"), device=device_fixture
    )
    with pytest.raises(grpc_interceptor.exceptions.GrpcException) as excinfo:
        default_service_fixture.LeaveSession(request)
    assert excinfo.value.status_code == grpc.StatusCode.NOT_FOUND


def test_leave_session_multiple_devices(
    default_service_fixture: ARFlowServicer, device_fixture: Device
):
    devices = [
        Device(
            model=device_fixture.model,
            name=f"{device_fixture.name}_{i}",
            type=device_fixture.type,
            uid=f"{device_fixture.uid}_{i}",
        )
        for i in range(3)
    ]
    default_service_fixture.client_sessions = {
        "session1": SessionStream(
            info=Session(
                id=SessionUuid(value="session1"),
                devices=devices,
            ),
            stream=MagicMock(),
        ),
    }
    for i, device in enumerate(devices):
        request = LeaveSessionRequest(
            session_id=SessionUuid(value="session1"), device=device
        )
        default_service_fixture.LeaveSession(request)
        assert (
            len(default_service_fixture.client_sessions["session1"].info.devices)
            == len(devices) - 1 - i
        )


def test_save_ar_frames(
    default_service_fixture: ARFlowServicer,
    device_fixture: Device,
):
    recording_stream = rr.new_recording(
        application_id=TEST_APP_ID, recording_id="session1", spawn=True
    )
    default_service_fixture.client_sessions = {
        "session1": SessionStream(
            info=Session(
                id=SessionUuid(value="session1"),
                devices=[device_fixture],
            ),
            stream=recording_stream,
        ),
    }

    with (
        patch.object(
            default_service_fixture, "on_save_transform_frames"
        ) as mock_on_save_transform_frames,
        patch.object(
            default_service_fixture, "on_save_color_frames"
        ) as mock_on_save_color_frames,
        patch.object(
            default_service_fixture, "on_save_depth_frames"
        ) as mock_on_save_depth_frames,
        patch.object(
            default_service_fixture, "on_save_gyroscope_frames"
        ) as mock_on_save_gyroscope_frames,
        patch.object(
            default_service_fixture, "on_save_audio_frames"
        ) as mock_on_save_audio_frames,
        patch.object(
            default_service_fixture, "on_save_plane_detection_frames"
        ) as mock_on_save_plane_detection_frames,
        (Path(__file__).parent / "bunny.drc").open("rb") as draco_file,
        patch.object(
            default_service_fixture, "on_save_mesh_detection_frames"
        ) as mock_on_save_mesh_detection_frames,
        patch.object(
            default_service_fixture, "on_save_ar_frames"
        ) as mock_on_save_ar_frames,
    ):
        transform_frames = [
            TransformFrame(
                device_timestamp=Timestamp(seconds=0, nanos=0),
                data=np.random.rand(12).astype(np.float32).tobytes(),
            )
        ]
        ar_frames = [
            ARFrame(
                transform_frame=transform_frames[0],
            )
        ]
        default_service_fixture.SaveARFrames(
            SaveARFramesRequest(
                session_id=SessionUuid(value="session1"),
                device=device_fixture,
                frames=ar_frames,
            )
        )
        mock_on_save_transform_frames.assert_called_once_with(
            frames=transform_frames,
            session_stream=default_service_fixture.client_sessions["session1"],
            device=device_fixture,
        )
        mock_on_save_ar_frames.assert_called_with(
            frames=ar_frames,
            session_stream=default_service_fixture.client_sessions["session1"],
            device=device_fixture,
        )

        color_frames = [
            ColorFrame(
                device_timestamp=Timestamp(seconds=0, nanos=0),
                image=XRCpuImage(
                    dimensions=Vector2Int(x=4, y=4),
                    format=XRCpuImage.FORMAT_ANDROID_YUV_420_888,
                    timestamp=0,
                    planes=[
                        XRCpuImage.Plane(
                            data=np.random.randint(  # pyright: ignore [reportUnknownMemberType]
                                0, 256, (4, 4), dtype=np.uint8
                            ).tobytes(),
                            pixel_stride=1,
                            row_stride=4,
                        ),
                        XRCpuImage.Plane(
                            data=np.random.randint(  # pyright: ignore [reportUnknownMemberType]
                                0, 256, (2, 2), dtype=np.uint8
                            ).tobytes()[:-1],  # Trim one byte
                            pixel_stride=1,
                            row_stride=2,
                        ),
                        XRCpuImage.Plane(
                            data=np.random.randint(  # pyright: ignore [reportUnknownMemberType]
                                0, 256, (2, 2), dtype=np.uint8
                            ).tobytes()[:-1],  # Trim one byte
                            pixel_stride=1,
                            row_stride=2,
                        ),
                    ],
                ),
                intrinsics=Intrinsics(
                    focal_length=Vector2(
                        x=1.0,
                        y=1.0,
                    ),
                    principal_point=Vector2(
                        x=1.0,
                        y=1.0,
                    ),
                    resolution=Vector2Int(
                        x=4,
                        y=4,
                    ),
                ),
            )
        ]
        ar_frames = [
            ARFrame(
                color_frame=color_frames[0],
            )
        ]
        default_service_fixture.SaveARFrames(
            SaveARFramesRequest(
                session_id=SessionUuid(value="session1"),
                device=device_fixture,
                frames=ar_frames,
            )
        )
        mock_on_save_color_frames.assert_called_once_with(
            frames=color_frames,
            session_stream=default_service_fixture.client_sessions["session1"],
            device=device_fixture,
        )
        mock_on_save_ar_frames.assert_called_with(
            frames=ar_frames,
            session_stream=default_service_fixture.client_sessions["session1"],
            device=device_fixture,
        )

        depth_frames = [
            DepthFrame(
                device_timestamp=Timestamp(seconds=0, nanos=0),
                environment_depth_temporal_smoothing_enabled=True,
                image=XRCpuImage(
                    dimensions=Vector2Int(x=4, y=4),
                    format=XRCpuImage.FORMAT_DEPTHUINT16,
                    timestamp=0,
                    planes=[
                        XRCpuImage.Plane(
                            data=np.random.randint(  # pyright: ignore [reportUnknownMemberType]
                                0, 255, (4, 4), dtype=np.uint16
                            ).tobytes(),
                        )
                    ],
                ),
            )
        ]
        ar_frames = [ARFrame(depth_frame=depth_frames[0])]
        default_service_fixture.SaveARFrames(
            SaveARFramesRequest(
                session_id=SessionUuid(value="session1"),
                device=device_fixture,
                frames=ar_frames,
            )
        )
        mock_on_save_depth_frames.assert_called_once_with(
            frames=depth_frames,
            session_stream=default_service_fixture.client_sessions["session1"],
            device=device_fixture,
        )
        mock_on_save_ar_frames.assert_called_with(
            frames=ar_frames,
            session_stream=default_service_fixture.client_sessions["session1"],
            device=device_fixture,
        )

        gyroscope_frames = [
            GyroscopeFrame(
                device_timestamp=Timestamp(seconds=0, nanos=0),
                attitude=Quaternion(x=1.0, y=2.0, z=3.0, w=4.0),
                rotation_rate=Vector3(x=1.0, y=2.0, z=3.0),
                gravity=Vector3(x=1.0, y=2.0, z=3.0),
                acceleration=Vector3(x=1.0, y=2.0, z=3.0),
            )
        ]
        ar_frames = [ARFrame(gyroscope_frame=gyroscope_frames[0])]
        default_service_fixture.SaveARFrames(
            SaveARFramesRequest(
                session_id=SessionUuid(value="session1"),
                device=device_fixture,
                frames=ar_frames,
            )
        )
        mock_on_save_gyroscope_frames.assert_called_once_with(
            frames=gyroscope_frames,
            session_stream=default_service_fixture.client_sessions["session1"],
            device=device_fixture,
        )
        mock_on_save_ar_frames.assert_called_with(
            frames=ar_frames,
            session_stream=default_service_fixture.client_sessions["session1"],
            device=device_fixture,
        )

        audio_frames = [
            AudioFrame(
                device_timestamp=Timestamp(seconds=0, nanos=0),
                data=np.random.rand(4).astype(np.float32).tobytes(),
            )
        ]
        ar_frames = [ARFrame(audio_frame=audio_frames[0])]
        default_service_fixture.SaveARFrames(
            SaveARFramesRequest(
                session_id=SessionUuid(value="session1"),
                device=device_fixture,
                frames=ar_frames,
            )
        )
        mock_on_save_audio_frames.assert_called_once_with(
            frames=audio_frames,
            session_stream=default_service_fixture.client_sessions["session1"],
            device=device_fixture,
        )
        mock_on_save_ar_frames.assert_called_with(
            frames=ar_frames,
            session_stream=default_service_fixture.client_sessions["session1"],
            device=device_fixture,
        )

        plane_detection_frames = [
            PlaneDetectionFrame(
                state=PlaneDetectionFrame.STATE_ADDED,
                device_timestamp=Timestamp(seconds=0, nanos=0),
                plane=ARPlane(
                    center=Vector3(x=1.0, y=2.0, z=3.0),
                    normal=Vector3(x=1.0, y=2.0, z=3.0),
                    size=Vector2(x=1.0, y=2.0),
                    boundary=[
                        Vector2(x=1.0, y=2.0),
                        Vector2(x=2.0, y=3.0),
                        Vector2(x=1.0, y=3.0),
                    ],
                ),
            )
        ]
        ar_frames = [ARFrame(plane_detection_frame=plane_detection_frames[0])]
        default_service_fixture.SaveARFrames(
            SaveARFramesRequest(
                session_id=SessionUuid(value="session1"),
                device=device_fixture,
                frames=ar_frames,
            )
        )
        mock_on_save_plane_detection_frames.assert_called_once_with(
            frames=plane_detection_frames,
            session_stream=default_service_fixture.client_sessions["session1"],
            device=device_fixture,
        )
        mock_on_save_ar_frames.assert_called_with(
            frames=ar_frames,
            session_stream=default_service_fixture.client_sessions["session1"],
            device=device_fixture,
        )

        # TODO: point cloud detection

        mesh = DracoPy.decode(draco_file.read())  # pyright: ignore [reportUnknownMemberType, reportUnknownVariableType]
        mesh_detection_frames = [
            MeshDetectionFrame(
                state=MeshDetectionFrame.STATE_ADDED,
                device_timestamp=Timestamp(seconds=0, nanos=0),
                mesh_filter=MeshFilter(
                    instance_id=1234,
                    mesh=MeshFilter.EncodedMesh(
                        sub_meshes=[
                            MeshFilter.EncodedMesh.EncodedSubMesh(
                                data=DracoPy.encode(  # pyright: ignore [reportUnknownMemberType, reportUnknownArgumentType]
                                    mesh.points,  # pyright: ignore [reportUnknownMemberType]
                                    faces=mesh.faces,  # pyright: ignore [reportUnknownMemberType]
                                    colors=mesh.colors,  # pyright: ignore [reportUnknownMemberType]
                                ),
                            )
                        ],
                    ),
                ),
            )
        ]
        ar_frames = [ARFrame(mesh_detection_frame=mesh_detection_frames[0])]
        default_service_fixture.SaveARFrames(
            SaveARFramesRequest(
                session_id=SessionUuid(value="session1"),
                device=device_fixture,
                frames=ar_frames,
            )
        )
        mock_on_save_mesh_detection_frames.assert_called_once_with(
            frames=mesh_detection_frames,
            session_stream=default_service_fixture.client_sessions["session1"],
            device=device_fixture,
        )
        mock_on_save_ar_frames.assert_called_with(
            frames=ar_frames,
            session_stream=default_service_fixture.client_sessions["session1"],
            device=device_fixture,
        )


def test_save_ar_frames_with_nonexistent_session(
    default_service_fixture: ARFlowServicer,
):
    invalid_frame = SaveARFramesRequest(session_id=SessionUuid(value="invalid_id"))
    with pytest.raises(grpc_interceptor.exceptions.GrpcException) as excinfo:
        default_service_fixture.SaveARFrames(invalid_frame)
    assert excinfo.value.status_code == grpc.StatusCode.INVALID_ARGUMENT


def test_save_ar_frames_with_nonexistent_device_in_existing_session(
    default_service_fixture: ARFlowServicer, device_fixture: Device
):
    default_service_fixture.client_sessions = {
        "session1": MagicMock(),
    }
    invalid_frame = SaveARFramesRequest(device=device_fixture)
    with pytest.raises(grpc_interceptor.exceptions.GrpcException) as excinfo:
        default_service_fixture.SaveARFrames(invalid_frame)
    assert excinfo.value.status_code == grpc.StatusCode.INVALID_ARGUMENT
