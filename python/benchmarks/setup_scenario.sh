#!/usr/bin/env sh

SCENARIOS_DIR="scenarios"
SCENARIO=$1
COPY_PAYLOAD=$2
SESSION_ID=$3

if ${COPY_PAYLOAD}; then
  rm -rf payload
  mkdir -p payload
  poetry run ./generate_payload.py --scenario="${SCENARIOS_DIR}/${SCENARIO}" --session-id="${SESSION_ID}" -o scenarios/"${SCENARIO}"/payload
  cp scenarios/"${SCENARIO}"/payload payload/payload
fi
