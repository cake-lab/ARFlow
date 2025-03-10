#!/usr/bin/env python3

# ruff:noqa: D100, D101, D103

from pathlib import Path

from google.protobuf.json_format import MessageToJson

from cakelab.arflow_grpc.v1.ar_frame_pb2 import ARFrame
from cakelab.arflow_grpc.v1.create_session_request_pb2 import CreateSessionRequest
from cakelab.arflow_grpc.v1.device_pb2 import Device
from cakelab.arflow_grpc.v1.save_ar_frames_request_pb2 import SaveARFramesRequest
from cakelab.arflow_grpc.v1.session_pb2 import SessionMetadata, SessionUuid

SCENARIOS_DIR = "scenarios"


def main() -> None:
    device = Device(
        model="iPhone 12",
        name="iPhone 12",
        type=Device.TYPE_HANDHELD,
        uid="f3131490-dddd-419a-8504-fa8bb55282b2",
    )
    message = SaveARFramesRequest(
        session_id=SessionUuid(value="SESSION_ID_PLACEHOLDER"),
        device=device,
        frames=[ARFrame(), ARFrame()],
    )
    message_as_json = MessageToJson(
        message=message, preserving_proto_field_name=True, indent=None
    )
    with open(Path(SCENARIOS_DIR, "complex_proto", "payload"), "w") as f:
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
