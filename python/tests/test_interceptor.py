"""Server-side interceptors tests."""

# ruff:noqa: D102,D103,D107

from typing import Any

import grpc
import pytest
from grpc_interceptor.testing import (
    DummyRequest,
    dummy_client,  # pyright: ignore [reportUnknownVariableType]
    raises,  # pyright: ignore [reportUnknownVariableType]
)

from arflow._error_interceptor import ErrorInterceptor


class MockErrorLogger(ErrorInterceptor):
    """Mock error logger that stores the last exception.

    You don’t actually want the logging side effect to happen. You just want to make sure it’s called.
    """

    def __init__(self):
        self.logged_exception = None

    def log_error(self, e: Exception) -> None:
        self.logged_exception = e


def test_log_error():
    """Test that the error logger catches exceptions.

    Use the `dummy_client()` context manager to create a client that’s connected to a real gRPC
    microservice. You send `DummyRequest` to the microservice, and it replies with `DummyResponse`.
    By default, the input of `DummyRequest` is echoed to the output of `DummyResponse`. However, you can
    pass `dummy_client()` a dictionary of special cases, and if input matches one of them, then it will
    call a function you provide and return the result.
    """
    mock = MockErrorLogger()
    ex = Exception()
    special_cases: dict[str, Any] = {"error": raises(ex)}

    with dummy_client(special_cases=special_cases, interceptors=[mock]) as client:
        # Test no exception
        assert client.Execute(DummyRequest(input="foo")).output == "foo"  # pyright: ignore [reportUnknownMemberType]
        assert mock.logged_exception is None

        # Test exception
        with pytest.raises(grpc.RpcError):
            client.Execute(DummyRequest(input="error"))  # pyright: ignore [reportUnknownMemberType]
        assert mock.logged_exception is ex


def test_log_error_multiple_cases():
    mock = MockErrorLogger()
    ex1 = Exception("Error 1")
    ex2 = Exception("Error 2")
    special_cases: dict[str, Any] = {
        "error1": raises(ex1),
        "error2": raises(ex2),
    }

    with dummy_client(special_cases=special_cases, interceptors=[mock]) as client:
        with pytest.raises(grpc.RpcError):
            client.Execute(DummyRequest(input="error1"))  # pyright: ignore [reportUnknownMemberType]
        assert mock.logged_exception is ex1

        with pytest.raises(grpc.RpcError):
            client.Execute(DummyRequest(input="error2"))  # pyright: ignore [reportUnknownMemberType]
        assert mock.logged_exception is ex2
