"""End-to-end gRPC server tests."""

# ruff:noqa: D103
# pyright: reportUnknownMemberType=false, reportUnknownVariableType=false, reportUnknownArgumentType=false
# We have to do the above because the grpc stub has no type hints
from concurrent import futures
from pathlib import Path
from typing import Any, Generator
from unittest.mock import ANY, patch

import DracoPy
import grpc
import numpy as np
import pytest
from google.protobuf.timestamp_pb2 import Timestamp

from arflow import ARFlowServicer
from arflow._error_interceptor import ErrorInterceptor
from cakelab.arflow_grpc.v1 import arflow_service_pb2_grpc
from cakelab.arflow_grpc.v1.ar_frame_pb2 import ARFrame
from cakelab.arflow_grpc.v1.ar_plane_pb2 import ARPlane
from cakelab.arflow_grpc.v1.arflow_service_pb2_grpc import ARFlowServiceStub
from cakelab.arflow_grpc.v1.audio_frame_pb2 import AudioFrame
from cakelab.arflow_grpc.v1.color_frame_pb2 import ColorFrame
from cakelab.arflow_grpc.v1.create_session_request_pb2 import CreateSessionRequest
from cakelab.arflow_grpc.v1.create_session_response_pb2 import CreateSessionResponse
from cakelab.arflow_grpc.v1.delete_session_request_pb2 import DeleteSessionRequest
from cakelab.arflow_grpc.v1.depth_frame_pb2 import DepthFrame
from cakelab.arflow_grpc.v1.device_pb2 import Device
from cakelab.arflow_grpc.v1.get_session_request_pb2 import GetSessionRequest
from cakelab.arflow_grpc.v1.gyroscope_frame_pb2 import GyroscopeFrame
from cakelab.arflow_grpc.v1.intrinsics_pb2 import Intrinsics
from cakelab.arflow_grpc.v1.join_session_request_pb2 import JoinSessionRequest
from cakelab.arflow_grpc.v1.join_session_response_pb2 import JoinSessionResponse
from cakelab.arflow_grpc.v1.leave_session_request_pb2 import LeaveSessionRequest
from cakelab.arflow_grpc.v1.list_sessions_request_pb2 import ListSessionsRequest
from cakelab.arflow_grpc.v1.list_sessions_response_pb2 import ListSessionsResponse
from cakelab.arflow_grpc.v1.mesh_detection_frame_pb2 import MeshDetectionFrame
from cakelab.arflow_grpc.v1.mesh_filter_pb2 import MeshFilter
from cakelab.arflow_grpc.v1.plane_detection_frame_pb2 import PlaneDetectionFrame
from cakelab.arflow_grpc.v1.quaternion_pb2 import Quaternion
from cakelab.arflow_grpc.v1.save_ar_frames_request_pb2 import SaveARFramesRequest
from cakelab.arflow_grpc.v1.session_pb2 import SessionUuid
from cakelab.arflow_grpc.v1.transform_frame_pb2 import TransformFrame
from cakelab.arflow_grpc.v1.vector2_int_pb2 import Vector2Int
from cakelab.arflow_grpc.v1.vector2_pb2 import Vector2
from cakelab.arflow_grpc.v1.vector3_pb2 import Vector3
from cakelab.arflow_grpc.v1.xr_cpu_image_pb2 import XRCpuImage
from tests.conftest import TEST_APP_ID


@pytest.fixture(scope="function")
def stub() -> Generator[ARFlowServiceStub, Any, None]:
    servicer = ARFlowServicer(spawn_viewer=True, application_id=TEST_APP_ID)
    interceptors = [ErrorInterceptor()]
    server = grpc.server(
        futures.ThreadPoolExecutor(
            max_workers=10,
        ),
        compression=grpc.Compression.Gzip,
        interceptors=interceptors,  # pyright: ignore [reportArgumentType]
        options=[
            ("grpc.max_receive_message_length", -1),
        ],
    )
    arflow_service_pb2_grpc.add_ARFlowServiceServicer_to_server(servicer, server)
    port = server.add_insecure_port("[::]:0")
    server.start()

    try:
        with grpc.insecure_channel(f"localhost:{port}") as channel:
            yield ARFlowServiceStub(channel)
    finally:
        server.stop(None)


def test_create_session(stub: ARFlowServiceStub):
    response: CreateSessionResponse = stub.CreateSession(CreateSessionRequest())
    assert len(response.session.id.value) == 36
    assert len(stub.ListSessions(ListSessionsRequest()).sessions) == 1


def test_delete_session(stub: ARFlowServiceStub):
    response: CreateSessionResponse = stub.CreateSession(CreateSessionRequest())
    assert len(stub.ListSessions(ListSessionsRequest()).sessions) == 1
    stub.DeleteSession(DeleteSessionRequest(session_id=response.session.id))
    assert len(stub.ListSessions(ListSessionsRequest()).sessions) == 0
    with pytest.raises(grpc.RpcError) as excinfo:
        stub.DeleteSession(
            DeleteSessionRequest(session_id=SessionUuid(value="nonexistent"))
        )
    assert excinfo.value.code() == grpc.StatusCode.NOT_FOUND


def test_get_session(stub: ARFlowServiceStub):
    response1: CreateSessionResponse = stub.CreateSession(CreateSessionRequest())
    response2 = stub.GetSession(GetSessionRequest(session_id=response1.session.id))
    assert response2.session == response1.session
    with pytest.raises(grpc.RpcError) as excinfo:
        stub.GetSession(GetSessionRequest(session_id=SessionUuid(value="nonexistent")))
    assert excinfo.value.code() == grpc.StatusCode.NOT_FOUND


def test_list_sessions(stub: ARFlowServiceStub, device_fixture: Device):
    sessions = []
    for i in range(3):
        response1: CreateSessionResponse = stub.CreateSession(
            CreateSessionRequest(
                device=Device(
                    model=device_fixture.model,
                    name=f"{device_fixture.name}_{i}",
                    type=device_fixture.type,
                    uid=f"{device_fixture.uid}_{i}",
                )
            )
        )
        sessions.append(response1.session)
    response2: ListSessionsResponse = stub.ListSessions(ListSessionsRequest())
    assert response2.sessions == sessions


def test_join_session(stub: ARFlowServiceStub):
    response1: CreateSessionResponse = stub.CreateSession(
        CreateSessionRequest(device=Device(uid="1"))
    )
    assert len(response1.session.devices) == 1
    response2: JoinSessionResponse = stub.JoinSession(
        JoinSessionRequest(session_id=response1.session.id, device=Device(uid="2"))
    )
    assert response2.session.id == response1.session.id
    assert len(response2.session.devices) == 2


def test_join_nonexistent_session(stub: ARFlowServiceStub):
    with pytest.raises(grpc.RpcError) as excinfo:
        stub.JoinSession(
            JoinSessionRequest(session_id=SessionUuid(value="nonexistent"))
        )
    assert excinfo.value.code() == grpc.StatusCode.NOT_FOUND


def test_join_session_multiple_devices(stub: ARFlowServiceStub, device_fixture: Device):
    response1: CreateSessionResponse = stub.CreateSession(
        CreateSessionRequest(device=device_fixture)
    )
    assert (
        len(
            stub.GetSession(
                GetSessionRequest(session_id=response1.session.id)
            ).session.devices
        )
        == 1
    )
    for i in range(3):
        response2: JoinSessionResponse = stub.JoinSession(
            JoinSessionRequest(
                session_id=response1.session.id, device=Device(uid=f"{i}")
            )
        )
        assert response2.session.id == response1.session.id
        assert (
            len(
                stub.GetSession(
                    GetSessionRequest(session_id=response2.session.id)
                ).session.devices
            )
            == i + 2
        )


def test_leave_session(stub: ARFlowServiceStub):
    response1 = stub.CreateSession(CreateSessionRequest(device=Device(uid="1")))
    response2: JoinSessionResponse = stub.JoinSession(
        JoinSessionRequest(session_id=response1.session.id, device=Device(uid="2"))
    )
    stub.LeaveSession(
        LeaveSessionRequest(session_id=response2.session.id, device=Device(uid="2"))
    )
    assert stub.GetSession(
        GetSessionRequest(session_id=response2.session.id)
    ).session.devices == [Device(uid="1")]
    stub.LeaveSession(
        LeaveSessionRequest(session_id=response2.session.id, device=Device(uid="1"))
    )
    assert (
        stub.GetSession(
            GetSessionRequest(session_id=response2.session.id)
        ).session.devices
        == []
    )
    with pytest.raises(grpc.RpcError) as excinfo:
        stub.LeaveSession(
            LeaveSessionRequest(session_id=SessionUuid(value="nonexistent"))
        )
    assert excinfo.value.code() == grpc.StatusCode.NOT_FOUND


def test_save_ar_frames(
    stub: ARFlowServiceStub,
    device_fixture: Device,
):
    response1: CreateSessionResponse = stub.CreateSession(
        CreateSessionRequest(device=device_fixture)
    )
    with (
        patch.object(
            ARFlowServicer, "on_save_transform_frames"
        ) as mock_on_save_transform_frames,
        patch.object(
            ARFlowServicer, "on_save_color_frames"
        ) as mock_on_save_color_frames,
        patch.object(
            ARFlowServicer, "on_save_depth_frames"
        ) as mock_on_save_depth_frames,
        patch.object(
            ARFlowServicer, "on_save_gyroscope_frames"
        ) as mock_on_save_gyroscope_frames,
        patch.object(
            ARFlowServicer, "on_save_audio_frames"
        ) as mock_on_save_audio_frames,
        patch.object(
            ARFlowServicer, "on_save_plane_detection_frames"
        ) as mock_on_save_plane_detection_frames,
        (Path(__file__).parent / "bunny.drc").open("rb") as draco_file,
        patch.object(
            ARFlowServicer, "on_save_mesh_detection_frames"
        ) as mock_on_save_mesh_detection_frames,
        patch.object(ARFlowServicer, "on_save_ar_frames") as mock_on_save_ar_frames,
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
        stub.SaveARFrames(
            SaveARFramesRequest(
                session_id=response1.session.id,
                device=device_fixture,
                frames=ar_frames,
            )
        )
        mock_on_save_transform_frames.assert_called_once_with(
            frames=transform_frames,
            # Only the session_stream key needs to be there, can be anything
            # since the actual Rerun stream is in-memory and internal in the
            # server and so hard to mock correctly
            session_stream=ANY,
            device=device_fixture,
        )
        mock_on_save_ar_frames.assert_called_with(
            frames=ar_frames,
            session_stream=ANY,
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
        stub.SaveARFrames(
            SaveARFramesRequest(
                session_id=response1.session.id,
                device=device_fixture,
                frames=ar_frames,
            )
        )
        mock_on_save_color_frames.assert_called_once_with(
            frames=color_frames,
            session_stream=ANY,
            device=device_fixture,
        )
        mock_on_save_ar_frames.assert_called_with(
            frames=ar_frames,
            session_stream=ANY,
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
        stub.SaveARFrames(
            SaveARFramesRequest(
                session_id=response1.session.id,
                device=device_fixture,
                frames=ar_frames,
            )
        )
        mock_on_save_depth_frames.assert_called_once_with(
            frames=depth_frames,
            session_stream=ANY,
            device=device_fixture,
        )
        mock_on_save_ar_frames.assert_called_with(
            frames=ar_frames,
            session_stream=ANY,
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
        stub.SaveARFrames(
            SaveARFramesRequest(
                session_id=response1.session.id,
                device=device_fixture,
                frames=ar_frames,
            )
        )
        mock_on_save_gyroscope_frames.assert_called_once_with(
            frames=gyroscope_frames,
            session_stream=ANY,
            device=device_fixture,
        )
        mock_on_save_ar_frames.assert_called_with(
            frames=ar_frames,
            session_stream=ANY,
            device=device_fixture,
        )

        audio_frames = [
            AudioFrame(
                device_timestamp=Timestamp(seconds=0, nanos=0),
                data=np.random.rand(4).astype(np.float32).tobytes(),
            )
        ]
        ar_frames = [ARFrame(audio_frame=audio_frames[0])]
        stub.SaveARFrames(
            SaveARFramesRequest(
                session_id=response1.session.id,
                device=device_fixture,
                frames=ar_frames,
            )
        )
        mock_on_save_audio_frames.assert_called_once_with(
            frames=audio_frames,
            session_stream=ANY,
            device=device_fixture,
        )
        mock_on_save_ar_frames.assert_called_with(
            frames=ar_frames,
            session_stream=ANY,
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
        stub.SaveARFrames(
            SaveARFramesRequest(
                session_id=response1.session.id,
                device=device_fixture,
                frames=ar_frames,
            )
        )
        mock_on_save_plane_detection_frames.assert_called_once_with(
            frames=plane_detection_frames,
            session_stream=ANY,
            device=device_fixture,
        )
        mock_on_save_ar_frames.assert_called_with(
            frames=ar_frames,
            session_stream=ANY,
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
        stub.SaveARFrames(
            SaveARFramesRequest(
                session_id=response1.session.id,
                device=device_fixture,
                frames=ar_frames,
            )
        )
        mock_on_save_mesh_detection_frames.assert_called_once_with(
            frames=mesh_detection_frames,
            session_stream=ANY,
            device=device_fixture,
        )
        mock_on_save_ar_frames.assert_called_with(
            frames=ar_frames,
            session_stream=ANY,
            device=device_fixture,
        )


def test_save_ar_frames_with_nonexistent_session(
    stub: ARFlowServiceStub,
):
    with pytest.raises(grpc.RpcError) as excinfo:
        stub.SaveARFrames(
            SaveARFramesRequest(session_id=SessionUuid(value="invalid_id"))
        )
    assert excinfo.value.code() == grpc.StatusCode.INVALID_ARGUMENT


def test_save_ar_frames_with_nonexistent_device_in_existing_session(
    stub: ARFlowServiceStub, device_fixture: Device
):
    stub.CreateSession(CreateSessionRequest(device=device_fixture))
    with pytest.raises(grpc.RpcError) as excinfo:
        stub.SaveARFrames(SaveARFramesRequest(device=device_fixture))
    assert excinfo.value.code() == grpc.StatusCode.INVALID_ARGUMENT
