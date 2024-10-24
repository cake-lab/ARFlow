# ARFlow

ARFlow is a data-sharing layer that enables developer-friendly data streaming, storage, and visualization for augmented reality (AR) device data.
This project aims to provide a tool to democratize and accelerate AR research and development.

[Paper](https://doi.org/10.1145/3638550.3643617) | [BibTeX](#how-to-cite-arflow) | [Project Page](https://cake.wpi.edu/ARFlow/) | [Video](https://youtu.be/mml8YrCgfTk)

## Quick Start

### Device Preparation

First, you need an AR device.
We currently support iOS and Android phones and tablets. Meta Quests 3 support is being developed.
Make sure you have the developer mode enabled on your device.

### Server Setup

Next, start up your own ARFlow server instance:

```shell
arflow serve # This will start the server on port 8500
```

### Client Setup

Next, go to the [releases](https://github.com/cake-lab/ARFlow/releases) page and find the prebuilt items for Android and iOS.
For Android, directly install the prebuilt apk on your device. For iOS, compile the generated Xcode project to deploy the ARFlow client app to your iOS device. Note that you will need to configure the developer credentials in the Xcode project.

After launching the ARFlow client app, follow the onscreen instruction to input the server address and port (8500 for the previous example) information, then tap **connect** and **start**.

Watch our demo video:

[![Demo video](https://img.youtube.com/vi/mml8YrCgfTk/maxresdefault.jpg)](https://youtu.be/mml8YrCgfTk)

## Contribution

Please read the [CONTRIBUTING](./CONTRIBUTING.md) guideline first, and refer to the individual [server](./python/README.md) and [client](./unity/README.md) installation guides.

### Contributors

<!-- readme: contributors -start -->
<table>
	<tbody>
		<tr>
            <td align="center">
                <a href="https://github.com/legoeruro">
                    <img src="https://avatars.githubusercontent.com/u/68761938?v=4" width="100;" alt="legoeruro"/>
                    <br />
                    <sub><b>Khang Luu</b></sub>
                </a>
            </td>
            <td align="center">
                <a href="https://github.com/YiqinZhao">
                    <img src="https://avatars.githubusercontent.com/u/11468820?v=4" width="100;" alt="YiqinZhao"/>
                    <br />
                    <sub><b>Yiqin Zhao</b></sub>
                </a>
            </td>
            <td align="center">
                <a href="https://github.com/FelixNgFender">
                    <img src="https://avatars.githubusercontent.com/u/75899581?v=4" width="100;" alt="FelixNgFender"/>
                    <br />
                    <sub><b>Thinh Nguyen</b></sub>
                </a>
            </td>
		</tr>
	<tbody>
</table>
<!-- readme: contributors -end -->

## How to cite ARFlow

Please add the following citation in your publication if you used our code for your research project.

```bibtex
@inproceedings{zhao2024arflow,
author = {Zhao, Yiqin and Guo, Tian},
title = {Demo: ARFlow: A Framework for Simplifying AR Experimentation Workflow},
year = {2024},
isbn = {9798400704970},
publisher = {Association for Computing Machinery},
address = {New York, NY, USA},
url = {https://doi.org/10.1145/3638550.3643617},
doi = {10.1145/3638550.3643617},
abstract = {The recent advancement in computer vision and XR hardware has ignited the community's interest in AR systems research. Similar to traditional systems research, the evaluation of AR systems involves capturing real-world data with AR hardware and iteratively evaluating the targeted system designs [1]. However, it is challenging to conduct scalable and reproducible AR experimentation [2] due to two key reasons. First, there is a lack of integrated framework support in real-world data capturing, which makes it a time-consuming process. Second, AR data often exhibits characteristics, including temporal and spatial variations, and is in a multi-modal format, which makes it difficult to conduct controlled evaluations.},
booktitle = {Proceedings of the 25th International Workshop on Mobile Computing Systems and Applications},
pages = {154},
numpages = {1},
location = {<conf-loc>, <city>San Diego</city>, <state>CA</state>, <country>USA</country>, </conf-loc>},
series = {HOTMOBILE '24}
}
```

## Acknowledgement

This work was supported in part by NSF Grants #2105564 and #2236987, and a VMware grant.
