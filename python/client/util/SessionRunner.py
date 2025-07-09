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
        success, encoded = cv2.imencode('.jpg', frame)
        if not success:
            return
        plane: XRCpuImage.Plane = XRCpuImage.Plane(
            data=encoded.tobytes(),
            row_stride=0,
            pixel_stride=0,
        )
        now = time.time()
        timestamp = Timestamp()
        nanos = int(now * 1e9)
        Timestamp.FromNanoseconds(timestamp, nanos)
        xrcpu_image: XRCpuImage = XRCpuImage(
            dimensions= Vector2Int(x=width, y=height),
            format= XRCpuImage.Format.FORMAT_JPEG_RGB24,
            timestamp=now,
            planes=[plane]
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
        



