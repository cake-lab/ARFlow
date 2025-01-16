from cakelab.arflow_grpc.v1 import device_pb2 as _device_pb2
from google.protobuf.internal import containers as _containers
from google.protobuf import descriptor as _descriptor
from google.protobuf import message as _message
from typing import ClassVar as _ClassVar, Iterable as _Iterable, Mapping as _Mapping, Optional as _Optional, Union as _Union

DESCRIPTOR: _descriptor.FileDescriptor

class SessionUuid(_message.Message):
    __slots__ = ("value",)
    VALUE_FIELD_NUMBER: _ClassVar[int]
    value: str
    def __init__(self, value: _Optional[str] = ...) -> None: ...

class SessionMetadata(_message.Message):
    __slots__ = ("name", "save_path")
    NAME_FIELD_NUMBER: _ClassVar[int]
    SAVE_PATH_FIELD_NUMBER: _ClassVar[int]
    name: str
    save_path: str
    def __init__(self, name: _Optional[str] = ..., save_path: _Optional[str] = ...) -> None: ...

class Session(_message.Message):
    __slots__ = ("id", "metadata", "devices")
    ID_FIELD_NUMBER: _ClassVar[int]
    METADATA_FIELD_NUMBER: _ClassVar[int]
    DEVICES_FIELD_NUMBER: _ClassVar[int]
    id: SessionUuid
    metadata: SessionMetadata
    devices: _containers.RepeatedCompositeFieldContainer[_device_pb2.Device]
    def __init__(self, id: _Optional[_Union[SessionUuid, _Mapping]] = ..., metadata: _Optional[_Union[SessionMetadata, _Mapping]] = ..., devices: _Optional[_Iterable[_Union[_device_pb2.Device, _Mapping]]] = ...) -> None: ...
