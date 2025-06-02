"""Command line interface tests."""

# ruff:noqa: D103
# pyright: reportPrivateUsage=false

import logging
import os
import shlex
from pathlib import Path
from typing import Literal
from unittest.mock import MagicMock, patch

import pytest

from arflow._cli import (
    _prompt_until_valid_dir,
    parse_args,
    rerun,
    save,
    view,
)


# https://docs.pytest.org/en/stable/how-to/tmp_path.html#the-tmp-path-fixture
def test_existing_directory(tmp_path: Path):
    assert _prompt_until_valid_dir(str(tmp_path)) == str(tmp_path)


def test_create_new_directory(tmp_path: Path):
    new_dir = str(tmp_path / "new_dir")

    with patch("builtins.input") as mock_input:
        mock_input.side_effect = ["y"]
        result = _prompt_until_valid_dir(new_dir)

    assert result == new_dir
    assert os.path.isdir(new_dir)


def test_provide_alternate_directory(tmp_path: Path):
    non_existent = str(tmp_path / "nonexistent")
    alternate = str(tmp_path / "alternate")
    os.makedirs(alternate, exist_ok=True)

    with patch("builtins.input") as mock_input:
        mock_input.side_effect = [
            "n",
            alternate,
        ]  # Decline creation, provide existing dir
        result = _prompt_until_valid_dir(non_existent)

    assert result == alternate


def test_view():
    with (
        patch("arflow._cli.run_server") as mock_run_server,
        patch("arflow._cli.ARFlowServicer") as mock_servicer,
    ):
        args = MagicMock()
        args.port = 1234
        args.application_id = "test-id"

        view(args)

        mock_run_server.assert_called_once_with(
            mock_servicer,
            spawn_viewer=True,
            save_dir=None,
            port=1234,
            application_id="test-id",
        )


def test_save():
    with (
        patch("arflow._cli.run_server") as mock_run_server,
        patch("arflow._cli.ARFlowServicer") as mock_servicer,
    ):
        args = MagicMock()
        args.port = 1234
        args.save_dir = "/tmp/save_path"
        args.application_id = "test-id"

        save(args)

        mock_run_server.assert_called_once_with(
            mock_servicer,
            spawn_viewer=False,
            save_dir=Path("/tmp/save_path"),
            port=1234,
            application_id="test-id",
        )


def test_rerun():
    with patch("os.execvp") as mock_execvp:
        rerun(["some-arbitary-rerun-command", "-p", "1234"])
        mock_execvp.assert_called_once_with(
            "rerun", ["rerun", "some-arbitary-rerun-command", "-p", "1234"]
        )


@pytest.mark.parametrize(
    "command, subcommand, debug, quiet, port, save_dir, application_id",
    [
        (
            "",
            None,
            False,
            False,
            None,
            None,
            "arflow",
        ),
        (
            "-d",
            None,
            True,
            False,
            None,
            None,
            "arflow",
        ),
        (
            "-q",
            None,
            False,
            True,
            None,
            None,
            "arflow",
        ),
        (
            "view",
            "view",
            False,
            False,
            8500,
            None,
            "arflow",
        ),
        (
            "-d save",
            "save",
            True,
            False,
            8500,
            None,
            "arflow",
        ),
        (
            "-d save -p 1234",
            "save",
            True,
            False,
            1234,
            None,
            "arflow",
        ),
        (
            "-d save -s /tmp/save_path",
            "save",
            True,
            False,
            8500,
            "/tmp/save_path",
            "arflow",
        ),
        (
            "-d save -p 1234 -s /tmp/save_path -a test-id",
            "save",
            True,
            False,
            1234,
            "/tmp/save_path",
            "test-id",
        ),
        (
            "rerun /path/to/data.file",
            "rerun",
            False,
            False,
            None,
            None,
            None,
        ),
        (
            "-d rerun /path/to/data.file",
            "rerun",
            True,
            False,
            None,
            None,
            None,
        ),
    ],
)
def test_parse_args(
    command: str,
    subcommand: Literal["view", "save", "rerun"] | None,
    debug: bool,
    quiet: bool,
    port: int | None,
    save_dir: str | None,
    application_id: str | None,
    tmp_path: Path,
):
    if save_dir is None:
        save_dir = str(tmp_path)
    with patch("arflow._cli._prompt_until_valid_dir", return_value=save_dir):
        _, args, _ = parse_args(shlex.split(command))

    if not debug and not quiet:
        assert args.loglevel == logging.INFO
    elif debug:
        assert args.loglevel == logging.DEBUG
    elif quiet:
        assert args.loglevel == logging.WARNING

    if subcommand == "view":
        assert args.func == view
        assert args.port == port
        assert args.application_id == application_id
    elif subcommand == "save":
        assert args.func == save
        assert args.port == port
        assert args.save_dir == save_dir
        assert args.application_id == application_id
    elif subcommand == "rerun":
        assert args.func == rerun
    else:
        assert not hasattr(args, "func")
        assert not hasattr(args, "port")
        assert not hasattr(args, "save_dir")
        assert not hasattr(args, "application_id")
