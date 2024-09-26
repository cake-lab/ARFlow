"""Error handling utilities for ARFlow."""

from typing import Final, TypedDict

import grpc


class GRPCError(TypedDict):
    """Represents a gRPC error.

    Attributes:
        details (str): The details of the error.
        status_code (grpc.StatusCode): The status code of the error.
    """

    details: str
    status_code: grpc.StatusCode


GRPC_CLIENT_CONFIG_NOT_FOUND: Final[GRPCError] = {
    "details": "Client configuration not found",
    "status_code": grpc.StatusCode.NOT_FOUND,
}


def set_grpc_error(context: grpc.ServicerContext, error: GRPCError) -> None:
    """Set the error details and status code in the context.

    This function should be used before returning from a gRPC service method.
    """
    context.set_details(error["details"])
    context.set_code(error["status_code"])


class CannotReachException(Exception):
    """Exception raised when an unreachable code path is reached."""

    pass
