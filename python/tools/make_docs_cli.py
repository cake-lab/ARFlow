"""A simple script to generate docs from Python doc strings."""

from pathlib import Path

import pdoc


def make_docs():
    pdoc.pdoc("arflow", output_directory=Path(__file__).parent.parent / "docs")


if __name__ == "__main__":
    make_docs()
