#!/bin/sh

python -m grpc_tools.protoc -Iprotos --python_out=python --pyi_out=python --grpc_python_out=python protos/arflow/*.proto
protoc --csharp_out=unity/Assets/Scripts/ARFlow --grpc_csharp_out=unity/Assets/Scripts/ARFlow protos/arflow/*.proto
