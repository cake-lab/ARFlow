#!/usr/bin/env bash

# Generate Python gRPC files
python -m grpc_tools.protoc --proto_path=protos --python_out=python --pyi_out=python --grpc_python_out=python protos/arflow_grpc/*.proto

# Generate C# gRPC files
protoc --csharp_out=unity/Assets/Scripts/ARFlow --grpc_csharp_out=unity/Assets/Scripts/ARFlow protos/arflow_grpc/*.proto
