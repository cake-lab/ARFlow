"""ARFlow command line interface."""

import argparse
import logging
import os
from pathlib import Path
from tempfile import gettempdir
from typing import Any, Sequence

from arflow._core import ARFlowServicer, run_server

logger = logging.getLogger(__name__)


def _prompt_until_valid_dir(path_as_str: str) -> str:
    """Check if the path is a valid directory. Prompt to help users create the directory if it doesn't exist."""
    if os.path.isdir(path_as_str):
        logger.debug(f"Directory '{path_as_str}' exists.")
        return path_as_str

    while True:
        response = (
            input(
                f"The directory '{path_as_str}' does not exist. Would you like to create it? (y/n): "
            )
            .strip()
            .lower()
        )

        if response == "y":
            os.makedirs(path_as_str)
            logger.info(f"Directory '{path_as_str}' created.")
            return path_as_str
        elif response == "n":
            path_as_str = input("Please enter a new directory path: ").strip()
            if os.path.isdir(path_as_str):
                return path_as_str
        else:  # pragma: no cover
            logger.warning("Please enter 'y' or 'n'.")


def view(args: Any):
    """Run the ARFlow server and Rerun Viewer to view live data from the clients."""
    run_server(
        ARFlowServicer,
        spawn_viewer=True,
        save_dir=None,
        application_id=args.application_id,
        port=args.port,
    )


def save(args: Any):
    """Run the ARFlow server and save the data to disk."""
    run_server(
        ARFlowServicer,
        spawn_viewer=False,
        save_dir=Path(args.save_dir),
        application_id=args.application_id,
        port=args.port,
    )


def rerun(args: list[str]):
    """Wrapper around the [Rerun CLI](https://rerun.io/docs/reference/cli)."""
    os.execvp("rerun", ["rerun"] + args)


def parse_args(
    argv: Sequence[str] | None = None,
) -> tuple[argparse.ArgumentParser, argparse.Namespace, list[str]]:
    parser = argparse.ArgumentParser(description="ARFlow CLI")
    subparsers = parser.add_subparsers()

    log_group = parser.add_mutually_exclusive_group()
    log_group.add_argument(
        "-d",
        "--debug",
        help="Print debug information.",
        action="store_const",
        dest="loglevel",
        const=logging.DEBUG,
        default=logging.INFO,
    )
    log_group.add_argument(
        "-q",
        "--quiet",
        help="Print only warnings and errors.",
        action="store_const",
        dest="loglevel",
        const=logging.WARNING,
    )

    # View subcommand
    view_parser = subparsers.add_parser(
        "view",
        help="Run the ARFlow server and Rerun Viewer to view live data from the clients.",
    )
    view_parser.add_argument(
        "-p",
        "--port",
        type=int,
        default=int(os.getenv("PORT", 8500)),
        help=f"Port to run the server on (default: %(default)s).",
    )
    view_parser.add_argument(
        "-a",
        "--application-id",
        type=str,
        default="arflow",
        help=f"Application ID to use for the Rerun recording (default: %(default)s).",
    )
    view_parser.set_defaults(func=view)

    # Save subcommand
    save_parser = subparsers.add_parser(
        "save", help="Run the ARFlow server and save the data to disk."
    )
    save_parser.add_argument(
        "-s",
        "--save-dir",
        type=_prompt_until_valid_dir,
        default=str(Path(gettempdir()) / "arflow"),
        help="The path to save the data to (default: %(default)s).",
    )
    save_parser.add_argument(
        "-p",
        "--port",
        type=int,
        default=8500,
        help=f"Port to run the server on (default: %(default)s).",
    )
    save_parser.add_argument(
        "-a",
        "--application-id",
        type=str,
        default="arflow",
        help=f"Application ID to use for the Rerun recording (default: %(default)s).",
    )
    save_parser.set_defaults(func=save)

    # Rerun subcommand
    rerun_parser = subparsers.add_parser(
        "rerun",
        help="Wrapper around the [Rerun CLI](https://rerun.io/docs/reference/cli). Everything after `arflow rerun` will be passed as is to `rerun` CLI. Helpful for visualizing and manipulating ARFlow session data files (`.rrd`).",
        add_help=False,  # Disable default help to pass through `-h`
    )
    rerun_parser.set_defaults(func=rerun)

    parsed_args, rerun_args = parser.parse_known_args(argv)

    logging.basicConfig(
        level=parsed_args.loglevel,
        format="%(asctime)s - %(levelname)s - %(name)s - %(message)s (%(filename)s:%(lineno)d)"
        if parsed_args.loglevel == logging.DEBUG
        else "%(asctime)s - %(levelname)s - arflow - %(message)s",
    )

    return parser, parsed_args, rerun_args


def main(argv: Sequence[str] | None = None):  # pragma: no cover
    parser, args, rerun_args = parse_args(argv)
    if hasattr(args, "func"):
        if args.func == rerun:
            args.func(rerun_args)
        else:
            args.func(args)
    else:
        parser.print_help()


if __name__ == "__main__":  # pragma: no cover
    main()
