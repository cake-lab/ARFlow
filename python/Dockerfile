# TODO: remove 
# FROm python:3.12-slim-bookworm
#
# WORKDIR /app
# COPY python_grpc_bench /app
# COPY proto /app/proto
#
# RUN python -m pip install grpcio grpcio-tools
# RUN python -m grpc_tools.protoc -I/app/proto/helloworld --python_out=. --grpc_python_out=. helloworld.proto
#
# ENTRYPOINT [ "python", "/app/server.py" ]

# FROM python:3.12-alpine AS base
FROM python:3.12-slim-bookworm AS base

ENV VIRTUAL_ENV=/app/.venv \
  PATH="/app/.venv/bin:$PATH"

# RUN apk update && \
#   apk add libpq


FROM base AS builder

ENV POETRY_NO_INTERACTION=1 \
  POETRY_VIRTUALENVS_IN_PROJECT=1 \
  POETRY_VIRTUALENVS_CREATE=1 \
  POETRY_CACHE_DIR=/tmp/poetry_cache

# RUN apk update && \
#   apk add musl-dev build-base gcc gfortran openblas-dev

WORKDIR /app

RUN pip install --no-cache-dir poetry==1.8.4

COPY pyproject.toml poetry.lock ./
RUN poetry install --without dev --no-root && rm -rf $POETRY_CACHE_DIR;


FROM base AS runtime

COPY --from=builder ${VIRTUAL_ENV} ${VIRTUAL_ENV}


WORKDIR /app

COPY arflow ./arflow
COPY cakelab ./cakelab

EXPOSE 8500

ENV PORT=8500

# TODO: Vary entrypoint to include save mode
ENTRYPOINT ["python", "-m", "arflow._cli", "view"]
