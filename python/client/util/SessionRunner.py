from asyncio import gather
from cakelab.arflow_grpc.v1.session_pb2 import Session
from cakelab.arflow_grpc.v1.device_pb2 import Device

from cakelab.arflow_grpc.v1.ar_frame_pb2 import ARFrame
from cakelab.arflow_grpc.v1.color_frame_pb2 import ColorFrame
from cakelab.arflow_grpc.v1.xr_cpu_image_pb2 import XRCpuImage
from cakelab.arflow_grpc.v1.vector2_int_pb2 import Vector2Int
from google.protobuf.timestamp_pb2 import Timestamp
import ffmpeg
import subprocess
import threading
import cv2
import time
from typing import Callable, Coroutine, Any

class SessionRunner:
    camera : cv2.VideoCapture | None = None
    session: Session | None = None
    device: Device | None = None
    onARFrame: Callable[[Session, ARFrame, Device], Coroutine[Any, Any, None]] | None = None
    def __init__(self, session: Session, device: Device, onARFrame: Callable[[Session, ARFrame, Device], Coroutine[Any, Any, None]], gathering_interval: int):
        self.camera = cv2.VideoCapture(0)
        self.onARFrame = onARFrame
        self.session = session
        self.device = device
        self.stopped = False
        self.gathering_interval = gathering_interval
        self.width = int(self.camera.get(cv2.CAP_PROP_FRAME_WIDTH))
        self.height = int(self.camera.get(cv2.CAP_PROP_FRAME_HEIGHT))
        self.chunk_buffer = bytearray()
        self.num_frames_stored = 0
        self.ffmpeg_enc = subprocess.Popen([
            'ffmpeg',
            '-f', 'rawvideo',
            '-pix_fmt', 'bgr24',
            '-s', f'{self.width}x{self.height}',
            '-i', 'pipe:0',
            '-c:v', 'libx264',
            '-preset', 'ultrafast',
            '-tune', 'zerolatency',
            '-f', 'h264',
            'pipe:1'
        ], stdin=subprocess.PIPE, stdout=subprocess.PIPE)
        self.thread_mutex: threading.Lock = threading.Lock()
    def frame_encoder(self):
        while not self.stopped:
            ret, frame = self.camera.read()
            frame_rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
            if not ret:
                break 
            self.ffmpeg_enc.stdin.write(frame_rgb.tobytes())
    def encoding_reciever(self):
        while not self.stopped:
            data = self.ffmpeg_enc.stdout.read(4096)
            if not data:
                break
            self.thread_mutex.acquire()
            self.num_frames_stored += 1
            self.chunk_buffer.extend(data)
            self.thread_mutex.release()
    def __del__(self):
        if self.camera is not None:
            self.camera.release()
            self.camera = None
    async def start_recording(self):
        threading.Thread(target=self.frame_encoder, args=(), daemon=True ).start()
        threading.Thread(target=self.encoding_reciever, args=(), daemon=True).start()
        print("hi")
    async def gather_camera_frame_async(self) -> None:
        """
        #Non - streaming required
        if self.camera is None:
            return
        ret, frame = self.camera.read()
        if not ret:
            return
        height, width = frame.shape[:2]
        frame_rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)

        #YUV Implementation

        yuv = (cv2.cvtColor(frame, cv2.COLOR_BGR2YUV_I420)).flatten()
        y_size = width * height
        uv_size = y_size // 4
        Y: XRCpuImage.Plane = XRCpuImage.Plane(data = (yuv[:y_size].reshape((height, width))).tobytes(), row_stride = width, pixel_stride=1)
        U: XRCpuImage.Plane = XRCpuImage.Plane(data = (yuv[y_size:y_size + uv_size].reshape((height // 2, width // 2))).tobytes(), row_stride = width // 2, pixel_stride=1)
        V: XRCpuImage.Plane = XRCpuImage.Plane(data = (yuv[y_size + uv_size:].reshape((height // 2, width // 2))).tobytes(), row_stride = width // 2, pixel_stride=1)
        # Trim the U and V planes because ARFlow adds an extra byte as it is a bug with the android format
        U.data = U.data[:-1]
        V.data = V.data[:-1]

        # rgb specific code

        RGB = XRCpuImage.Plane(data=frame_rgb.tobytes())
        """
        h264 = None
        if not self.chunk_buffer:
            return
        self.thread_mutex.acquire()
        h264 = XRCpuImage.Plane(
            data=bytes(self.chunk_buffer),
            row_stride = self.num_frames_stored, #for now utilize the row stride field to store number of frames
            pixel_stride = self.gathering_interval
        )
        self.num_frames_stored = 0
        self.chunk_buffer.clear()
        self.thread_mutex.release()
        now = time.time()
        timestamp = Timestamp()
        nanos = int(now * 1e9)
        Timestamp.FromNanoseconds(timestamp, nanos)
        xrcpu_image: XRCpuImage = XRCpuImage(
            dimensions= Vector2Int(x=self.width, y=self.height),
            #format= XRCpuImage.FORMAT_ANDROID_YUV_420_888,
            timestamp=now,
            format=16, # make a new format for streaming data
            planes=[h264],
        )
        color_frame = ColorFrame(
            image=xrcpu_image,
            device_timestamp=timestamp,

        )
        ar_frame = ARFrame(
            color_frame=color_frame
        )
        if self.onARFrame and self.session and self.device:
            await self.onARFrame(self.session, ar_frame, self.device)
        



