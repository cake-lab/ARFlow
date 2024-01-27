# ARFlow

[ARFlow: A Framework for Simplifying AR Experimentation Workflow](https://doi.org/10.1145/3638550.3643617)

[Paper](https://doi.org/10.1145/3638550.3643617) | [BibTeX](#bibtex) | [Project Page](https://cake.wpi.edu/ARFlow/) | [Video](https://youtu.be/mml8YrCgfTk)

Watch our demo video:

[![Demo video](https://img.youtube.com/vi/mml8YrCgfTk/maxresdefault.jpg)](https://youtu.be/mml8YrCgfTk)


The recent advancement in computer vision and XR hardware has ignited the community's interest in AR systems research. Similar to traditional systems research, the evaluation of AR systems involves capturing real-world data with AR hardware and iteratively evaluating the targeted system designs. However, it is challenging to conduct scalable and reproducible AR experimentation due to two key reasons. First, there is a lack of integrated framework support in real-world data capturing, which makes it a time-consuming process. Second, AR data often exhibits characteristics, including temporal and spatial variations, and is in a multi-modal format, which makes it difficult to conduct controlled evaluations. In this demo paper, we present the design and implementation of a framework called ARFlow that simplifies the evaluation workflow of AR systems researchers.

## Get Started

To use ARFlow, you should deploy the ARFlow server on your developer desktop and the ARFlow client to an AR device.

### ARFlow Serer

ARFlow server can be simply installed via `pip`:

```bash
pip install arflow
```

Next, you may integrate ARFlow with your own research prototype via the Python API:

```python
"""A simple example of extending the ARFlow server."""
import arflow


class CustomService(arflow.ARFlowService):
    def on_frame_received(self, frame: arflow.DataFrameRequest):
        """Called when a frame is received."""
        print("Frame received!")


def main():
    arflow.create_server(CustomService, port=8500, path_to_save="./")


if __name__ == "__main__":
    main()
```

### ARFlow Client

The ARFlow client is responsible for on-device AR data collection and high-performance AR data streaming. You may directly deploy our client code to your AR device.

Please locate the client code in the `unity` folder, and compile the code for your target deployment platform. Currently, we support the following platforms:

- iOS (iPhone, iPad)
- Android (Android Phone)

<!-- TODO: client side address input and screenshot. -->


## BibTex

```bibtex
@article{zhao2024hotmobile,
  author    = {Zhao, Yiqin and Guo, Tian},
  title     = {ARFlow: A Framework for Simplifying AR Experimentation Workflow},
  journal   = {HotMobile},
  year      = {2024},
}
```
