"""Session helps its participating devices to stream to the same Rerun recording."""

import rerun as rr

from arflow._types import (
    ARFrameType,
    DecodedColorFrames,
    DecodedDepthFrames,
)
from cakelab.arflow_grpc.v1.device_pb2 import Device
from cakelab.arflow_grpc.v1.session_pb2 import Session
from cakelab.arflow_grpc.v1.timeline_pb2 import Timeline
from cakelab.arflow_grpc.v1.xr_cpu_image_pb2 import XRCpuImage


class SessionStream:
    """All devices in a session share a stream."""

    def __init__(self, info: Session, stream: rr.RecordingStream):
        self.info = info
        """The session information."""
        self.stream = stream
        """The recording stream handle to the an associated Rerun recording for this session."""

    def save_color_frames(
        self,
        frames: DecodedColorFrames,
        device: Device,
        format: XRCpuImage.Format,
        width: int,
        height: int,
        device_timestamps: list[float],
        image_timestamps: list[float],
    ):
        """Save color frames to the stream. Assumes that the device is in the session and the data is "nice" (e.g., all frames have the same format, width, height, and originating device).

        Raises:
            ValueError: If the decoded color frames, device timestamps, and image timestamps do not have the same length or if the frame format is not supported.

        @private
        """
        if (
            len(frames) == 0
            or len(frames) != len(image_timestamps)
            or len(frames) != len(device_timestamps)
        ):
            raise ValueError(
                "The decoded color frames, device timestamps, and image timestamps must have the same length"
            )

        entity_path = rr.new_entity_path(
            [
                self.info.id.value,
                device.uid,
                ARFrameType.COLOR_FRAME,
                f"{width}x{height}",
            ]
        )

        if format == XRCpuImage.FORMAT_ANDROID_YUV_420_888:
            format_static = rr.components.ImageFormat(
                width=width,
                height=height,
                pixel_format=rr.PixelFormat.Y_U_V12_LimitedRange,
            )
        elif format == XRCpuImage.FORMAT_IOS_YP_CBCR_420_8BI_PLANAR_FULL_RANGE:
            format_static = rr.components.ImageFormat(
                width=width,
                height=height,
                pixel_format=rr.PixelFormat.NV12,
            )
        else:
            raise ValueError(f"Unsupported frame format: {format}")

        rr.log(
            entity_path,
            [format_static, rr.Image.indicator()],
            static=True,
            recording=self.stream,
        )
        # TODO: What is the behavior here when using view mode but for multiple RecordingStreams?
        rr.send_columns(
            entity_path,
            times=[
                rr.TimeSecondsColumn(
                    timeline=Timeline.Name(Timeline.TIMELINE_DEVICE_TIMESTAMP),
                    times=device_timestamps,
                ),
                rr.TimeSecondsColumn(
                    timeline=Timeline.Name(Timeline.TIMELINE_IMAGE_TIMESTAMP),
                    times=image_timestamps,
                ),
            ],
            components=[
                rr.components.ImageBufferBatch(
                    data=frames.reshape(len(image_timestamps), -1)
                )
            ],
            recording=self.stream,
        )

    def save_depth_frames(
        self,
        frames: DecodedDepthFrames,
        device: Device,
        format: XRCpuImage.Format,
        width: int,
        height: int,
        device_timestamps: list[float],
        image_timestamps: list[float],
    ):
        """Save depth frames to the stream. Assumes that the device is in the session and the data is "nice" (e.g., all frames have the same format, width, height, and originating device).

        Raises:
            ValueError: If the decoded depth frames, device timestamps, and image timestamps do not have the same length or if the frame format is not supported.

        @private
        """
        if (
            len(frames) == 0
            or len(frames) != len(image_timestamps)
            or len(frames) != len(device_timestamps)
        ):
            raise ValueError(
                "The decoded depth frames, device timestamps, and image timestamps must have the same length"
            )

        entity_path = rr.new_entity_path(
            [
                self.info.id.value,
                device.uid,
                ARFrameType.DEPTH_FRAME,
                f"{width}x{height}",
            ]
        )

        if format == XRCpuImage.FORMAT_DEPTHFLOAT32:
            format_static = rr.components.ImageFormat(
                width=width,
                height=height,
                color_model=rr.ColorModel.L,
                channel_datatype=rr.ChannelDatatype.F32,
            )
        elif format == XRCpuImage.FORMAT_DEPTHUINT16:
            format_static = rr.components.ImageFormat(
                width=width,
                height=height,
                color_model=rr.ColorModel.L,
                channel_datatype=rr.ChannelDatatype.U16,
            )
        else:
            raise ValueError(f"Unsupported frame format: {format}")

        rr.log(
            entity_path,
            [format_static, rr.DepthImage.indicator()],
            static=True,
            recording=self.stream,
        )
        rr.send_columns(
            entity_path,
            times=[
                rr.TimeSecondsColumn(
                    timeline=Timeline.Name(Timeline.TIMELINE_DEVICE_TIMESTAMP),
                    times=device_timestamps,
                ),
                rr.TimeSecondsColumn(
                    timeline=Timeline.Name(Timeline.TIMELINE_IMAGE_TIMESTAMP),
                    times=image_timestamps,
                ),
            ],
            components=[
                rr.components.ImageBufferBatch(
                    data=frames.reshape(len(image_timestamps), -1)
                )
            ],
            recording=self.stream,
        )
