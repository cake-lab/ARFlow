"""A class that gets the device information for the current device."""
import platform
import uuid

from cakelab.arflow_grpc.v1.device_pb2 import Device


class GetDeviceInfo:
    """A class that gets the device information for the current device."""
    @staticmethod
    def get_device_info() -> Device:
        """Gets the device information for the current device."""
        name = platform.node()
        #not sure what model is, im just gonna leave it as system name combined with version, change this later
        model = platform.system() + platform.version()
        return Device(
            name=name,
            model=model,
            type=3, #for now only functions on desktops
            uid= str(uuid.uuid3(uuid.NAMESPACE_DNS, name + model))
        )