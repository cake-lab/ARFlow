"""Replay tests."""

# ruff:noqa: D103

# from unittest.mock import MagicMock, patch

# from arflow import ARFlowPlayer, RegisterClientRequest
# from arflow._types import EnrichedARFlowRequest


# def test_replay_data():
#     mock_service = MagicMock()

#     # Mock the pickle load to simulate the requests history
#     mock_requests_history = [
#         EnrichedARFlowRequest(timestamp=1, data=RegisterClientRequest()),
#         EnrichedARFlowRequest(timestamp=2, data=RegisterClientRequest()),
#     ]

#     with patch("pickle.load", return_value=mock_requests_history):
#         player = ARFlowPlayer(mock_service, "dummy_path")
#         player.run()

#     # Verify the service processes the requests correctly
#     assert mock_service.RegisterClient.call_count == 2
