from sensorflex.library.net._distributed import get_msgpack_coder_transforms

from arflow.models.session import (
    DeviceMsg,
    SessionUuidMsg,
    SessionMetadataMsg,
    SessionMsg,
    CreateSessionRequestMsg,
    CreateSessionResponseMsg,
    DeleteSessionRequestMsg,
    DeleteSessionResponseMsg,
    GetSessionRequestMsg,
    GetSessionResponseMsg,
    ListSessionsRequestMsg,
    ListSessionsResponseMsg,
    JoinSessionRequestMsg,
    JoinSessionResponseMsg,
    LeaveSessionRequestMsg,
    LeaveSessionResponseMsg,
)

# Note: The order of this list will determine the SensorFlex Type ID (0, 1, 2...) for each Message
# This order is extremely important, it must be strictly consistent with the Unity end, and the order of existing items should not be changed later!
ARFLOW_MESSAGE_MODELS = [
    DeviceMsg,  # 0
    SessionUuidMsg,  # 1
    SessionMetadataMsg,  # 2
    SessionMsg,  # 3
    CreateSessionRequestMsg,  # 4
    CreateSessionResponseMsg,  # 5
    DeleteSessionRequestMsg,  # 6
    DeleteSessionResponseMsg,  # 7
    GetSessionRequestMsg,  # 8
    GetSessionResponseMsg,  # 9
    ListSessionsRequestMsg,  # 10
    ListSessionsResponseMsg,  # 11
    JoinSessionRequestMsg,  # 12
    JoinSessionResponseMsg,  # 13
    LeaveSessionRequestMsg,  # 14
    LeaveSessionResponseMsg,  # 15
]

sf_encode, sf_decode = get_msgpack_coder_transforms(*ARFLOW_MESSAGE_MODELS)
