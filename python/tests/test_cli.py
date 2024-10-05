"""Command line interface tests."""

# ruff:noqa: D103
# pyright: reportPrivateUsage=false

import argparse
import logging
import shlex
import tempfile
from pathlib import Path
from typing import Literal
from unittest.mock import MagicMock, patch

import pytest

from arflow._cli import (
    _validate_dir_path,
    _validate_file_path,
    parse_args,
    replay,
    serve,
)


@pytest.fixture
def temp_dir():
    with tempfile.TemporaryDirectory() as td:
        yield td


@pytest.fixture
def temp_file():
    with tempfile.NamedTemporaryFile() as tf:
        yield tf.name


def test_validate_dir_path_valid(temp_dir: str):
    assert _validate_dir_path(temp_dir) == temp_dir


def test_validate_dir_path_invalid():
    with pytest.raises(argparse.ArgumentTypeError):
        _validate_dir_path("/path/to/nonexistent/directory")


def test_validate_dir_path_none():
    assert _validate_dir_path(None) is None


def test_validate_file_path_valid(temp_file: str):
    assert _validate_file_path(temp_file) == temp_file


def test_validate_file_path_invalid():
    with pytest.raises(argparse.ArgumentTypeError):
        _validate_file_path("/path/to/nonexistent/file.txt")


def test_serve():
    with patch("arflow._cli.run_server") as mock_run_server, patch(
        "arflow._cli.ARFlowServicer"
    ) as mock_servicer:
        args = MagicMock()
        args.port = 1234
        args.save_path = "/tmp/save_path"

        serve(args)

        mock_run_server.assert_called_once_with(
            mock_servicer, port=1234, path_to_save=Path("/tmp/save_path")
        )


def test_replay():
    with patch("arflow._cli.ARFlowPlayer") as mock_player_class, patch(
        "arflow._cli.ARFlowServicer"
    ) as mock_servicer:
        mock_player_instance = MagicMock()
        mock_player_class.return_value = mock_player_instance

        args = MagicMock()
        args.file_path = "/path/to/data.file"

        replay(args)

        mock_player_class.assert_called_once_with(
            mock_servicer, Path("/path/to/data.file")
        )
        mock_player_instance.run.assert_called_once()


@pytest.mark.parametrize(
    "command, subcommand, debug, verbose, port, save_path, file_path",
    [
        ("", None, False, False, None, None, None),
        ("-d", None, True, False, None, None, None),
        ("-v", None, False, True, None, None, None),
        ("-d -v", None, True, True, None, None, None),
        ("serve", "serve", False, False, 8500, None, None),
        ("-d serve", "serve", True, False, 8500, None, None),
        ("-d serve -p 1234", "serve", True, False, 1234, None, None),
        (
            "-d serve -s /tmp/save_path",
            "serve",
            True,
            False,
            8500,
            "/tmp/save_path",
            None,
        ),
        (
            "-d serve -p 1234 -s /tmp/save_path",
            "serve",
            True,
            False,
            1234,
            "/tmp/save_path",
            None,
        ),
        (
            "replay /path/to/data.file",
            "replay",
            False,
            False,
            None,
            None,
            "/path/to/data.file",
        ),
        (
            "-d replay /path/to/data.file",
            "replay",
            True,
            False,
            None,
            None,
            "/path/to/data.file",
        ),
    ],
)
def test_parse_args(
    command: str,
    subcommand: Literal["serve", "replay"] | None,
    debug: bool,
    verbose: bool,
    port: int | None,
    save_path: str | None,
    file_path: str | None,
):
    with patch(
        "arflow._cli._validate_file_path", return_value="/path/to/data.file"
    ), patch("arflow._cli._validate_dir_path", return_value="/tmp/save_path"):
        if debug and verbose:
            with pytest.raises(SystemExit):
                parse_args(shlex.split(command))
            return

        _, args = parse_args(shlex.split(command))

        if not debug and not verbose:
            assert args.loglevel == logging.WARNING
        elif debug:
            assert args.loglevel == logging.DEBUG
        elif verbose:
            assert args.loglevel == logging.INFO

        if subcommand == "serve":
            assert args.func == serve
            assert args.port == port
            assert args.save_path == save_path
        elif subcommand == "replay":
            assert args.func == replay
            assert args.file_path == file_path
        else:
            assert not hasattr(args, "port")
            assert not hasattr(args, "save_path")
            assert not hasattr(args, "file_path")
            assert not hasattr(args, "func")
