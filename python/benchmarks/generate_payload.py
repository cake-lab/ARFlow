#!/usr/bin/env python3

# ruff:noqa: D100, D101, D103

import argparse
from pathlib import Path

from google.protobuf.json_format import MessageToJson

from cakelab.arflow_grpc.v1.ar_frame_pb2 import ARFrame
from cakelab.arflow_grpc.v1.device_pb2 import Device
from cakelab.arflow_grpc.v1.save_ar_frames_request_pb2 import SaveARFramesRequest
from cakelab.arflow_grpc.v1.session_pb2 import SessionUuid

SCENARIOS_DIR = "scenarios"


def main() -> None:
    # list out directories in scenarios
    scenarios = [str(d) for d in Path(SCENARIOS_DIR).iterdir() if d.is_dir()]
    parser = argparse.ArgumentParser(description="Generate payload for AR frames.")
    parser.add_argument(
        "--scenario", required=True, choices=scenarios, help="Scenario name"
    )
    parser.add_argument(
        "--session-id",
        required=True,
        type=str,
        help="Session ID to replace placeholder",
    )
    parser.add_argument("-o", "--output", required=True, help="Output file path")
    args = parser.parse_args()

    device = Device(
        model="iPhone 12",
        name="iPhone 12",
        type=Device.TYPE_HANDHELD,
        uid="f3131490-dddd-419a-8504-fa8bb55282b2",
    )
    message = SaveARFramesRequest(
        session_id=SessionUuid(value=args.session_id),
        device=device,
        frames=[ARFrame(), ARFrame()],
    )
    message_as_json = MessageToJson(
        message=message, preserving_proto_field_name=True, indent=None
    )
    with open(args.output, "w") as f:
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
