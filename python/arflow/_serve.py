"""Simple server for ARFlow service."""

import argparse
import logging
import os
from pathlib import Path

from arflow._core import ARFlowServicer, run_server


def _dir_path(path_as_str: str | None) -> str | None:
    """Check if the path is a valid directory."""
    if path_as_str is None:
        return None
    if not os.path.isdir(path_as_str):
        raise argparse.ArgumentTypeError(f"{path_as_str} is not a valid path.")
    return path_as_str


def serve():
    """Run a simple ARFlow server."""
    parser = argparse.ArgumentParser(description="Run a simple ARFlow server.")
    parser.add_argument(
        "-p",
        "--port",
        type=int,
        default=8500,
        help="Port to run the server on.",
    )
    parser.add_argument(
        "-s",
        "--save_path",
        type=_dir_path,
        default=None,
        help="Path to save the requests history.",
    )
    args = parser.parse_args()
    run_server(
        ARFlowServicer,
        port=args.port,
        path_to_save=Path(args.save_path) if args.save_path else None,
    )


if __name__ == "__main__":
    logging.basicConfig()  # TODO: Replace print with logging
    serve()
