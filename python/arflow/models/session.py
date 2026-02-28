import msgspec
from typing import List


class DeviceMsg(msgspec.Struct):
    device_id: str
    device_name: str
    os_version: str


class SessionUuidMsg(msgspec.Struct):
    value: str


class SessionMetadataMsg(msgspec.Struct):
    save_path: str = ""


class SessionMsg(msgspec.Struct):
    id: SessionUuidMsg
    metadata: SessionMetadataMsg
    devices: List[DeviceMsg]


class CreateSessionRequestMsg(msgspec.Struct):
    session_metadata: SessionMetadataMsg
    device: DeviceMsg


class CreateSessionResponseMsg(msgspec.Struct):
    session: SessionMsg


class DeleteSessionRequestMsg(msgspec.Struct):
    session_id: SessionUuidMsg


class DeleteSessionResponseMsg(msgspec.Struct):
    pass


class GetSessionRequestMsg(msgspec.Struct):
    session_id: SessionUuidMsg


class GetSessionResponseMsg(msgspec.Struct):
    session: SessionMsg


class ListSessionsRequestMsg(msgspec.Struct):
    pass


class ListSessionsResponseMsg(msgspec.Struct):
    sessions: List[SessionMsg]


class JoinSessionRequestMsg(msgspec.Struct):
    session_id: SessionUuidMsg
    device: DeviceMsg


class JoinSessionResponseMsg(msgspec.Struct):
    session: SessionMsg


class LeaveSessionRequestMsg(msgspec.Struct):
    session_id: SessionUuidMsg
    device: DeviceMsg


class LeaveSessionResponseMsg(msgspec.Struct):
    pass
