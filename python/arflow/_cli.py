"""ARFlow command line interface."""

import argparse
import logging
from pathlib import Path
from typing import Any, Sequence

from arflow._core import ARFlowServicer, run_server


def _validate_dir_path(path_as_str: str | None) -> str | None:
    """Check if the path is a valid directory."""
    if path_as_str is None:
        return None
    path = Path(path_as_str)
    if not path.is_dir():
        raise argparse.ArgumentTypeError(f"{path_as_str} is not a valid path.")
    return path_as_str


def _validate_file_path(path_as_str: str) -> str:
    """Check if the path is a valid file."""
    path = Path(path_as_str)
    if not path.is_file():
        raise argparse.ArgumentTypeError(f"{path_as_str} is not a valid file.")
    return path_as_str


def serve(args: Any):
    """Run the ARFlow server."""
    run_server(
        ARFlowServicer,
        port=args.port,
        save_dir=Path(args.save_dir) if args.save_dir else None,
        spawn_viewer=not args.headless,
    )


def replay(args: Any):
    """Replay an ARFlow data file."""
    # player = ARFlowPlayer(ARFlowServicer, Path(args.file_path))
    # player.run()


def parse_args(
    argv: Sequence[str] | None = None,
) -> tuple[argparse.ArgumentParser, argparse.Namespace]:
    parser = argparse.ArgumentParser(description="ARFlow CLI")
    subparsers = parser.add_subparsers()

    group = parser.add_mutually_exclusive_group()
    group.add_argument(
        "-d",
        "--debug",
        help="Print debug information.",
        action="store_const",
        dest="loglevel",
        const=logging.DEBUG,
        default=logging.WARNING,
    )
    group.add_argument(
        "-v",
        "--verbose",
        help="Print verbose information.",
        action="store_const",
        dest="loglevel",
        const=logging.INFO,
    )

    # Serve subcommand
    serve_parser = subparsers.add_parser("serve", help="Run a simple ARFlow server")
    serve_parser.add_argument(
        "-p",
        "--port",
        type=int,
        default=8500,
        help="Port to run the server on.",
    )
    # TODO: https://rerun.io/docs/reference/sdk/operating-modes. mutually exclusive operating modes: view or save.
    # serve_subparsers = serve_parser.add_subparsers()

    # serve_group = serve_parser.add_mutually_exclusive_group()
    # serve_group.add_argument(
    #     "--view",
    #     help="View the data in the Rerun Viewer.",
    #     action="store_true",
    # )
    # serve_group.add_argument(
    #     "--save",
    #     help="Save the data to disk.",
    #     action="store_true",
    # )

    serve_parser.add_argument(
        "-s",
        "--save-dir",
        type=_validate_dir_path,
        default=None,
        help="The path to save the data to. If None, defaults to a temporary directory located at `gettempdir()` in the `arflow` subdirectory.",
    )
    serve_parser.add_argument(
        "--headless",
        help="Do not spawn the Rerun Viewer in another process.",
        action="store_true",
    )
    serve_parser.set_defaults(func=serve)

    # Replay subcommand
    replay_parser = subparsers.add_parser("replay", help="Replay an ARFlow data file")
    replay_parser.add_argument(
        "file-path",
        type=_validate_file_path,
        help="Path to the ARFlow data file.",
    )
    replay_parser.set_defaults(func=replay)

    parsed_args = parser.parse_args(argv)

    logging.basicConfig(
        level=parsed_args.loglevel,
        format="%(asctime)s - %(name)s - %(levelname)s - %(message)s (%(filename)s:%(lineno)d)",
    )

    return parser, parsed_args


def main(argv: Sequence[str] | None = None):  # pragma: no cover
    parser, args = parse_args(argv)
    if hasattr(args, "func"):
        args.func(args)
    else:
        parser.print_help()


if __name__ == "__main__":  # pragma: no cover
    main()
