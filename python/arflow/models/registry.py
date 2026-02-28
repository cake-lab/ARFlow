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

# 注意：此列表的顺序将决定每种 Message 对应的 SensorFlex Type ID (0, 1, 2...)
# 这个顺序极其重要，必须与 Unity 端保持严格一致，且后期不再更改已有项的顺序！
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
