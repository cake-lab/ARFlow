"""End-to-end gRPC server tests."""

# ruff:noqa: D103
# pyright: reportUnknownMemberType=false, reportUnknownVariableType=false, reportUnknownArgumentType=false
# We have to do the above because of the typelessness of the grpc stub
from concurrent import futures
from typing import Any, Generator

import grpc
import numpy as np
import pytest
from arflow_grpc import service_pb2, service_pb2_grpc
from arflow_grpc.service_pb2 import (
    JoinSessionRequest,
    ProcessFrameRequest,
    RegisterClientRequest,
    RegisterClientResponse,
)
from arflow_grpc.service_pb2_grpc import ARFlowServiceStub

from arflow import ARFlowServicer
from arflow._error_interceptor import ErrorInterceptor


@pytest.fixture(scope="function")
def stub() -> Generator[ARFlowServiceStub, Any, None]:
    servicer = ARFlowServicer()
    interceptors = [ErrorInterceptor()]
    server = grpc.server(
        futures.ThreadPoolExecutor(
            max_workers=10,
        ),
        interceptors=interceptors,  # pyright: ignore [reportArgumentType]
        options=[
            ("grpc.max_send_message_length", -1),
            ("grpc.max_receive_message_length", -1),
        ],
    )
    service_pb2_grpc.add_ARFlowServiceServicer_to_server(servicer, server)
    port = server.add_insecure_port("[::]:0")
    server.start()

    try:
        with grpc.insecure_channel(f"localhost:{port}") as channel:
            yield ARFlowServiceStub(channel)
    finally:
        server.stop(None)


def test_register_client(stub: ARFlowServiceStub):
    request = RegisterClientRequest()

    response: RegisterClientResponse = stub.RegisterClient(request)
    assert len(response.uid) == 32


def test_register_client_with_init_uid(stub: ARFlowServiceStub):
    request = RegisterClientRequest(init_uid="1234")

    response: RegisterClientResponse = stub.RegisterClient(request)
    assert response.uid == "1234"


def test_multiple_clients(stub: ARFlowServiceStub):
    """Flaky since UUIDs might collide."""
    uids = []
    for _ in range(3):
        request = RegisterClientRequest()
        response = stub.RegisterClient(request)
        assert len(response.uid) == 32
        assert response.uid not in uids
        uids.append(response.uid)
    assert len(uids) == 3


def test_register_same_client_twice(stub: ARFlowServiceStub):
    request = RegisterClientRequest()
    response = stub.RegisterClient(request)
    request.init_uid = response.uid

    response = stub.RegisterClient(request)
    assert response.uid == request.init_uid


def test_join_session(stub: ARFlowServiceStub):
    request = RegisterClientRequest()
    register_response = stub.RegisterClient(request)
    join_request = JoinSessionRequest(session_uid=register_response.uid)
    join_response = stub.JoinSession(join_request)
    assert len(join_response.uid) == 32
    assert join_response.uid != register_response.uid


def test_join_nonexistent_session(stub: ARFlowServiceStub):
    request = JoinSessionRequest(session_uid="nonexistent")
    with pytest.raises(grpc.RpcError) as excinfo:
        stub.JoinSession(request)
    assert excinfo.value.code() == grpc.StatusCode.NOT_FOUND


def test_join_session_multiple_clients(stub: ARFlowServiceStub):
    request = RegisterClientRequest()
    register_response = stub.RegisterClient(request)
    for _ in range(3):
        join_request = JoinSessionRequest(session_uid=register_response.uid)
        join_response = stub.JoinSession(join_request)
        assert len(join_response.uid) == 32
        assert join_response.uid != register_response.uid


def test_join_session_chaining_multiple_clients(
    stub: ARFlowServiceStub,
):
    """Client A starts session, B joins A using A's ID, C joins B using C's ID."""
    request = RegisterClientRequest()
    register_response = stub.RegisterClient(request)
    join_request = JoinSessionRequest(session_uid=register_response.uid)
    join_response = stub.JoinSession(join_request)
    assert len(join_response.uid) == 32
    assert join_response.uid != register_response.uid
    for _ in range(3):
        previous_join_response = join_response
        join_request = JoinSessionRequest(session_uid=join_response.uid)
        join_response = stub.JoinSession(join_request)
        assert len(join_response.uid) == 32
        assert join_response.uid != previous_join_response.uid
        assert join_response.uid != register_response.uid


def test_process_frame(stub: ARFlowServiceStub):
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
    )
    response = stub.RegisterClient(client_config)
    client_id = response.uid

    frame = ProcessFrameRequest(
        uid=client_id,
        color=np.random.randint(0, 255, 4 * 4 * 3, dtype=np.uint8).tobytes(),  # pyright: ignore [reportUnknownMemberType]
        depth=np.random.rand(4, 4).astype(np.float32).tobytes(),
        transform=np.random.rand(12).astype(np.float32).tobytes(),
    )

    response = stub.ProcessFrame(frame)
    assert response.message == "OK"


def test_process_frame_with_unregistered_client(stub: ARFlowServiceStub):
    invalid_frame = ProcessFrameRequest(uid="invalid_id")

    with pytest.raises(grpc.RpcError) as excinfo:
        stub.ProcessFrame(invalid_frame)
    assert excinfo.value.code() == grpc.StatusCode.NOT_FOUND


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
    client_config: RegisterClientRequest, stub: ARFlowServiceStub
):
    response = stub.RegisterClient(client_config)
    client_id = response.uid
    invalid_frame = ProcessFrameRequest(
        uid=client_id,
    )
    with pytest.raises(grpc.RpcError) as excinfo:
        stub.ProcessFrame(invalid_frame)
    assert excinfo.value.code() == grpc.StatusCode.INVALID_ARGUMENT


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
                color=np.random.randint(  # pyright: ignore [reportUnknownMemberType]
                    0, 255, (4, 4, 2), dtype=np.uint8
                ).tobytes(),  # Incorrect size
            ),
        ),
        (
            RegisterClientRequest(
                camera_color=RegisterClientRequest.CameraColor(
                    enabled=False,
                ),
                camera_depth=RegisterClientRequest.CameraDepth(
                    enabled=True, resolution_x=4, resolution_y=4, data_type="f32"
                ),
            ),
            ProcessFrameRequest(
                depth=np.random.rand(4 * 4)
                .astype(np.float32)
                .tobytes()[:1],  # Incorrect size
            ),
        ),
        (
            RegisterClientRequest(
                camera_color=RegisterClientRequest.CameraColor(
                    enabled=False,
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
    stub: ARFlowServiceStub,
):
    response = stub.RegisterClient(
        client_config,
    )
    corrupted_frame.uid = response.uid

    with pytest.raises(grpc.RpcError) as excinfo:
        stub.ProcessFrame(
            corrupted_frame,
        )
    assert excinfo.value.code() == grpc.StatusCode.INVALID_ARGUMENT
