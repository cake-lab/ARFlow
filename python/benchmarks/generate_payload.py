#!/usr/bin/env python3

# ruff:noqa: D100, D101, D103

import argparse
import os
from pathlib import Path

import numpy as np
from google.protobuf.json_format import MessageToJson
from google.protobuf.timestamp_pb2 import Timestamp

from cakelab.arflow_grpc.v1.ar_frame_pb2 import ARFrame
from cakelab.arflow_grpc.v1.color_frame_pb2 import ColorFrame
from cakelab.arflow_grpc.v1.device_pb2 import Device
from cakelab.arflow_grpc.v1.intrinsics_pb2 import Intrinsics
from cakelab.arflow_grpc.v1.save_ar_frames_request_pb2 import SaveARFramesRequest
from cakelab.arflow_grpc.v1.session_pb2 import SessionUuid
from cakelab.arflow_grpc.v1.transform_frame_pb2 import TransformFrame
from cakelab.arflow_grpc.v1.vector2_int_pb2 import Vector2Int
from cakelab.arflow_grpc.v1.vector2_pb2 import Vector2
from cakelab.arflow_grpc.v1.xr_cpu_image_pb2 import XRCpuImage

SCENARIOS_DIR = "scenarios"


def main() -> None:
    scenarios = [
        str(d)[len(SCENARIOS_DIR) + 1 :]
        for d in Path(SCENARIOS_DIR).iterdir()
        if d.is_dir()
    ]
    parser = argparse.ArgumentParser(description="Generate payload for AR frames.")
    parser.add_argument(
        "--session-id",
        required=True,
        type=str,
        help="Session ID to send frames to",
    )
    parser.add_argument(
        "--scenario",
        required=True,
        type=str,
        choices=scenarios,
        help="Scenario to generate payload",
    )
    parser.add_argument(
        "--frames-per-request",
        required=True,
        type=int,
        help="Number of AR frames per request",
    )
    args = parser.parse_args()

    round_robin_frames = []
    if "light" == args.scenario:
        round_robin_frames = [
            [
                ARFrame(
                    transform_frame=TransformFrame(
                        device_timestamp=Timestamp(seconds=i, nanos=0),
                        data=np.random.rand(12).astype(np.float32).tobytes(),
                    ),
                )
                for i in range(args.frames_per_request)
            ]
        ]
    elif "heavy" == args.scenario:
        width, height = 640, 480
        uv_width, uv_height = width // 2, height // 2
        y_plane_data = np.random.randint(
            0, 256, (height, width), dtype=np.uint8
        ).tobytes()
        u_plane_data = np.random.randint(
            0, 256, (uv_height, uv_width), dtype=np.uint8
        ).tobytes()[:-1]  # Trim one byte
        v_plane_data = np.random.randint(
            0, 256, (uv_height, uv_width), dtype=np.uint8
        ).tobytes()[:-1]  # Trim one byte
        y_row_stride = width
        uv_row_stride = uv_width
        y_pixel_stride = 1
        uv_pixel_stride = 1
        round_robin_frames = [
            [
                ARFrame(
                    color_frame=ColorFrame(
                        device_timestamp=Timestamp(seconds=i, nanos=0),
                        image=XRCpuImage(
                            dimensions=Vector2Int(x=width, y=height),
                            format=XRCpuImage.FORMAT_ANDROID_YUV_420_888,
                            timestamp=0,
                            planes=[
                                XRCpuImage.Plane(
                                    data=y_plane_data,
                                    pixel_stride=y_pixel_stride,
                                    row_stride=y_row_stride,
                                ),
                                XRCpuImage.Plane(
                                    data=u_plane_data,
                                    pixel_stride=uv_pixel_stride,
                                    row_stride=uv_row_stride,
                                ),
                                XRCpuImage.Plane(
                                    data=v_plane_data,
                                    pixel_stride=uv_pixel_stride,
                                    row_stride=uv_row_stride,
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
                                x=width,
                                y=height,
                            ),
                        ),
                    )
                )
                for i in range(args.frames_per_request)
            ]
        ]
    elif "mixed" == args.scenario:
        width, height = 640, 480
        uv_width, uv_height = width // 2, height // 2
        y_plane_data = np.random.randint(
            0, 256, (height, width), dtype=np.uint8
        ).tobytes()
        u_plane_data = np.random.randint(
            0, 256, (uv_height, uv_width), dtype=np.uint8
        ).tobytes()[:-1]  # Trim one byte
        v_plane_data = np.random.randint(
            0, 256, (uv_height, uv_width), dtype=np.uint8
        ).tobytes()[:-1]  # Trim one byte
        y_row_stride = width
        uv_row_stride = uv_width
        y_pixel_stride = 1
        uv_pixel_stride = 1
        round_robin_frames = [
            [
                ARFrame(
                    transform_frame=TransformFrame(
                        device_timestamp=Timestamp(seconds=i, nanos=0),
                        data=np.random.rand(12).astype(np.float32).tobytes(),
                    ),
                )
                for i in range(args.frames_per_request)
            ],
            [
                ARFrame(
                    color_frame=ColorFrame(
                        device_timestamp=Timestamp(seconds=i, nanos=0),
                        image=XRCpuImage(
                            dimensions=Vector2Int(x=width, y=height),
                            format=XRCpuImage.FORMAT_ANDROID_YUV_420_888,
                            timestamp=0,
                            planes=[
                                XRCpuImage.Plane(
                                    data=y_plane_data,
                                    pixel_stride=y_pixel_stride,
                                    row_stride=y_row_stride,
                                ),
                                XRCpuImage.Plane(
                                    data=u_plane_data,
                                    pixel_stride=uv_pixel_stride,
                                    row_stride=uv_row_stride,
                                ),
                                XRCpuImage.Plane(
                                    data=v_plane_data,
                                    pixel_stride=uv_pixel_stride,
                                    row_stride=uv_row_stride,
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
                                x=width,
                                y=height,
                            ),
                        ),
                    )
                )
                for i in range(args.frames_per_request)
            ],
        ]
    else:
        raise ValueError(f"Invalid scenario: {args.scenario}")

    device = Device(
        model="iPhone 12",
        name="iPhone 12",
        type=Device.TYPE_HANDHELD,
        uid="f3131490-dddd-419a-8504-fa8bb55282b2",
    )
    messages = [
        SaveARFramesRequest(
            session_id=SessionUuid(value=args.session_id),
            device=device,
            frames=frames,
        )
        for frames in round_robin_frames
    ]
    messages_as_json = [
        MessageToJson(message=message, preserving_proto_field_name=True, indent=None)
        for message in messages
    ]
    payload_as_json = f"[{','.join(messages_as_json)}]"
    os.makedirs(f"{SCENARIOS_DIR}/{args.scenario}", exist_ok=True)
    with open(f"{SCENARIOS_DIR}/{args.scenario}/payload", "w") as f:
        f.write(payload_as_json)


if __name__ == "__main__":
    main()
