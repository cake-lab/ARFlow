import collections


def is_sequence_instance(seq, type):
    if isinstance(seq, collections.Sequence):
        for v in seq:
            if not isinstance(v, type):
                return False

        return True

    return False
