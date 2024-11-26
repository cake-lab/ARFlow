"""Session helps its participating devices to stream to the same Rerun recording."""

import rerun as rr

from arflow._types import (
    DecodedCameraFrames,
)
from cakelab.arflow_grpc.v1.camera_frame_pb2 import CameraFrame
from cakelab.arflow_grpc.v1.device_pb2 import Device
from cakelab.arflow_grpc.v1.session_pb2 import Session
from cakelab.arflow_grpc.v1.timeline_pb2 import Timeline


class SessionStream:
    """All devices in a session share a stream."""

    def __init__(self, info: Session, stream: rr.RecordingStream):
        self.info = info
        """The session information."""
        self.stream = stream
        """The recording stream handle to the an associated Rerun recording for this session."""

    def save_camera_frames(
        self,
        decoded_camera_frames: DecodedCameraFrames,
        device_timestamps: list[float],
        image_timestamps: list[float],
        device: Device,
        format: CameraFrame.Format,
        width: int,
        height: int,
    ):
        """Save camera frames to the stream. Assumes that the device is in the session and the data is "nice" (e.g., all frames have the same format, width, height, and originating device).

        Raises:
            ValueError: If the decoded camera frames, device timestamps, and image timestamps do not have the same length or if the frame format is not supported.

        @private
        """
        if (
            len(decoded_camera_frames) == 0
            or len(decoded_camera_frames) != len(image_timestamps)
            or len(decoded_camera_frames) != len(device_timestamps)
        ):
            raise ValueError(
                "The decoded camera frames, device timestamps, and image timestamps must have the same length"
            )

        entity_path = rr.new_entity_path(
            [
                self.info.id.value,
                device.uid,
                "camera",
                f"{width}x{height}",
            ]
        )

        if format == CameraFrame.FORMAT_RGB24:
            format_static = rr.components.ImageFormat(
                width=width,
                height=height,
                color_model=rr.ColorModel.RGB,
                channel_datatype=rr.ChannelDatatype.U8,
            )
        elif format == CameraFrame.FORMAT_RGBA32:
            format_static = rr.components.ImageFormat(
                width=width,
                height=height,
                color_model=rr.ColorModel.RGBA,
                channel_datatype=rr.ChannelDatatype.U8,
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
                    data=decoded_camera_frames.reshape(len(image_timestamps), -1)
                )
            ],
            recording=self.stream,
        )
