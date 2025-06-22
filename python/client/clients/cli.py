

from GrpcClient import GrpcClient
from util.GetDeviceInfo import GetDeviceInfo
from util.SessionRunner import SessionRunner

from cakelab.arflow_grpc.v1.device_pb2 import Device
from cakelab.arflow_grpc.v1.session_pb2 import Session
from cakelab.arflow_grpc.v1.ar_frame_pb2 import ARFrame

from cakelab.arflow_grpc.v1.create_session_response_pb2 import CreateSessionResponse
from cakelab.arflow_grpc.v1.list_sessions_response_pb2 import ListSessionsResponse

import asyncio
from threading import Thread
from threading import Event
from time import sleep

class CLIClient:
    session: Session | None = None
    running: Thread | None = None
    stop_event: Event | None = None
    def __init__(self):
        host = input("Enter hostname: ")
        port = input("Enter port: ")
        self.client = GrpcClient(f"{host}:{port}")
        asyncio.run(self.__manage_sessions())

    async def __manage_sessions(self):
        while True:
            print("Available Sessions:")
            session_list: list[Session] = list((await self.client.list_sessions_async()).sessions)
            for session in session_list:
                print(f"   Name: {session.metadata.name}, # of Devices: {len(session.devices)}")
            print("Options:")
            print("1. Create and Join  Session")
            print("2. Join Session")
            print("3. Delete Session")
            print("4. Refresh")
            print("5. Exit")
            choice: str = input("Choose an option: ")
            match choice:
                case "1":
                    name: str = input("Enter session name: ")
                    if any(session.metadata.name == name for session in session_list):
                        name = input(f"Session with name '{name}' already exists. Please choose a different name:")
                    device: Device = GetDeviceInfo.get_device_info()
                    try:
                        self.session = (await self.client.create_session_async(name, device)).session
                        print("Created Session")
                        await self.__join_session()
                    except Exception as e:
                        print(f"Failed to create session: {e}")
                case "2":
                    name: str = input("Enter session name to join: ")
                    target_session: Session | None = next((session for session in session_list if session.metadata.name == name), None)
                    if target_session is None:
                        print(f"No session found with name '{name}'")
                        continue
                    try:
                        self.session = (await self.client.join_session_async(target_session.id.value, GetDeviceInfo.get_device_info())).session
                        print("Joined Session")
                        await self.__join_session()
                    except Exception as e:
                        print(f"Failed to join session: {e}")
                case "3":
                    name: str = input("Enter session name to delete: ")
                    target_session: Session | None = next((session for session in session_list if session.metadata.name == name), None)
                    if target_session is None:
                        print(f"No session found with name '{name}'")
                        continue
                    try:
                        await self.client.delete_session_async(target_session.id.value)
                        print(f"Session '{name}' deleted successfully.")
                    except Exception as e:
                        print(f"Failed to delete session: {e}")
                case "4":
                    continue
                case "5":
                    return
                case _:
                    print("Invalid Option")

    async def __join_session(self):
        if self.session is None: return
        runner = SessionRunner(self.session, GetDeviceInfo.get_device_info(), self.__on_frame, 250)
        print("Currently only able to record camera frames")
        while True:
            print("Options:")
            if self.running:
                print("1. Stop Recording")
            else:
                print("1. Start Recording")
            print("2. Leave Session")
            choice: str = input("Choose an option: ")
            match choice:
                case "1":
                    if self.running:
                        self.stop_event.set()
                        self.running.join()
                        self.running = None
                        self.stop_event = None
                        print("Stopping Recording")
                    else:
                        self.stop_event = Event()
                        self.running = Thread(target=self.__record_frame, args=(runner,))
                        self.running.start()
                        await runner.start_recording()
                        print("Starting Recording")
                case "2":
                    if self.running:
                        self.stop_event.set()
                        self.running.join()
                        self.running = None
                        self.stop_event = None
                    await self.client.leave_session_async(self.session.id.value, GetDeviceInfo.get_device_info())
                    print("Leaving Session")
                    return
                case _:
                    print("Invalid Option")

    def __record_frame(self, runner: SessionRunner):
        while not self.stop_event.is_set():
            asyncio.run(runner.gather_camera_frame_async())
            sleep(0.25)


    async def __on_frame(self, session: Session, frame: ARFrame , device: Device):
        if self.session is None:
            return
        await self.client.save_ar_frames_async(
            session_id=self.session.id.value,
            ar_frames=[frame],
            device=GetDeviceInfo.get_device_info()
        )

def main():
    CLIClient()
    
    
if __name__ == "__main__":
    main()