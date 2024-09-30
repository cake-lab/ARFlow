from grpc_interceptor import ServerInterceptor


class ErrorLogger(ServerInterceptor):
    def intercept(self, method, request_or_iterator, context, method_name):  # type: ignore
        try:
            return method(request_or_iterator, context)  # type: ignore
        except Exception as e:
            self.log_error(e)
            raise

    def log_error(self, e: Exception) -> None:
        """Called whenever an unhandled exception occurs in the service."""
        print(f"Error: {e}")
