#!/usr/bin/env bash

# TODO: use this ./generate_ci.sh >.github/workflows/build.yml

export GRPC_REQUEST_SCENARIO=${GRPC_REQUEST_SCENARIO:-"complex_proto"}
export GRPC_IMAGE_NAME="${GRPC_IMAGE_NAME:-grpc_bench}"

# Setup the chosen scenario
if ! sh setup_scenario.sh "$GRPC_REQUEST_SCENARIO" false; then
  echo "Scenario setup fiascoed."
  exit 1
fi

benchmark="arflow_server_bench"
builds=""
echo "==> Building Docker image..."
cd ..
( (
  docker image build \
    --pull \
    --file Dockerfile \
    --tag "$GRPC_IMAGE_NAME:${benchmark}-$GRPC_REQUEST_SCENARIO" \
    . >"${benchmark}.tmp" 2>&1 &&
    rm -f "${benchmark}.tmp" &&
    echo "==> Done building ${benchmark}"
) || (
  cat "${benchmark}.tmp"
  rm -f "${benchmark}.tmp"
  echo "==> Error building ${benchmark}"
  exit 1
)) &
builds="${builds} ${!}"

echo "==> Waiting for the builds to finish..."
for job in ${builds}; do
  if ! wait "${job}"; then
    wait
    echo "Error building Docker image(s)"
    exit 1
  fi
done
echo "All done."
