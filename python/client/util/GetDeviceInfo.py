from cakelab.arflow_grpc.v1.device_pb2 import Device
import platform
import uuid

class GetDeviceInfo:
    @staticmethod
    def get_device_info() -> Device:
        name = platform.node()
        #not sure what model is, im just gonna leave it as system name combined with version, change this later
        model = platform.system() + platform.version()
        return Device(
            name=name,
            model=model,
            type=3, #for now only functions on desktops
            uid= str(uuid.uuid3(uuid.NAMESPACE_DNS, name + model))
        )