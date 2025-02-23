#!/usr/bin/env bash

RESULTS_DIR="results/$(date '+%y%m%dT%H%M%S')"
export GRPC_BENCHMARK_DURATION=${GRPC_BENCHMARK_DURATION:-"20s"}
export GRPC_BENCHMARK_WARMUP=${GRPC_BENCHMARK_WARMUP:-"5s"}
export GRPC_SERVER_CPUS=${GRPC_SERVER_CPUS:-"1"}
export GRPC_SERVER_RAM=${GRPC_SERVER_RAM:-"512m"}
export GRPC_CLIENT_CONNECTIONS=${GRPC_CLIENT_CONNECTIONS:-"50"}
export GRPC_CLIENT_CONCURRENCY=${GRPC_CLIENT_CONCURRENCY:-"1000"}
export GRPC_CLIENT_RPS=${GRPC_CLIENT_RPS:-"0"}
export GRPC_CLIENT_CPUS=${GRPC_CLIENT_CPUS:-"1"}
export GRPC_CLIENT_FRAMES_PER_REQUEST=${GRPC_CLIENT_FRAMES_PER_REQUEST:-"50"}
export GRPC_REQUEST_SCENARIO=${GRPC_REQUEST_SCENARIO:-"mixed"}
export GRPC_IMAGE_NAME="${GRPC_IMAGE_NAME:-grpc_bench}"
export GRPC_GHZ_TAG="${GRPC_GHZ_TAG:-0.114.0}"

# Let containers know how many CPUs they will be running on
# Additionally export other vars for further analysis script.
# export GRPC_SERVER_CPUS
# export GRPC_CLIENT_CPUS
# export GRPC_BENCHMARK_DURATION
# export GRPC_BENCHMARK_WARMUP
# export GRPC_CLIENT_CONNECTIONS
# export GRPC_CLIENT_CONCURRENCY
# export GRPC_CLIENT_QPS

wait_on_tcp50051() {
  for ((i = 1; i <= 10 * 30; i++)); do
    nc -z localhost 50051 && return 0
    sleep .1
  done
  return 1
}

benchmark="arflow_server_bench"
echo "==> Running benchmark for ${benchmark}..."

mkdir -p "${RESULTS_DIR}"

# Start the local Rerun Viewer
poetry run rerun &

# Start the gRPC Server container
docker run \
  --name "${benchmark}" \
  --rm \
  --cpus "${GRPC_SERVER_CPUS}" \
  --memory "${GRPC_SERVER_RAM}" \
  -e GRPC_SERVER_CPUS \
  -e GRPC_SERVER_RAM \
  -e PORT=50051 \
  --network=host \
  --detach \
  --tty \
  "$GRPC_IMAGE_NAME:${benchmark}-$GRPC_REQUEST_SCENARIO" >/dev/null

printf 'Waiting for server to come up... '
if ! wait_on_tcp50051; then
  echo 'server unresponsive!'
  exit 1
fi
echo 'ready.'

# Set up ARFlow session
# Data must match with what's in `generate_payload.py`
docker run --name ghz --rm --network=host -v "${PWD}/../../protos:/protos:ro" \
  ghcr.io/bojand/ghz:"${GRPC_GHZ_TAG}" \
  --proto=/protos/cakelab/arflow_grpc/v1/arflow_service.proto \
  --import-paths=/protos \
  --call=cakelab.arflow_grpc.v1.ARFlowService.CreateSession \
  --disable-template-functions \
  --disable-template-data \
  --insecure \
  --total=1 \
  --data "{\"device\": {\"model\": \"iPhone 12\", \"name\": \"iPhone 12\", \"type\": \"TYPE_HANDHELD\", \"uid\": \"f3131490-dddd-419a-8504-fa8bb55282b2\"}}" \
  127.0.0.1:50051 >/dev/null

# Setup the chosen scenario
session_id=$(docker logs ${benchmark} | grep -m 1 "value:" | cut -d'"' -f2)

if ! sh setup_scenario.sh "$GRPC_REQUEST_SCENARIO" true "${session_id}" "$GRPC_CLIENT_FRAMES_PER_REQUEST"; then
  echo "Scenario setup fiascoed."
  exit 1
fi

echo "Sending frames to session: ${session_id}"

# Warm up the service
if [[ "${GRPC_BENCHMARK_WARMUP}" != "0s" ]]; then
  echo -n "Warming up the service for ${GRPC_BENCHMARK_WARMUP}... "
  docker run --name ghz --rm --network=host -v "${PWD}/../../protos:/protos:ro" \
    -v "${PWD}/payload:/payload:ro" \
    --cpus $GRPC_CLIENT_CPUS \
    ghcr.io/bojand/ghz:"${GRPC_GHZ_TAG}" \
    --proto=/protos/cakelab/arflow_grpc/v1/arflow_service.proto \
    --import-paths=/protos \
    --call=cakelab.arflow_grpc.v1.ARFlowService.SaveARFrames \
    --disable-template-functions \
    --disable-template-data \
    --insecure \
    --concurrency="${GRPC_CLIENT_CONCURRENCY}" \
    --connections="${GRPC_CLIENT_CONNECTIONS}" \
    --rps="${GRPC_CLIENT_RPS}" \
    --duration "${GRPC_BENCHMARK_WARMUP}" \
    --data-file /payload/payload \
    127.0.0.1:50051 >/dev/null

  echo "done."
else
  echo "gRPC Server Warmup skipped."
fi

# Actual benchmark
echo "Benchmarking now... "

# Start collecting stats
./collect_stats.sh "${benchmark}" "${RESULTS_DIR}" &

# Start the gRPC Client
docker run --name ghz --rm --network=host -v "${PWD}/../../protos:/protos:ro" \
  -v "${PWD}/payload:/payload:ro" \
  --cpus $GRPC_CLIENT_CPUS \
  ghcr.io/bojand/ghz:"${GRPC_GHZ_TAG}" \
  --proto=/protos/cakelab/arflow_grpc/v1/arflow_service.proto \
  --import-paths=/protos \
  --call=cakelab.arflow_grpc.v1.ARFlowService.SaveARFrames \
  --disable-template-functions \
  --disable-template-data \
  --insecure \
  --concurrency="${GRPC_CLIENT_CONCURRENCY}" \
  --connections="${GRPC_CLIENT_CONNECTIONS}" \
  --rps="${GRPC_CLIENT_RPS}" \
  --duration "${GRPC_BENCHMARK_DURATION}" \
  --data-file /payload/payload \
  127.0.0.1:50051 >"${RESULTS_DIR}/${benchmark}".report

# Show quick summary (reqs/sec)
cat <<EOF
		done.
		Results:
		$(cat "${RESULTS_DIR}/${benchmark}".report | grep "Requests/sec" | sed -E 's/^ +/    /')
EOF

kill -INT %1 2>/dev/null
docker container stop "${benchmark}" >/dev/null

if sh analyze.sh $RESULTS_DIR; then
  cat ${RESULTS_DIR}/bench.params
  echo "All done."
else
  echo "Analysis fiascoed."
  ls -lha $RESULTS_DIR
  for f in $RESULTS_DIR/*; do
    echo
    echo
    echo "$f"
    cat "$f"
  done
  exit 1
fi
