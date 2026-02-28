"""Data exchanging service via WebSockets."""

import os
import pickle
import time
import uuid
import asyncio
import logging
from time import gmtime, strftime
from typing import Dict, List, Any

import numpy as np
import rerun as rr
from websockets.asyncio.server import serve, ServerConnection
from websockets.exceptions import ConnectionClosed

from arflow.models.registry import sf_decode, sf_encode
from arflow.models.session import (
    CreateSessionRequestMsg,
    CreateSessionResponseMsg,
    SessionMsg,
    SessionUuidMsg,
    SessionMetadataMsg,
)

logger = logging.getLogger(__name__)

# 全局的 Session 管理字典，存 MsgSpec 定义的数据
sessions: Dict[str, CreateSessionRequestMsg] = {}
"""@private"""

class ARFlowWebSocketServer:
    """ARFlow WebSocket service."""

    _start_time = time.time_ns()
    _frame_data: List[Dict[str, float | bytes]] = []

    def __init__(self, application_id: str = "arflow", spawn_viewer: bool = True) -> None:
        self.recorder = rr
        self.application_id = application_id
        self.spawn_viewer = spawn_viewer
        # _core.py 时代，是在 register 的时候才 init，我们保留这个设计或在 Server 启动时 init
        # 这里为兼容 0.3 的逻辑，选择在 handle_create_session 时处理

    def _save_frame_data(self, message_type_name: str, message_bytes: bytes):
        """@private: 为了在退出时保存 session 的记录"""
        time_stamp = (time.time_ns() - self._start_time) / 1e9
        self._frame_data.append(
            {"time_stamp": time_stamp, "data": message_bytes, "type": message_type_name}
        )

    async def handle_client(self, websocket: ServerConnection):
        logger.info(f"Client connected: {websocket.remote_address}")
        try:
            async for message in websocket:
                if isinstance(message, bytes):
                    await self._process_message(websocket, message)
                else:
                    logger.warning("Received non-binary message, dropping.")
        except ConnectionClosed:
            logger.info(f"Client disconnected: {websocket.remote_address}")
        except Exception as e:
            logger.error(f"Error handling client {websocket.remote_address}: {e}")

    async def _process_message(self, websocket: ServerConnection, message: bytes):
        try:
            obj = sf_decode(message)
        except Exception as e:
            logger.error(f"Failed to decode message: {e}")
            return

        if isinstance(obj, CreateSessionRequestMsg):
            # 将这个 CreateSessionRequestMsg 储存起来
            self._save_frame_data("CreateSessionRequestMsg", message)
            await self.handle_create_session(websocket, obj)
        else:
            # 预留给以后的 Image 等传输 Frame
            logger.warning(f"Unhandled message type: {type(obj)}")

    async def handle_create_session(self, websocket: ServerConnection, request: CreateSessionRequestMsg):
        """Register a client via CreateSession request."""
        new_session_id = str(uuid.uuid4())
        
        sessions[new_session_id] = request

        # 唤醒 Rerun Viewer
        self.recorder.init(f"{request.device.device_name} - ARFlow", spawn=self.spawn_viewer)
        print("Registered a client with UUID: %s" % new_session_id, request)

        # Call the for user extension code.
        self.on_register(request)

        # 构建发回客户端的凭证
        session_msg = SessionMsg(
            id=SessionUuidMsg(value=new_session_id),
            metadata=request.session_metadata,
            devices=[request.device],
        )
        response = CreateSessionResponseMsg(session=session_msg)
        await websocket.send(sf_encode(response))

    def on_register(self, request: CreateSessionRequestMsg):
        """Called when a new device is registered. Override this method to process the data."""
        pass

    def on_frame_received(self, frame_data: Any):
        """Called when a frame is received. Override this method to process the data."""
        pass

    def on_program_exit(self, path_to_save: str | None):
        """Save the data and exit."""
        if path_to_save is None:
            return
        print("Saving the data...")
        f_name = strftime("%Y_%m_%d_%H_%M_%S", gmtime())
        save_path = os.path.join(path_to_save, f"frames_{f_name}.pkl")
        with open(save_path, "wb") as f:
            pickle.dump(self._frame_data, f)

        print(f"Data saved to {save_path}")

    # 省略 60FPS 的 Decode 函数，未来第六周重写此部分时会补充上来