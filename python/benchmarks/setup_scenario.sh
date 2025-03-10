#!/usr/bin/env sh

SCENARIO=$1
COPY_PAYLOAD=$2
SESSION_ID=$3

if ${COPY_PAYLOAD}; then
  rm -rf payload
  mkdir -p payload
  cp scenarios/"${SCENARIO}"/payload payload/payload
  # replace SESSION_ID_PLACEHOLDER in payload
  sed -i "s/SESSION_ID_PLACEHOLDER/${SESSION_ID}/g" payload/payload
fi
