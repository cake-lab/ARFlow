"""gRPC server tests with an end-to-end fashion."""

# ruff:noqa: D103
# pyright: reportUnknownMemberType=false, reportUnknownVariableType=false, reportUnknownArgumentType=false
# We have to do the above because of the typelessness of the grpc stub
from concurrent import futures
from typing import Any, Generator

import grpc
import numpy as np
import pytest

from arflow import ARFlowServicer
from arflow._error_interceptor import ErrorInterceptor
from arflow_grpc import service_pb2_grpc
from arflow_grpc.service_pb2 import ClientConfiguration, ClientIdentifier, DataFrame
from arflow_grpc.service_pb2_grpc import ARFlowStub


@pytest.fixture(scope="function")
def stub() -> Generator[ARFlowStub, Any, None]:
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
    service_pb2_grpc.add_ARFlowServicer_to_server(servicer, server)
    port = server.add_insecure_port("[::]:0")
    server.start()

    try:
        with grpc.insecure_channel(f"localhost:{port}") as channel:
            yield ARFlowStub(channel)
    finally:
        server.stop(None)


def test_register_client(stub: ARFlowStub):
    request = ClientConfiguration()

    response: ClientIdentifier = stub.RegisterClient(request)
    assert len(response.uid) == 36


# def test_register_client_with_init_uid(stub: ARFlowStub):
#     request = ClientConfiguration()

#     response: ClientIdentifier = stub.RegisterClient(request, init_uid="1234")
#     assert response.uid == "1234"


def test_multiple_clients(stub: ARFlowStub):
    """Flaky since UUIDs might collide."""
    uids = []
    for _ in range(3):
        request = ClientConfiguration()
        response = stub.RegisterClient(request)
        assert len(response.uid) == 36
        assert response.uid not in uids
        uids.append(response.uid)
    assert len(uids) == 3


# def test_register_same_client_twice(stub: ARFlowStub):
#     request = ClientConfiguration()
#     response = stub.RegisterClient(request)
#     uid = response.uid

#     response = stub.RegisterClient(request, init_uid=uid)
#     assert response.uid == uid


def test_process_frame(stub: ARFlowStub):
    client_config = ClientConfiguration(
        camera_color=ClientConfiguration.CameraColor(
            enabled=True, data_type="RGB24", resize_factor_x=1.0, resize_factor_y=1.0
        ),
        camera_depth=ClientConfiguration.CameraDepth(
            enabled=True, data_type="f32", resolution_x=4, resolution_y=4
        ),
        camera_transform=ClientConfiguration.CameraTransform(enabled=True),
        camera_point_cloud=ClientConfiguration.CameraPointCloud(enabled=True),
        camera_intrinsics=ClientConfiguration.CameraIntrinsics(
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

    frame = DataFrame(
        uid=client_id,
        color=np.random.randint(0, 255, 4 * 4 * 3, dtype=np.uint8).tobytes(),  # pyright: ignore [reportUnknownMemberType]
        depth=np.random.rand(4, 4).astype(np.float32).tobytes(),
        transform=np.random.rand(12).astype(np.float32).tobytes(),
    )

    response = stub.ProcessFrame(frame)
    assert response.message == "OK"


def test_process_frame_with_unregistered_client(stub: ARFlowStub):
    invalid_frame = DataFrame(uid="invalid_id")

    with pytest.raises(grpc.RpcError) as excinfo:
        stub.ProcessFrame(invalid_frame)
    assert excinfo.value.code() == grpc.StatusCode.NOT_FOUND


@pytest.mark.parametrize(
    "client_config",
    [
        ClientConfiguration(
            camera_color=(
                ClientConfiguration.CameraColor(
                    enabled=True,
                    data_type="unknown",
                )
            )
        ),
        ClientConfiguration(
            camera_depth=(
                ClientConfiguration.CameraDepth(
                    enabled=True,
                    data_type="unknown",
                )
            )
        ),
    ],
)
def test_process_frame_with_invalid_data_types(
    client_config: ClientConfiguration, stub: ARFlowStub
):
    response = stub.RegisterClient(client_config)
    client_id = response.uid
    invalid_frame = DataFrame(
        uid=client_id,
    )
    with pytest.raises(grpc.RpcError) as excinfo:
        stub.ProcessFrame(invalid_frame)
    assert excinfo.value.code() == grpc.StatusCode.INVALID_ARGUMENT


@pytest.mark.parametrize(
    "client_config, corrupted_frame",
    [
        (
            ClientConfiguration(
                camera_color=ClientConfiguration.CameraColor(
                    enabled=True,
                    data_type="RGB24",
                    resize_factor_x=1.0,
                    resize_factor_y=1.0,
                ),
                camera_intrinsics=ClientConfiguration.CameraIntrinsics(
                    resolution_x=4,
                    resolution_y=4,
                ),
            ),
            DataFrame(
                color=np.random.randint(  # pyright: ignore [reportUnknownMemberType]
                    0, 255, (4, 4, 2), dtype=np.uint8
                ).tobytes(),  # Incorrect size
            ),
        ),
        (
            ClientConfiguration(
                camera_color=ClientConfiguration.CameraColor(
                    enabled=False,
                ),
                camera_depth=ClientConfiguration.CameraDepth(
                    enabled=True, resolution_x=4, resolution_y=4, data_type="f32"
                ),
            ),
            DataFrame(
                depth=np.random.rand(4 * 4)
                .astype(np.float32)
                .tobytes()[:1],  # Incorrect size
            ),
        ),
        (
            ClientConfiguration(
                camera_color=ClientConfiguration.CameraColor(
                    enabled=False,
                    resize_factor_x=1.0,
                    resize_factor_y=1.0,
                ),
                camera_transform=ClientConfiguration.CameraTransform(enabled=True),
                camera_intrinsics=ClientConfiguration.CameraIntrinsics(
                    focal_length_x=1.0,
                    focal_length_y=1.0,
                    principal_point_x=1.0,
                    principal_point_y=1.0,
                ),
            ),
            DataFrame(
                transform=np.random.rand(8 // 4)
                .astype(np.float32)
                .tobytes(),  # Incorrect size
            ),
        ),
    ],
)
def test_process_frame_with_corrupted_data(
    client_config: ClientConfiguration,
    corrupted_frame: DataFrame,
    stub: ARFlowStub,
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
