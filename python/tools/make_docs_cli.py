"""A simple script to generate docs from Python doc strings."""

from pathlib import Path

import pdoc


def make_docs():
    """Generate documentation for the `arflow` package and `examples` package."""
    pdoc.pdoc(
        "arflow", "examples", output_directory=Path(__file__).parent.parent / "docs"
    )


if __name__ == "__main__":
    make_docs()
