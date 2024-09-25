"""Example of a test file. Used to pass CI until we have proper tests."""

# ruff:noqa: D103 (missing docstring in public function)


def inc(x: int):
    return x + 1


def test_answer():
    assert inc(4) == 5
