

from GrpcClient import GrpcClient
from util.GetDeviceInfo import GetDeviceInfo
from util.SessionRunner import SessionRunner

from cakelab.arflow_grpc.v1.device_pb2 import Device
from cakelab.arflow_grpc.v1.session_pb2 import Session

from cakelab.arflow_grpc.v1.create_session_response_pb2 import CreateSessionResponse
from cakelab.arflow_grpc.v1.list_sessions_response_pb2 import ListSessionsResponse
import asyncio

class CLIClient:
    session: Session | None = None
    def __init__(self):
        host = input("Enter hostname")
        port = input("Enter port")
        self.client = GrpcClient(f"{host}:{port}")
        asyncio.run(self.__manage_sessions())

    async def __manage_sessions(self):
        while True:
            print("Available Sessions:")
            session_list: list[Session] = list((await self.client.ListSessionsAsync()).sessions)
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
                        session = (await self.client.CreateSessionAsync(name, device)).session
                        print("Created Session")
                        await self.__join_session(session)
                    except Exception as e:
                        print(f"Failed to create session: {e}")
                case "2":
                    name: str = input("Enter session name to join: ")
                    target_session: Session | None = next((session for session in session_list if session.metadata.name == name), None)
                    if target_session is None:
                        print(f"No session found with name '{name}'")
                        continue
                    try:
                        session = await self.client.JoinSessionAsync(target_session.id.value, GetDeviceInfo.get_device_info())
                        print("Joined Session")
                        await self.__join_session(session)
                    except Exception as e:
                        print(f"Failed to join session: {e}")
                case "3":
                    name: str = input("Enter session name to delete: ")
                    target_session: Session | None = next((session for session in session_list if session.metadata.name == name), None)
                    if target_session is None:
                        print(f"No session found with name '{name}'")
                        continue
                    try:
                        await self.client.DeleteSessionAsync(target_session.id.value)
                        print(f"Session '{name}' deleted successfully.")
                    except Exception as e:
                        print(f"Failed to delete session: {e}")
                case "4":
                    continue
                case "5":
                    return
                case _:
                    print("Invalid Option")
    async def __join_session(self, session: Session):
        print("Active session logic missing! Implement me!")
def main():
    CLIClient()
    
    
if __name__ == "__main__":
    main()