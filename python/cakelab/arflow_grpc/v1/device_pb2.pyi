from google.protobuf.internal import enum_type_wrapper as _enum_type_wrapper
from google.protobuf import descriptor as _descriptor
from google.protobuf import message as _message
from typing import ClassVar as _ClassVar, Optional as _Optional, Union as _Union

DESCRIPTOR: _descriptor.FileDescriptor

class Device(_message.Message):
    __slots__ = ("model", "name", "type", "uid")
    class Type(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
        __slots__ = ()
        TYPE_UNSPECIFIED: _ClassVar[Device.Type]
        TYPE_HANDHELD: _ClassVar[Device.Type]
        TYPE_CONSOLE: _ClassVar[Device.Type]
        TYPE_DESKTOP: _ClassVar[Device.Type]
    TYPE_UNSPECIFIED: Device.Type
    TYPE_HANDHELD: Device.Type
    TYPE_CONSOLE: Device.Type
    TYPE_DESKTOP: Device.Type
    MODEL_FIELD_NUMBER: _ClassVar[int]
    NAME_FIELD_NUMBER: _ClassVar[int]
    TYPE_FIELD_NUMBER: _ClassVar[int]
    UID_FIELD_NUMBER: _ClassVar[int]
    model: str
    name: str
    type: Device.Type
    uid: str
    def __init__(self, model: _Optional[str] = ..., name: _Optional[str] = ..., type: _Optional[_Union[Device.Type, str]] = ..., uid: _Optional[str] = ...) -> None: ...
