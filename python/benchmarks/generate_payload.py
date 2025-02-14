#!/usr/bin/env python3

# ruff:noqa: D100, D101, D103

import argparse
import os
from pathlib import Path

import numpy as np
from google.protobuf.json_format import MessageToJson
from google.protobuf.timestamp_pb2 import Timestamp

from cakelab.arflow_grpc.v1.ar_frame_pb2 import ARFrame
from cakelab.arflow_grpc.v1.audio_frame_pb2 import AudioFrame
from cakelab.arflow_grpc.v1.color_frame_pb2 import ColorFrame
from cakelab.arflow_grpc.v1.depth_frame_pb2 import DepthFrame
from cakelab.arflow_grpc.v1.device_pb2 import Device

# from cakelab.arflow_grpc.v1.gyroscope_frame_pb2 import GyroscopeFrame
from cakelab.arflow_grpc.v1.intrinsics_pb2 import Intrinsics

# from cakelab.arflow_grpc.v1.quaternion_pb2 import Quaternion
from cakelab.arflow_grpc.v1.save_ar_frames_request_pb2 import SaveARFramesRequest
from cakelab.arflow_grpc.v1.session_pb2 import SessionUuid

# from cakelab.arflow_grpc.v1.transform_frame_pb2 import TransformFrame
from cakelab.arflow_grpc.v1.vector2_int_pb2 import Vector2Int
from cakelab.arflow_grpc.v1.vector2_pb2 import Vector2

# from cakelab.arflow_grpc.v1.vector3_pb2 import Vector3
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
    args = parser.parse_args()

    frames = []
    if "light" in args.scenario:
        # transform sampling interval is 50ms so 20Hz, default send interval is
        # 0.5s, so each time we send 10 transform frames
        frames = [
            ARFrame(
                # transform_frame=TransformFrame(
                #     device_timestamp=Timestamp(seconds=i, nanos=0),
                #     data=np.random.rand(12).astype(np.float32).tobytes(),
                # ),
                # gyroscope_frame=GyroscopeFrame(
                #     device_timestamp=Timestamp(seconds=i, nanos=0),
                #     attitude=Quaternion(x=1.0, y=2.0, z=3.0, w=4.0),
                #     rotation_rate=Vector3(x=1.0, y=2.0, z=3.0),
                #     gravity=Vector3(x=1.0, y=2.0, z=3.0),
                #     acceleration=Vector3(x=1.0, y=2.0, z=3.0),
                # ),
                audio_frame=AudioFrame(
                    device_timestamp=Timestamp(seconds=i, nanos=0),
                    data=np.random.rand(4).astype(np.float32).tobytes(),
                )
            )
            # TODO: Interesting: different frame lengths yield very
            # different results
            for i in range(100)
        ]
    elif "medium" == args.scenario:
        # 60fps camera with default 0.5s send interval gives us 30 depth frames
        # per request
        frames = [
            ARFrame(
                depth_frame=DepthFrame(
                    device_timestamp=Timestamp(seconds=i, nanos=0),
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
                ),
            )
            for i in range(30)
        ]
    elif "heavy" == args.scenario:
        # same here,  30 color frames per request
        # TODO: This does not work yet, because of the extra padding on Android
        # assumption we make on the server.
        frames = [
            ARFrame(
                color_frame=ColorFrame(
                    device_timestamp=Timestamp(seconds=i, nanos=0),
                    image=XRCpuImage(
                        dimensions=Vector2Int(x=4, y=4),
                        format=XRCpuImage.FORMAT_ANDROID_YUV_420_888,
                        timestamp=0,
                        planes=[
                            XRCpuImage.Plane(
                                data=np.random.randint(  # pyright: ignore [reportUnknownMemberType]
                                    0, 255, (4, 4), dtype=np.uint8
                                ).tobytes(),
                                pixel_stride=1,
                                row_stride=4,
                            ),
                            XRCpuImage.Plane(
                                data=np.random.randint(  # pyright: ignore [reportUnknownMemberType]
                                    0, 255, (2, 4), dtype=np.uint8
                                ).tobytes(),
                                pixel_stride=2,
                                row_stride=4,
                            ),
                            XRCpuImage.Plane(
                                data=np.random.randint(  # pyright: ignore [reportUnknownMemberType]
                                    0, 255, (2, 4), dtype=np.uint8
                                ).tobytes(),
                                pixel_stride=2,
                                row_stride=4,
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
            )
            for i in range(30)
        ]
    elif "mixed" == args.scenario:
        frames = []
    else:
        raise ValueError(f"Invalid scenario: {args.scenario}")

    device = Device(
        model="iPhone 12",
        name="iPhone 12",
        type=Device.TYPE_HANDHELD,
        uid="f3131490-dddd-419a-8504-fa8bb55282b2",
    )
    message = SaveARFramesRequest(
        session_id=SessionUuid(value=args.session_id),
        device=device,
        frames=frames,
    )
    message_as_json = MessageToJson(
        message=message, preserving_proto_field_name=True, indent=None
    )
    os.makedirs(args.scenario, exist_ok=True)
    with open(f"{SCENARIOS_DIR}/{args.scenario}/payload", "w") as f:
        f.write(message_as_json)

    # message = CreateSessionRequest(
    #     device=device,
    # )
    # message_as_json = MessageToJson(
    #     message=message, preserving_proto_field_name=True, indent=None
    # )
    # with open(Path(SCENARIOS_DIR, "session"), "w") as f:
    #     f.write(message_as_json)

    # message_as_bin = message.SerializeToString()
    # with open("bing.bin", "wb") as f:
    #     f.write(message_as_bin)


if __name__ == "__main__":
    main()
