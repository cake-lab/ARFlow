#!/usr/bin/env python3
"""A demo integration with Xihe.

Reference:
- Yiqin Zhao and Tian Guo. 2021. Xihe: A 3D Vision-Based Lighting
Estimation Framework for Mobile Augmented Reality. In Proceedings of
the 19th Annual International Conference on Mobile Systems, Applications,
and Services (MobiSys'21). 28-40.
"""

from threading import Thread
from typing import Any, Dict

import numpy as np
import pandas as pd
import torch
import utils3d as u3d
import xihenet_utils

# This import is necessary to avoid a segmentation fault when loading
# DO NOT REMOVE (please)
# To install this (workaround currently), enter "poetry shell" and run "pip install torch-cluster -f https://data.pyg.org/whl/torch-2.4.0+${CUDA}.html"
import torch_cluster

import arflow


class XiheService(arflow.ARFlowService):
    def __init__(self, *args, **kwargs) -> None:
        super().__init__(*args, **kwargs)

        self.xihenet = torch.jit.load("xihenet.pt")
        self.xihenet.eval()

        self.anchors_inf = xihenet_utils.fibonacci_sphere(samples=1280)
        self.anchors_xyz = torch.from_numpy(self.anchors_inf.T[np.newaxis, :, :])

        self.calculator = xihenet_utils.JointEntropyCalculator()

    def on_register(self):
        self.num_frame = 0

    def on_frame_received(self, frame_data: Dict[str, Any]):
        # Run XiheNet inference.
        pcd = frame_data["point_cloud_pcd"]
        clr = frame_data["point_cloud_clr"]
        if self.num_frame % 100 == 0:
            thread = Thread(
                target=self.run_xihenet_inference, args=(pcd.copy(), clr.copy())
            )
            thread.start()

        self.num_frame = self.num_frame + 1

    def run_xihenet_inference(self, xyz: np.ndarray, rgb: np.ndarray):
        # Log input entropy.
        entropy = self.calculator.forward(torch.from_numpy(xyz).float())
        self.recorder.log("Xihe/input_entropy", self.recorder.TimeSeriesScalar(entropy))

        # Inference preprocessing code copied from previous
        # lighting estimation visualization code.
        dst = np.linalg.norm(xyz, axis=-1, keepdims=True)
        m = (dst > 0)[:, 0]
        pc_xyz = xyz[m, :] / dst[m, :]
        t = self.anchors_inf @ pc_xyz.T

        # print(t.shape, dst.shape)
        df = pd.DataFrame.from_dict(
            {"anchor_id": np.argmax(t, axis=0), "distance": dst.reshape(-1)}
        )
        df = df.reset_index().groupby(["anchor_id"]).min()

        # base color (0.5, 0.5, 0.5)
        anchor_clr = np.zeros_like(self.anchors_inf) + 0.5
        anchor_clr[df.index] = rgb[df["index"]] / 255.0

        anchor_clr = torch.from_numpy(anchor_clr.T[np.newaxis, :, :])
        p = self.xihenet(self.anchors_xyz, anchor_clr).detach()
        p = (p - xihenet_utils.n_min) / xihenet_utils.n_scale
        p = p.numpy()
        coefficients = p.reshape((-1)).reshape((3, -1))
        t = coefficients
        coefficients = np.moveaxis(coefficients, 0, -1)
        coefficients = coefficients.reshape((-1))
        # coefficients = coefficients.tolist()

        self.result_buffer.append(",".join([str(v) for v in t.flatten().tolist()]))

        shc = u3d.math.spherical_harmonics.SphericalHarmonics.from_array(
            coefficients, channel_order="last"
        )
        shc_image = shc.reconstruct_to_canvas()
        shc_image = shc_image.data.astype("float32")
        shc_image = np.clip(shc_image, 0, 1) * 255
        shc_image = shc_image.astype("uint8")
        self.recorder.log("Xihe/SH_coefficients", self.recorder.Image(shc_image))


def main():
    arflow.create_server(XiheService, port=8500, path_to_save=None)


if __name__ == "__main__":
    main()
