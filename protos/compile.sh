#!/usr/bin/env bash

# Generate Python gRPC files
# Temporarily prefer --mypy_out (3rd party) over the built-in --pyi_out as it yields better type hints
# python -m grpc_tools.protoc --proto_path=protos --python_out=python --pyi_out=python --grpc_python_out=python protos/arflow_grpc/*.proto
python -m grpc_tools.protoc --proto_path=protos --python_out=python --mypy_out=python --grpc_python_out=python protos/arflow_grpc/*.proto

# Generate C# gRPC files
protoc --csharp_out=unity/Assets/Scripts/ARFlow --grpc_out=unity/Assets/Scripts/ARFlow --plugin=protoc-gen-grpc=grpc_csharp_plugin protos/arflow_grpc/*.proto
