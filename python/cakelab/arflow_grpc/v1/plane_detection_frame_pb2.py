# -*- coding: utf-8 -*-
# Generated by the protocol buffer compiler.  DO NOT EDIT!
# NO CHECKED-IN PROTOBUF GENCODE
# source: cakelab/arflow_grpc/v1/plane_detection_frame.proto
# Protobuf Python Version: 5.28.3
"""Generated protocol buffer code."""
from google.protobuf import descriptor as _descriptor
from google.protobuf import descriptor_pool as _descriptor_pool
from google.protobuf import runtime_version as _runtime_version
from google.protobuf import symbol_database as _symbol_database
from google.protobuf.internal import builder as _builder
_runtime_version.ValidateProtobufRuntimeVersion(
    _runtime_version.Domain.PUBLIC,
    5,
    28,
    3,
    '',
    'cakelab/arflow_grpc/v1/plane_detection_frame.proto'
)
# @@protoc_insertion_point(imports)

_sym_db = _symbol_database.Default()


from cakelab.arflow_grpc.v1 import ar_plane_pb2 as cakelab_dot_arflow__grpc_dot_v1_dot_ar__plane__pb2
from google.protobuf import timestamp_pb2 as google_dot_protobuf_dot_timestamp__pb2


DESCRIPTOR = _descriptor_pool.Default().AddSerializedFile(b'\n2cakelab/arflow_grpc/v1/plane_detection_frame.proto\x12\x16\x63\x61kelab.arflow_grpc.v1\x1a%cakelab/arflow_grpc/v1/ar_plane.proto\x1a\x1fgoogle/protobuf/timestamp.proto\"\xb3\x02\n\x13PlaneDetectionFrame\x12G\n\x05state\x18\x01 \x01(\x0e\x32\x31.cakelab.arflow_grpc.v1.PlaneDetectionFrame.StateR\x05state\x12\x45\n\x10\x64\x65vice_timestamp\x18\x02 \x01(\x0b\x32\x1a.google.protobuf.TimestampR\x0f\x64\x65viceTimestamp\x12\x35\n\x05plane\x18\x03 \x01(\x0b\x32\x1f.cakelab.arflow_grpc.v1.ARPlaneR\x05plane\"U\n\x05State\x12\x15\n\x11STATE_UNSPECIFIED\x10\x00\x12\x0f\n\x0bSTATE_ADDED\x10\x01\x12\x11\n\rSTATE_UPDATED\x10\x02\x12\x11\n\rSTATE_REMOVED\x10\x03\x42\xad\x01\n\x1a\x63om.cakelab.arflow_grpc.v1B\x18PlaneDetectionFrameProtoP\x01\xa2\x02\x03\x43\x41X\xaa\x02\x16\x43\x61keLab.ARFlow.Grpc.V1\xca\x02\x15\x43\x61kelab\\ArflowGrpc\\V1\xe2\x02!Cakelab\\ArflowGrpc\\V1\\GPBMetadata\xea\x02\x17\x43\x61kelab::ArflowGrpc::V1b\x06proto3')

_globals = globals()
_builder.BuildMessageAndEnumDescriptors(DESCRIPTOR, _globals)
_builder.BuildTopDescriptorsAndMessages(DESCRIPTOR, 'cakelab.arflow_grpc.v1.plane_detection_frame_pb2', _globals)
if not _descriptor._USE_C_DESCRIPTORS:
  _globals['DESCRIPTOR']._loaded_options = None
  _globals['DESCRIPTOR']._serialized_options = b'\n\032com.cakelab.arflow_grpc.v1B\030PlaneDetectionFrameProtoP\001\242\002\003CAX\252\002\026CakeLab.ARFlow.Grpc.V1\312\002\025Cakelab\\ArflowGrpc\\V1\342\002!Cakelab\\ArflowGrpc\\V1\\GPBMetadata\352\002\027Cakelab::ArflowGrpc::V1'
  _globals['_PLANEDETECTIONFRAME']._serialized_start=151
  _globals['_PLANEDETECTIONFRAME']._serialized_end=458
  _globals['_PLANEDETECTIONFRAME_STATE']._serialized_start=373
  _globals['_PLANEDETECTIONFRAME_STATE']._serialized_end=458
# @@protoc_insertion_point(module_scope)
