import logging
from typing import Any, NoReturn

import grpc
from grpc_interceptor import ExceptionToStatusInterceptor

logger = logging.getLogger(__name__)


class ErrorInterceptor(ExceptionToStatusInterceptor):
    def handle_exception(
        self,
        ex: Exception,
        request_or_iterator: Any,
        context: grpc.ServicerContext,
        method_name: str,
    ) -> NoReturn:
        self.log_error(ex)
        super().handle_exception(ex, request_or_iterator, context, method_name)

    def log_error(self, e: Exception) -> None:
        logger.exception(e)
