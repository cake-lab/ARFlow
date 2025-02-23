#!/usr/bin/env bash

bash build.sh || exit 1

export GRPC_BENCHMARK_DURATION=60s
export GRPC_BENCHMARK_WARMUP=10s
export GRPC_SERVER_RAM=4096m
export GRPC_CLIENT_CPUS=5
export GRPC_CLIENT_CONCURRENCY=5
export GRPC_CLIENT_CONNECTIONS=5
export GRPC_REQUEST_SCENARIO=${GRPC_REQUEST_SCENARIO:-"mixed"}

if [ "$GRPC_REQUEST_SCENARIO" == "mixed" ]; then
  rpss=(40 20 10) # double to account for round-robinness of the data modalities sent
  frames_per_requests=(15 30 60)
else
  rpss=(20 10 5)
  frames_per_requests=(15 30 60)
fi

cpus=0
while [ $cpus -ne 2 ]; do
  cpus=$((cpus + 1))
  for i in "${!rpss[@]}"; do
    rps=${rpss[$i]}
    frames_per_request=${frames_per_requests[$i]}

    echo "Benchmarking $GRPC_REQUEST_SCENARIO scenario with $cpus CPU(s), RPS=$rps, and Frames Per Request=$frames_per_request"

    GRPC_SERVER_CPUS=$cpus \
      GRPC_CLIENT_RPS=$rps \
      GRPC_CLIENT_FRAMES_PER_REQUEST=$frames_per_request \
      bash bench.sh || exit 2
    sleep 10
  done
done

echo "Benchmarking finished"
