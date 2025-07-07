"""Pretty much a clone of the Unity GetDeviceInfo class, but in Python. (except for uid/type).

This class is used to get the device information such as name, model, type, and uid.
"""
import platform
import uuid

from cakelab.arflow_grpc.v1.device_pb2 import Device


class GetDeviceInfo:
    """Pretty much a clone of the Unity GetDeviceInfo class, but in Python. (except for uid/type).

    This class is used to get the device information such as name, model, type, and uid.
    """

    @staticmethod
    def get_device_info() -> Device:
        """Get the device information."""
        name = platform.node()
        # not sure what model is, im just gonna leave it as system name combined with version, change this later
        model = platform.system() + platform.version()
        return Device(
            name=name,
            model=model,
            type=3,  # for now only functions on desktops
            uid=str(uuid.uuid3(uuid.NAMESPACE_DNS, name + model)),
        )
