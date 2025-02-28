#!/usr/bin/env sh

SCENARIO=$1
COPY_PAYLOAD=$2
SESSION_ID=$3
FRAMES_PER_REQUEST=$4

if ${COPY_PAYLOAD}; then
  rm -rf payload
  mkdir -p payload
  poetry run ./generate_payload.py --scenario "${SCENARIO}" --session-id="${SESSION_ID}" --frames-per-request="${FRAMES_PER_REQUEST}"
  cp scenarios/"${SCENARIO}"/payload payload/payload
fi
