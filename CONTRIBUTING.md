# Contributing to ARFlow

Thank you for considering contributing to ARFlow! This document outlines the
process for contributing to the ARFlow project. Please read it carefully before
making a contribution.

<!--toc:start-->

- [Contributing to ARFlow](#contributing-to-arflow)
  - [Server Development](#server-development)
    - [Server Setup](#server-setup)
    - [Packages and Tools](#packages-and-tools)
      - [Poetry](#poetry)
      - [Protobuf](#protobuf)
      - [Rerun](#rerun)
    - [Guidelines](#guidelines)
      - [Code Style](#code-style)
      - [Type Completeness](#type-completeness)
      - [Testing](#testing)
      - [Logging](#logging)
      - [gRPC Best Practices](#grpc-best-practices)
        - [Input validation](#input-validation)
        - [Error handling](#error-handling)
        - [Protobuf versioning](#protobuf-versioning)
        - [Protobuf linting](#protobuf-linting)
        - [Type checking Protobuf bindings](#type-checking-protobuf-bindings)
        - [Graceful shutdown](#graceful-shutdown)
        - [Securing channels](#securing-channels)
      - [Continuous Integration](#continuous-integration)
      - [Documentation](#documentation)
  - [Client Development](#client-development)
    - [Client Setup](#client-setup)
    - [Architecture](#architecture)
    - [Documentation Generation](#documentation-generation)
  - [Common Issues](#common-issues)
    - [VSCode Force Changes Locale](#vscode-force-changes-locale)
    - [Running Rerun on WSL2](#running-rerun-on-wsl2)
    - [Black screen when opening the app](#black-screen-when-opening-the-app)
    - [Problem building the Android app/app crashes immediately](#problem-building-the-android-appapp-crashes-immediately)
    <!--toc:end-->

## Server Development

### Server Setup

Fork the ARFlow repository into your account:
[https://github.com/cake-lab/ARFlow/fork](https://github.com/cake-lab/ARFlow/fork)

ARFlow uses [Poetry](https://python-poetry.org) for dependency management.
Install it [here](https://python-poetry.org/docs/).

Clone the forked repository:

```shell
git clone https://github.com/{your-account}/ARFlow.git
cd ARFlow/python
poetry install
```

### Packages and Tools

#### Poetry

ARFlow uses [Poetry](https://python-poetry.org) to manage dependencies and run
commands. Dependencies and configuration are stored in the
[pyproject.toml](./python/pyproject.toml) file.

#### Protobuf

ARFlow uses [Protobuf](https://protobuf.dev) to define the communication
protocol between the server and the client. The protocol is defined in
[service.proto](./protos/arflow_grpc/service.proto) and can be compiled using
[compile.sh](./scripts/compile.sh).

#### Rerun

ARFlow uses the [Rerun](https://github.com/rerun-io/rerun) Python SDK to
visualize data collected by the ARFlow server. We also use the RRD data format
to store Rerun-compatible recordings.

### Guidelines

#### Code Style

ARFlow uses [ruff](https://docs.astral.sh/ruff/) for linting and formatting. We
also use [pyright](https://github.com/microsoft/pyright) for type checking. Make
sure you have the appropriate extensions or corresponding LSPs installed in your
editor.

These tools should run automatically in your editor. If you want to run them
manually, you can also use the following commands:

```shell
poetry run ruff check # check for linting errors

poetry run ruff check --fix # check for linting errors and fix them

poetry run ruff format # format the code
```

All of these quality checks are run automatically before every commit using
[pre-commit](https://pre-commit.com). To install the pre-commit hooks, run:

```shell
poetry run pre-commit install
```

To manually invoke the pre-commit checks, run:

```shell
poetry run pre-commit run --all-files
```

#### Type Completeness

Library authors are encouraged to prioritize bringing their public API to 100%
type coverage. Although this is very hard in ARFlow's case due to our dependency
on gRPC, we should still strive to achieve this goal. To check for type
completeness, run:

```shell
poetry run pyright --ignoreexternal --verifytypes arflow
```

To read more about formalizing libraries' public APIs, please refer to this
excellent
[blog post](https://dagster.io/blog/adding-python-types#-step-3-formalize-public-api)
by Dagster.

#### Testing

ARFlow uses [pytest](https://pytest.org). Pytest configuration can be found in
the [pyproject.toml](./python/pyproject.toml) file. To run tests, use:

```shell
# in the python directory
poetry run pytest # this will automatically pick up configuration in pyproject.toml
```

#### Logging

- Log key events for debugging and tracking.
- Avoid logging sensitive information (e.g., user data).
- For the default log level (`INFO`), typically log **once** per user action.
- Initialize a logger in each module using
  `logger = logging.getLogger(__name__)`. This enables granular logging and
  gives users control over logs from specific parts of the library.
- Use appropriate log levels:

| Level       | Usage                        |
| ----------- | ---------------------------- |
| `debug()`   | Detailed internal state info |
| `info()`    | General operational events   |
| `warning()` | Unexpected events, non-fatal |
| `error()`   | Errors, exceptions           |

Example:

```python
logger = logging.getLogger(__name__)
logger.debug("Processing request: %s", request_id)
```

#### gRPC Best Practices

The ARFlow server and client communicates through gRPC. Here are some best
practices to keep in mind when working with gRPC:

##### Input validation

All fields in proto3 are optional, so you’ll need to validate that they’re all
set. If you leave one unset, then it’ll default to zero for numeric types or to
an empty string for strings.

##### Error handling

gRPC is built on top of HTTP/2, the status code is like the standard HTTP status
code. This allows clients to take different actions based on the code they
receive. Proper error handling also allows middleware, like monitoring systems,
to log how many requests have errors.

ARFlow uses the [grpc_interceptor](https://pypi.org/project/grpc-interceptor/)
library to handle exceptions. This library provides a way to raise exceptions in
your service handlers, and have them automatically converted to gRPC status
codes. Check out an example usage
[here](https://github.com/d5h-foss/grpc-interceptor/tree/master?tab=readme-ov-file#server-interceptor).

The library also provides a testing framework to run a gRPC service with
interceptors. You can check out the example usage
[here](./python/tests/test_interceptor.py).

##### Protobuf versioning

To achieve **backward compatibility**, you should never remove a field from a
message. Instead, mark it as deprecated and add a new field with the new name.
This way, clients that use the old field will still work.

##### Protobuf linting

We use [buf](https://buf.build/) to lint our protobuf files. You can install it
by following the instructions [here](https://buf.build/docs/installation).

##### Type checking Protobuf bindings

We use [pyright](https://github.com/microsoft/pyright) and
[grpc-stubs](https://pypi.org/project/grpc-stubs/) to type check our
Protobuf-generated code.

##### Graceful shutdown

When the server is shutting down, it should wait for all in-flight requests to
complete before shutting down. This is to prevent data loss or corruption. We
have done this in the ARFlow server.

##### Securing channels

gRPC supports TLS encryption out of the box. We have not implemented this in the
ARFlow server yet. If you are interested in working on this, please let us know.

#### Continuous Integration

ARFlow uses GitHub Actions for continuous integration. The CI pipeline runs the
following checks:

```shell
poetry run ruff check # linting
poetry run pyright arflow # type checking
poetry run pytest # testing
```

#### Documentation

ARFlow uses [pdoc](https://pdoc.dev). You can refer to their documentation for
more information on how to generate documentation.

To preview the documentation locally, run:

```shell
poetry run pdoc arflow examples # or replace with module_name that you want to preview
```

## Client Development

### Client Setup

This package can be installed with Unity Package Manager's Install from Git
feature. This package has some dependencies that must be installed seperately.

1. Install these dependency packages by specifying the following URL in
   `Add package from git URL...`

```shell
https://github.com/Cysharp/YetAnotherHttpHandler.git?path=src/YetAnotherHttpHandler#{1.0.0}
```

```shell
https://github.com/atteneder/DracoUnity.git
```

1. Install the Unity Voice Processor package by importing the following
   `.unitypackage` into your Unity Project (dragging and dropping)

```shell
https://github.com/Picovoice/unity-voice-processor/blob/main/unity-voice-processor-1.0.0.unitypackage
```

1. To install the latest package version, specify the following URL in
   `Add package from git URL...` of Package Manager on Unity

```shell
https://github.com/cake-lab/ARFlow.git?path=unity/Assets/ARFlowPackage/ARFlow
```

### Architecture

The core functions are implemented in
[unity/Assets/Scripts](./unity/Assets/Scripts) directory. We show three example
ARFlow integration of three different data sources:

- Mock data: inside
  [ARFlowMockDataSample.cs](./unity/Assets/Scripts/MockDataSample/ARFlowMockDataSample.cs)
- ARFoundation device data: inside
  [ARFlowDeviceSample.cs](./unity/Assets/Scripts/DeviceSample/ARFlowDeviceSample.cs)
- Unity scene data: inside
  [ARFlowUnityDataSample.cs](./unity/Assets/Scripts/UnityDataSample/ARFlowUnityDataSample.cs)

To use ARFlow with your own device, you should directly deploy our client code
to your AR device. Please compile the Unity code for your target deployment
platform and install the compiled application.

Currently, we support the following platforms:

- iOS (iPhone, iPad)
- Android (Android Phone) (see
  [common issues](#problem-building-the-android-appapp-crashes-immediately))

<!-- TODO: client side address input and screenshot. -->

### Documentation Generation

Documentation for the C# code was generated
[docfx](https://dotnet.github.io/docfx/). To get started on building the
document:

1. Make sure you have [dotnet](https://dotnet.microsoft.com/en-us/) installed
   (preferably dotnet 6).
2. Run either [build.cmd](./unity/Documentation/scripts/build.cmd) or
   [build.sh](unity/Documentation/scripts/build.sh)

If you want to have the web page served locally, instead of the script run:

```shell
docfx docfx.json --serve
```

## Common Issues

### VSCode Force Changes Locale

VSCode may force changes the locale to `en_US.UTF-8` for git commit hooks. To
fix this, run:

```shell
sudo locale-gen en_US.UTF-8
```

### Running Rerun on WSL2

Please refer to their documentation
[documentation](https://rerun.io/docs/getting-started/troubleshooting#wsl2).

### Black screen when opening the app

In `Build Settings`, add `Scenes/DeviceData` to the scenes in `Build`. Add the
corresponding scene of which you want to run

- Sample data to test cameras (depth, RGB): add the `DeviceData` scene to build
- Demos: add the corresponding demo scene to build.

### Problem building the Android app/app crashes immediately

Building on Android is prone to some issues, regarding target SDK version
(Android version), graphics API, and more. Below are some build configuration
that has worked on our devices:

- In `Build Settings`, add `Scenes/DeviceData` to the scenes in `Build`.
- In `Player Settings`, uncheck `Auto Graphics API`, remove `Vulkan`.
- In `Player Settings`, change `Android minimal SDK version` to at least `24`
  (Android 7.0).
- In `Player Settings`, change `Scripting Backend` to `IL2CPP`.
- In `Player Settings`, check `ARMv7` and `ARM64` in `Target Architectures`.
  (Check any other architectures if needed).
- In `Player Settings`, change `Active Input Handling` to
  `Input System Package (New)`.
