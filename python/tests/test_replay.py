"""Replay unit tests."""

# ruff:noqa: D103

# from unittest.mock import MagicMock, patch

# from arflow import ARFlowPlayer, ClientConfiguration
# from arflow._types import EnrichedARFlowRequest


# def test_replay_data():
#     mock_service = MagicMock()

#     # Mock the pickle load to simulate the requests history
#     mock_requests_history = [
#         EnrichedARFlowRequest(timestamp=1, data=ClientConfiguration()),
#         EnrichedARFlowRequest(timestamp=2, data=ClientConfiguration()),
#     ]

#     with patch("pickle.load", return_value=mock_requests_history):
#         player = ARFlowPlayer(mock_service, "dummy_path")
#         player.run()

#     # Verify the service processes the requests correctly
#     assert mock_service.RegisterClient.call_count == 2
