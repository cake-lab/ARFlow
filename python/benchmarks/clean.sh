#!/usr/bin/env sh

export GRPC_IMAGE_NAME="${GRPC_IMAGE_NAME:-grpc_bench}"

benchmark="arflow_server_bench"

for scenario in $(find scenarios/ -maxdepth 1 -type d | tail -n+2 | sort); do
  scenario=${scenario##scenarios/}
  IMAGES_TO_CLEAN="${IMAGES_TO_CLEAN} ${GRPC_IMAGE_NAME}:${benchmark}-${scenario}"
done

docker image remove ${IMAGES_TO_CLEAN}
