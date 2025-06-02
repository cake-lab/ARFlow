from google.protobuf.internal import containers as _containers
from google.protobuf import descriptor as _descriptor
from google.protobuf import message as _message
from typing import ClassVar as _ClassVar, Iterable as _Iterable, Mapping as _Mapping, Optional as _Optional, Union as _Union

DESCRIPTOR: _descriptor.FileDescriptor

class MeshFilter(_message.Message):
    __slots__ = ("instance_id", "mesh")
    class EncodedMesh(_message.Message):
        __slots__ = ("sub_meshes",)
        class EncodedSubMesh(_message.Message):
            __slots__ = ("data",)
            DATA_FIELD_NUMBER: _ClassVar[int]
            data: bytes
            def __init__(self, data: _Optional[bytes] = ...) -> None: ...
        SUB_MESHES_FIELD_NUMBER: _ClassVar[int]
        sub_meshes: _containers.RepeatedCompositeFieldContainer[MeshFilter.EncodedMesh.EncodedSubMesh]
        def __init__(self, sub_meshes: _Optional[_Iterable[_Union[MeshFilter.EncodedMesh.EncodedSubMesh, _Mapping]]] = ...) -> None: ...
    INSTANCE_ID_FIELD_NUMBER: _ClassVar[int]
    MESH_FIELD_NUMBER: _ClassVar[int]
    instance_id: int
    mesh: MeshFilter.EncodedMesh
    def __init__(self, instance_id: _Optional[int] = ..., mesh: _Optional[_Union[MeshFilter.EncodedMesh, _Mapping]] = ...) -> None: ...
