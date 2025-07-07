"""This module provides a SessionRunner class that manages AR sessions and gathers AR frames."""
import subprocess
import threading
import time
from asyncio import gather
from typing import Any, Callable, Coroutine

import cv2
import ffmpeg
from google.protobuf.timestamp_pb2 import Timestamp

from cakelab.arflow_grpc.v1.ar_frame_pb2 import ARFrame
from cakelab.arflow_grpc.v1.color_frame_pb2 import ColorFrame
from cakelab.arflow_grpc.v1.device_pb2 import Device
from cakelab.arflow_grpc.v1.session_pb2 import Session
from cakelab.arflow_grpc.v1.vector2_int_pb2 import Vector2Int
from cakelab.arflow_grpc.v1.xr_cpu_image_pb2 import XRCpuImage


class SessionRunner:
    """A class which represents a sessions available functionality into simply calling the onArFrame callback every time an ARFrame is gathered.

    For example, gathering gyroscope, color, depth, and other frames.
    For now, only supports gathering frames in H264 format.
    """

    camera: cv2.VideoCapture | None = None
    session: Session | None = None
    device: Device | None = None
    onARFrame: (
        Callable[[Session, ARFrame, Device], Coroutine[Any, Any, None]] | None
    ) = None

    def __init__(
        self,
        session: Session,
        device: Device,
        onARFrame: Callable[[Session, ARFrame, Device], Coroutine[Any, Any, None]],
        gathering_interval: int,
    ):
        """Initializes the SessionRunner with a session, device, and callback for AR frames."""
        self.onARFrame = onARFrame
        self.session = session
        self.device = device
        self.stopped = False
        self.gatherer_thread = None
        self.reader_thread = None
        self.gathering_interval = gathering_interval
        self.width = None
        self.height = None
        self.chunk_buffer = bytearray()
        self.num_frames_stored = 0
        self.ffmpeg_enc = None
        self.thread_mutex: threading.Lock = threading.Lock()
        self.buffer_mutex: threading.Lock = threading.Lock()

    def frame_encoder(self):
        """Continuously captures frames from the camera, and writes to ffmpegs pipe to encode them."""
        while not self.stopped:
            ret, frame = self.camera.read()
            if not ret:
                break
            frame_rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
            self.ffmpeg_enc.stdin.write(frame_rgb.tobytes())
            self.thread_mutex.acquire()
            self.num_frames_stored += 1
            self.thread_mutex.release()

    def encoding_receiver(self):
        """Continuously reads encoded data from ffmpeg and stores it in a buffer."""
        while not self.stopped:
            data = self.ffmpeg_enc.stdout.read(4096)
            if not data:
                break
            self.buffer_mutex.acquire()
            self.chunk_buffer.extend(data)
            self.buffer_mutex.release()

    def start(self):
        """Starts this session runner, initializing camera and ffmpeg encoder."""
        self.stopped = False
        if self.camera is None:
            self.camera = cv2.VideoCapture(0)
            self.width = int(self.camera.get(cv2.CAP_PROP_FRAME_WIDTH))
            self.height = int(self.camera.get(cv2.CAP_PROP_FRAME_HEIGHT))
        self.ffmpeg_enc = self.ffmpeg_enc = subprocess.Popen(
            [
                "ffmpeg",
                "-f",
                "rawvideo",
                "-pix_fmt",
                "bgr24",
                "-s",
                f"{self.width}x{self.height}",
                "-i",
                "pipe:0",
                "-c:v",
                "libx264",
                "-preset",
                "ultrafast",
                "-tune",
                "zerolatency",
                "-f",
                "h264",
                "pipe:1",
            ],
            stdin=subprocess.PIPE,
            stdout=subprocess.PIPE,
        )

    def stop(self):
        """Stops this session runner, releasing camera and ffmpeg resources."""
        self.stopped = True
        if self.camera is not None:
            self.camera.release()
            self.camera = None
        if self.ffmpeg_enc is not None:
            self.ffmpeg_enc.stdin.close()
            self.ffmpeg_enc.stdout.close()
            self.ffmpeg_enc.wait()
            self.ffmpeg_enc = None
        if self.gatherer_thread is not None:
            self.gatherer_thread.join()
            self.gatherer_thread = None
        if self.reader_thread is not None:
            self.reader_thread.join()
            self.reader_thread = None

    async def start_recording(self):
        """Starts the recording process by initializing a threads for frame gathering and encoding."""
        self.gatherer_thread = threading.Thread(
            target=self.frame_encoder, args=(), daemon=True
        )
        self.gatherer_thread.start()
        self.reader_thread = threading.Thread(
            target=self.encoding_receiver, args=(), daemon=True
        )
        self.reader_thread.start()

    async def gather_camera_frame_async(self) -> None:
        """Gathers the needed data for camera frame(s), encodes it(them), and sends it(them) as an ARFrame.

        Current implementation gathers and sends frames in H264 format.
        Look in main branch for support for other formats.
        """
        h264 = None
        if not self.chunk_buffer:
            return
        self.thread_mutex.acquire()
        self.buffer_mutex.acquire()
        h264 = XRCpuImage.Plane(
            data=bytes(self.chunk_buffer),
            row_stride=self.num_frames_stored,  # for now utilize the row stride field to store number of frames
            pixel_stride=self.gathering_interval,
        )
        self.num_frames_stored = 0
        self.chunk_buffer.clear()
        self.buffer_mutex.release()
        self.thread_mutex.release()
        now = time.time()
        timestamp = Timestamp()
        nanos = int(now * 1e9)
        Timestamp.FromNanoseconds(timestamp, nanos)
        xrcpu_image: XRCpuImage = XRCpuImage(
            dimensions=Vector2Int(x=self.width, y=self.height),
            timestamp=now,
            format=XRCpuImage.FORMAT_H264RGB24,
            planes=[h264],
        )
        color_frame = ColorFrame(
            image=xrcpu_image,
            device_timestamp=timestamp,
        )
        ar_frame = ARFrame(color_frame=color_frame)
        if self.onARFrame and self.session and self.device:
            await self.onARFrame(self.session, ar_frame, self.device)
