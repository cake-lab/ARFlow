import asyncio
import logging
import uuid
import websockets
from websockets.exceptions import ConnectionClosed

from arflow.models.registry import sf_encode, sf_decode
from arflow.models.session import (
    CreateSessionRequestMsg,
    CreateSessionResponseMsg,
    DeviceMsg,
    SessionMetadataMsg,
)

logger = logging.getLogger(__name__)

async def test_client():
    uri = "ws://localhost:8500"
    
    test_device = DeviceMsg(
        device_id="test-iphone-0.3.0",
        device_name="Yige's iPhone (0.3.0 mode)",
        os_version="iOS 17.0"
    )
    
    req_msg = CreateSessionRequestMsg(
        session_metadata=SessionMetadataMsg(save_path="test_session.rrd"),
        device=test_device
    )

    try:
        async with websockets.connect(uri) as websocket:
            logger.info("Connected to ARFlow WebSocket Server!")
            
            payload_bytes = sf_encode(req_msg)
            logger.info(f"Sending CreateSessionRequestMsg (Size: {len(payload_bytes)} bytes)")
            await websocket.send(payload_bytes)
            
            response_bytes = await websocket.recv()
            logger.info(f"Received raw bytes from server (Size: {len(response_bytes)} bytes)")
            
            response_obj = sf_decode(response_bytes)
            assert isinstance(response_obj, CreateSessionResponseMsg), "Expected Response Msg"
            
            logger.info(f"Successfully Decoded CreateSessionResponse! Session UUID: {response_obj.session.id.value}")
            
    except ConnectionClosed:
        logger.error("Connection closed unexpectedly.")
    except Exception as e:
        logger.error(f"Error during test: {e}")

if __name__ == "__main__":
    logging.basicConfig(level=logging.INFO)
    asyncio.run(test_client())
