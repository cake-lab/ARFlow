"""gRPC service tests."""

# ruff:noqa: D103
# pyright: reportPrivateUsage=false

from pathlib import Path
from unittest.mock import patch

import DracoPy
import grpc
import grpc_interceptor
import grpc_interceptor.exceptions
import numpy as np
import pytest

from arflow import ARFlowServicer, DecodedDataFrame, RegisterClientRequest
from arflow._types import HashableClientIdentifier
from arflow_grpc import service_pb2
from arflow_grpc.service_pb2 import ProcessFrameRequest


@pytest.fixture
def default_service():
    """A default ARFlow service that can be shared across tests."""
    return ARFlowServicer()


def test_save_request(default_service: ARFlowServicer):
    request = RegisterClientRequest()
    default_service._save_request(request)

    assert len(default_service._requests_history) == 1
    enriched_request = default_service._requests_history[0]
    assert enriched_request.timestamp >= 0
    assert enriched_request.data == request

    request = ProcessFrameRequest()
    default_service._save_request(request)

    assert len(default_service._requests_history) == 2
    enriched_request = default_service._requests_history[1]
    assert enriched_request.timestamp >= 0
    assert enriched_request.data == request
    assert (
        default_service._requests_history[0].timestamp
        <= default_service._requests_history[1].timestamp
    )


def test_register_client(default_service: ARFlowServicer):
    request = RegisterClientRequest()

    response = default_service.RegisterClient(request)
    assert len(response.uid) == 36


def test_register_client_with_init_uid(default_service: ARFlowServicer):
    request = RegisterClientRequest()

    response = default_service.RegisterClient(request, init_uid="1234")
    assert response.uid == "1234"


def test_multiple_clients(default_service: ARFlowServicer):
    """Flaky since UUIDs might collide."""
    # Register multiple clients
    for _ in range(3):
        request = RegisterClientRequest()
        response = default_service.RegisterClient(request)
        assert len(response.uid) == 36

    assert len(default_service._client_configurations) == 3


def test_register_same_client_twice(default_service: ARFlowServicer):
    request = RegisterClientRequest()
    response1 = default_service.RegisterClient(request)
    response2 = default_service.RegisterClient(request, init_uid=response1.uid)

    assert response1.uid == response2.uid


@pytest.mark.parametrize(
    "client_config,expected_enabled",
    [
        (
            RegisterClientRequest(
                camera_color=RegisterClientRequest.CameraColor(enabled=True)
            ),
            True,
        ),
        (
            RegisterClientRequest(
                camera_color=RegisterClientRequest.CameraColor(enabled=False)
            ),
            False,
        ),
    ],
)
def test_ensure_correct_config(
    default_service: ARFlowServicer,
    client_config: RegisterClientRequest,
    expected_enabled: bool,
):
    response = default_service.RegisterClient(client_config)
    client_id = HashableClientIdentifier(response.uid)
    assert (
        default_service._client_configurations[client_id].camera_color.enabled
        == expected_enabled
    )


def test_process_frame(default_service: ARFlowServicer):
    client_config = RegisterClientRequest(
        camera_color=RegisterClientRequest.CameraColor(
            enabled=True, data_type="RGB24", resize_factor_x=1.0, resize_factor_y=1.0
        ),
        camera_depth=RegisterClientRequest.CameraDepth(
            enabled=True, data_type="f32", resolution_x=4, resolution_y=4
        ),
        camera_transform=RegisterClientRequest.CameraTransform(enabled=True),
        camera_point_cloud=RegisterClientRequest.CameraPointCloud(enabled=True),
        camera_intrinsics=RegisterClientRequest.CameraIntrinsics(
            resolution_x=4,
            resolution_y=4,
            focal_length_x=1.0,
            focal_length_y=1.0,
            principal_point_x=1.0,
            principal_point_y=1.0,
        ),
        camera_plane_detection=RegisterClientRequest.CameraPlaneDetection(enabled=True),
        gyroscope=RegisterClientRequest.Gyroscope(enabled=True),
        audio=RegisterClientRequest.Audio(enabled=True),
        meshing=RegisterClientRequest.Meshing(enabled=True),
    )
    response = default_service.RegisterClient(client_config)
    client_id = response.uid

    with open(Path(__file__).parent / "bunny.drc", "rb") as draco_file:
        mesh = DracoPy.decode(draco_file.read())  # pyright: ignore [reportUnknownMemberType, reportUnknownVariableType]
        mock_frame = ProcessFrameRequest(
            uid=client_id,
            color=np.random.randint(0, 255, 4 * 4 * 3, dtype=np.uint8).tobytes(),  # pyright: ignore [reportUnknownMemberType]
            depth=np.random.rand(4, 4).astype(np.float32).tobytes(),
            transform=np.random.rand(12).astype(np.float32).tobytes(),
            plane_detection=[
                service_pb2.ProcessFrameRequest.Plane(
                    center=service_pb2.ProcessFrameRequest.Vector3(x=1.0, y=2.0, z=3.0),
                    normal=service_pb2.ProcessFrameRequest.Vector3(x=1.0, y=2.0, z=3.0),
                    size=service_pb2.ProcessFrameRequest.Vector2(x=1.0, y=2.0),
                    boundary_points=[
                        service_pb2.ProcessFrameRequest.Vector2(x=1.0, y=2.0),
                        service_pb2.ProcessFrameRequest.Vector2(x=2.0, y=3.0),
                        service_pb2.ProcessFrameRequest.Vector2(x=1.0, y=3.0),
                    ],
                )
            ],
            gyroscope=service_pb2.ProcessFrameRequest.GyroscopeData(
                attitude=service_pb2.ProcessFrameRequest.Quaternion(
                    x=1.0, y=2.0, z=3.0, w=4.0
                ),
                rotation_rate=service_pb2.ProcessFrameRequest.Vector3(
                    x=1.0, y=2.0, z=3.0
                ),
                gravity=service_pb2.ProcessFrameRequest.Vector3(x=1.0, y=2.0, z=3.0),
                acceleration=service_pb2.ProcessFrameRequest.Vector3(
                    x=1.0, y=2.0, z=3.0
                ),
            ),
            audio_data=np.random.rand(4).astype(np.float32),
            meshes=[
                service_pb2.ProcessFrameRequest.Mesh(
                    data=DracoPy.encode(  # pyright: ignore [reportUnknownMemberType, reportUnknownArgumentType]
                        mesh.points,  # pyright: ignore [reportUnknownMemberType]
                        faces=mesh.faces,  # pyright: ignore [reportUnknownMemberType]
                        colors=mesh.colors,  # pyright: ignore [reportUnknownMemberType]
                    )
                )
            ],
        )

    with patch.object(default_service, "on_frame_received") as mock_on_frame:
        response = default_service.ProcessFrame(mock_frame)
        assert response.message == "OK"
        mock_on_frame.assert_called_once()
        assert isinstance(mock_on_frame.call_args[0][0], DecodedDataFrame)


def test_process_frame_with_unregistered_client(default_service: ARFlowServicer):
    invalid_frame = ProcessFrameRequest(uid="invalid_id")
    with pytest.raises(grpc_interceptor.exceptions.GrpcException) as excinfo:
        default_service.ProcessFrame(invalid_frame)
    assert excinfo.value.status_code == grpc.StatusCode.NOT_FOUND


@pytest.mark.parametrize(
    "client_config",
    [
        RegisterClientRequest(
            camera_color=(
                RegisterClientRequest.CameraColor(
                    enabled=True,
                    data_type="unknown",
                )
            )
        ),
        RegisterClientRequest(
            camera_depth=(
                RegisterClientRequest.CameraDepth(
                    enabled=True,
                    data_type="unknown",
                )
            )
        ),
    ],
)
def test_process_frame_with_invalid_data_types(
    client_config: RegisterClientRequest, default_service: ARFlowServicer
):
    response = default_service.RegisterClient(client_config)
    client_id = response.uid
    invalid_frame = ProcessFrameRequest(
        uid=client_id,
    )
    with pytest.raises(grpc_interceptor.exceptions.GrpcException) as excinfo:
        default_service.ProcessFrame(invalid_frame)
    assert excinfo.value.status_code == grpc.StatusCode.INVALID_ARGUMENT


@pytest.mark.parametrize(
    "client_config, corrupted_frame",
    [
        (
            RegisterClientRequest(
                camera_color=RegisterClientRequest.CameraColor(
                    enabled=True,
                    data_type="RGB24",
                    resize_factor_x=1.0,
                    resize_factor_y=1.0,
                ),
                camera_intrinsics=RegisterClientRequest.CameraIntrinsics(
                    resolution_x=4,
                    resolution_y=4,
                ),
            ),
            ProcessFrameRequest(
                uid="1234",
                color=np.random.randint(  # pyright: ignore [reportUnknownMemberType]
                    0, 255, (4, 4, 2), dtype=np.uint8
                ).tobytes(),  # Incorrect size
            ),
        ),
        (
            RegisterClientRequest(
                camera_depth=RegisterClientRequest.CameraDepth(
                    enabled=True, resolution_x=4, resolution_y=4, data_type="f32"
                ),
            ),
            ProcessFrameRequest(
                uid="1234",
                depth=np.random.rand(4 * 4)
                .astype(np.float32)
                .tobytes()[:1],  # Incorrect size
            ),
        ),
        (
            RegisterClientRequest(
                camera_color=RegisterClientRequest.CameraColor(
                    resize_factor_x=1.0,
                    resize_factor_y=1.0,
                ),
                camera_transform=RegisterClientRequest.CameraTransform(enabled=True),
                camera_intrinsics=RegisterClientRequest.CameraIntrinsics(
                    focal_length_x=1.0,
                    focal_length_y=1.0,
                    principal_point_x=1.0,
                    principal_point_y=1.0,
                ),
            ),
            ProcessFrameRequest(
                uid="1234",
                transform=np.random.rand(8 // 4)
                .astype(np.float32)
                .tobytes(),  # Incorrect size
            ),
        ),
        (
            RegisterClientRequest(
                camera_plane_detection=RegisterClientRequest.CameraPlaneDetection(
                    enabled=True
                ),
            ),
            ProcessFrameRequest(
                uid="1234",
                plane_detection=[
                    service_pb2.ProcessFrameRequest.Plane(
                        center=service_pb2.ProcessFrameRequest.Vector3(
                            x=1.0, y=2.0, z=3.0
                        ),
                        normal=service_pb2.ProcessFrameRequest.Vector3(
                            x=1.0, y=2.0, z=3.0
                        ),
                        size=service_pb2.ProcessFrameRequest.Vector2(x=1.0, y=2.0),
                        boundary_points=[
                            service_pb2.ProcessFrameRequest.Vector2(x=1.0, y=2.0),
                            service_pb2.ProcessFrameRequest.Vector2(x=2.0, y=3.0),
                            # Missing one point
                        ],
                    )
                ],
            ),
        ),
    ],
)
def test_process_frame_with_corrupted_data(
    client_config: RegisterClientRequest,
    corrupted_frame: ProcessFrameRequest,
    default_service: ARFlowServicer,
):
    default_service.RegisterClient(
        client_config,
        init_uid="1234",
    )

    with pytest.raises(grpc_interceptor.exceptions.InvalidArgument) as excinfo:
        default_service.ProcessFrame(
            corrupted_frame,
        )
    assert excinfo.value.status_code == grpc.StatusCode.INVALID_ARGUMENT
