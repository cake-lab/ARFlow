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
        """
        Initializes the SessionRunner with a session, device, and callback for AR frames.
        """
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
        """ Continuously captures frames from the camera, and writes to ffmpegs pipe to encode them"""
        while not self.stopped:
            ret, frame = self.camera.read()
            if not ret:
                break 
            frame_rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
            self.ffmpeg_enc.stdin.write(frame_rgb.tobytes())
            self.thread_mutex.acquire()
            self.num_frames_stored += 1
            self.thread_mutex.release()
    def encoding_reciever(self):
        """ Continuously reads encoded data from ffmpeg and stores it in a buffer"""
        while not self.stopped:
            data = self.ffmpeg_enc.stdout.read(4096)
            if not data:
                break
            self.buffer_mutex.acquire()
            self.chunk_buffer.extend(data)
            self.buffer_mutex.release()
    def start(self):
        """
        Starts this session runner, initializing camera and ffmpeg encoder.
        """
        self.stopped = False
        if self.camera is None:
            self.camera = cv2.VideoCapture(0)
            self.width = int(self.camera.get(cv2.CAP_PROP_FRAME_WIDTH))
            self.height = int(self.camera.get(cv2.CAP_PROP_FRAME_HEIGHT))
        self.ffmpeg_enc = self.ffmpeg_enc = subprocess.Popen([
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
    def stop(self):
        """ 
        Stops this session runner, releasing camera and ffmpeg resources.
        """
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
        self.gatherer_thread = threading.Thread(target=self.frame_encoder, args=(), daemon=True )
        self.gatherer_thread.start()
        self.reader_thread = threading.Thread(target=self.encoding_reciever, args=(), daemon=True)
        self.reader_thread.start()
    async def gather_camera_frame_async(self) -> None:
        """
        Gathers a camera frame, encodes it, and sends it as an ARFrame.
        Current implementation gathers and sends frames in H264 format.
        However you will find code to send individual RGB and YUV frames as well in the comment below.
        """
        """
        #This code addresses implementation for gather frames in RGB and YUV formats without compression
        #Mostly as a demonstration of how to do so in python
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
        self.buffer_mutex.acquire()
        h264 = XRCpuImage.Plane(
            data=bytes(self.chunk_buffer),
            row_stride = self.num_frames_stored, #for now utilize the row stride field to store number of frames
            pixel_stride = self.gathering_interval
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
        



