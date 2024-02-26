import json

import numpy as np
import pandas as pd


def pretty_print_matrix(mat):
    res_rows = []

    for i in range(len(mat)):
        res_cols = []
        for j in range(len(mat[i])):
            res_cols.append(json.dumps(mat[i, j].tolist()))

        res_rows.append(res_cols)

    df = pd.DataFrame(res_rows)

    print(df)
