"""A demo integration with Xihe.

Reference:
- Yiqin Zhao and Tian Guo. 2021. Xihe: A 3D Vision-Based Lighting
Estimation Framework for Mobile Augmented Reality. In Proceedings of
the 19th Annual International Conference on Mobile Systems, Applications,
and Services (MobiSys'21). 28-40.
"""

from threading import Thread

import arflow
import numpy as np
import pandas as pd
import torch
import torch_cluster  # noqa
from examples.demo_xihe import utils3d as u3d
from examples.demo_xihe import xihenet_utils


class XiheService(arflow.ARFlowService):
    def __init__(self, *args, **kwargs) -> None:
        super().__init__(*args, **kwargs)

        self.xihenet = torch.jit.load("examples/demo_xihe/xihenet.pt")
        self.xihenet.eval()

        self.anchors_inf = xihenet_utils.fibonacci_sphere(samples=1280)
        self.anchors_xyz = torch.from_numpy(self.anchors_inf.T[np.newaxis, :, :])

        self.calculator = xihenet_utils.JointEntropyCalculator()

    def on_frame_received(self, decoded_data: dict):
        """Called when a frame is received."""
        # print("Frame received!")

        # Run XiheNet inference.
        pcd = decoded_data["point_cloud_pcd"]
        clr = decoded_data["point_cloud_clr"]
        if self.num_frame % 100 == 0:
            thread = Thread(
                target=self.run_xihenet_inference, args=(pcd.copy(), clr.copy())
            )
            thread.start()

        # increase frame counter.
        self.num_frame = self.num_frame + 1

    def run_xihenet_inference(self, xyz: np.ndarray, rgb: np.ndarray):
        # Log input entropy.
        entropy = self.calculator.forward(torch.from_numpy(xyz).float())
        self.rr.log("Xihe/input_entropy", self.rr.TimeSeriesScalar(entropy))

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

        shc = u3d.SphericalHarmonics.from_array(coefficients, channel_order="last")
        shc_image = shc.reconstruct_to_canvas()
        shc_image = shc_image.data.astype("float32")
        shc_image = np.clip(shc_image, 0, 1) * 255
        shc_image = shc_image.astype("uint8")
        self.rr.log("Xihe/SH_coefficients", self.rr.Image(shc_image))


def main():
    arflow.create_server(XiheService, port=8500, path_to_save=None)


if __name__ == "__main__":
    main()
