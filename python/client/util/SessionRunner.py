from cakelab.arflow_grpc.v1.session_pb2 import Session
from cakelab.arflow_grpc.v1.device_pb2 import Device

from cakelab.arflow_grpc.v1.ar_frame_pb2 import ARFrame
from cakelab.arflow_grpc.v1.color_frame_pb2 import ColorFrame
from cakelab.arflow_grpc.v1.xr_cpu_image_pb2 import XRCpuImage
from cakelab.arflow_grpc.v1.vector2_int_pb2 import Vector2Int
from google.protobuf.timestamp_pb2 import Timestamp
import cv2
import time
from typing import Callable, Coroutine, Any

class SessionRunner:
    camera : cv2.VideoCapture | None = None
    session: Session | None = None
    device: Device | None = None
    onARFrame: Callable[[Session, ARFrame, Device], Coroutine[Any, Any, None]] | None = None
    def __init__(self, session: Session, device: Device, onARFrame: Callable[[Session, ARFrame, Device], Coroutine[Any, Any, None]]):
        self.camera = cv2.VideoCapture(0)
        self.onARFrame = onARFrame
        self.session = session
        self.device = device
    def __del__(self):
        if self.camera is not None:
            self.camera.release()
            self.camera = None
    async def gather_camera_frame_async(self) -> None:
        if self.camera is None:
            return
        ret, frame = self.camera.read()
        if not ret:
            return
        height, width = frame.shape[:2]
        yuv = (cv2.cvtColor(frame, cv2.COLOR_BGR2YUV_I420)).flatten()
        y_size = width * height
        uv_size = y_size // 4
        Y: XRCpuImage.Plane = XRCpuImage.Plane(data = (yuv[:y_size].reshape((height, width))).tobytes(), row_stride = width, pixel_stride=1)
        U: XRCpuImage.Plane = XRCpuImage.Plane(data = (yuv[y_size:y_size + uv_size].reshape((height // 2, width // 2))).tobytes(), row_stride = width // 2, pixel_stride=1)
        V: XRCpuImage.Plane = XRCpuImage.Plane(data = (yuv[y_size + uv_size:].reshape((height // 2, width // 2))).tobytes(), row_stride = width // 2, pixel_stride=1)
        # Trim the U and V planes because ARFlow adds an extra byte as it is a bug with the android format
        U.data = U.data[:-1]
        V.data = V.data[:-1]
        now = time.time()
        timestamp = Timestamp()
        nanos = int(now * 1e9)
        Timestamp.FromNanoseconds(timestamp, nanos)
        xrcpu_image: XRCpuImage = XRCpuImage(
            dimensions= Vector2Int(x=width, y=height),
            format= XRCpuImage.FORMAT_ANDROID_YUV_420_888,
            timestamp=now,
            planes=[Y, U, V]
        )
        color_frame = ColorFrame(
            image=xrcpu_image,
            device_timestamp=timestamp
        )
        ar_frame = ARFrame(
            color_frame=color_frame
        )
        if self.onARFrame and self.session and self.device:
            await self.onARFrame(self.session, ar_frame, self.device)
        



