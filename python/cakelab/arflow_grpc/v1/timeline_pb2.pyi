from google.protobuf.internal import enum_type_wrapper as _enum_type_wrapper
from google.protobuf import descriptor as _descriptor
from typing import ClassVar as _ClassVar

DESCRIPTOR: _descriptor.FileDescriptor

class Timeline(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    TIMELINE_UNSPECIFIED: _ClassVar[Timeline]
    TIMELINE_DEVICE_TIMESTAMP: _ClassVar[Timeline]
    TIMELINE_IMAGE_TIMESTAMP: _ClassVar[Timeline]
TIMELINE_UNSPECIFIED: Timeline
TIMELINE_DEVICE_TIMESTAMP: Timeline
TIMELINE_IMAGE_TIMESTAMP: Timeline
