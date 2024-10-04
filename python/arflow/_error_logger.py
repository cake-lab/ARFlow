import logging

from grpc_interceptor import ServerInterceptor

logger = logging.getLogger(__name__)


class ErrorLogger(ServerInterceptor):
    def intercept(self, method, request_or_iterator, context, method_name):  # pyright: ignore [reportUnknownParameterType, reportMissingParameterType]
        try:
            return method(request_or_iterator, context)  # pyright: ignore [reportUnknownVariableType]
        except Exception as e:
            self.log_error(e)
            raise

    def log_error(self, e: Exception) -> None:
        """Called whenever an unhandled exception occurs in the service."""
        logger.exception(e)  # pragma: no cover
