#!/usr/bin/env python3
"""Real ARFlow Compression Test.

This script runs a test for a suite of tests on the device by connecting to an existing ARFlow server
and creating a session. It then starts recording on its own.
"""

import argparse
import asyncio
import csv
import json
import sys
import time
import traceback
from dataclasses import dataclass
from datetime import datetime
from pathlib import Path
from typing import Any, List

import cv2

# Add ARFlow to path
sys.path.insert(0, str(Path(__file__).parent.parent))

try:
    import psutil  # type: ignore

    from cakelab.arflow_grpc.v1.ar_frame_pb2 import ARFrame
    from cakelab.arflow_grpc.v1.device_pb2 import Device
    from cakelab.arflow_grpc.v1.session_pb2 import Session
    from client.GrpcClient import GrpcClient
    from client.util.GetDeviceInfo import GetDeviceInfo
    from client.util.SessionRunner import SessionRunner
except ImportError as e:
    print(f"❌ Failed to import required components: {e}")
    print("Make sure you're running this from the python/benchmarks directory")
    print("Required: pip install psutil opencv-python")
    sys.exit(1)


@dataclass
class TestConfig:
    """Configuration for a real ARFlow test."""

    name: str
    duration: int  # seconds
    description: str
    width: int
    height: int


@dataclass
class TestResult:
    """Results from a real ARFlow test."""

    config: TestConfig
    frames_sent: int
    bytes_sent: int
    fps_achieved: float
    cpu_usage_avg: float | str
    memory_usage_mb: float | str
    bandwidth_mbps: float
    psnr: float | None
    errors: List[str]
    width: int
    height: int


class RealARFlowTester:
    """Test compression impact using real ARFlow components."""

    def __init__(self, server_host: str = "localhost", server_port: int = 8500):
        """Initialize the tester with server details."""
        self.server_host = server_host
        self.server_port = server_port
        self.configs = [
            TestConfig(
                name="ARFlow_SD_PNG",
                duration=30,
                description="ARFlow SD (PNG Compressed)",
                width=640,
                height=480,
            ),
            TestConfig(
                name="ARFlow_HD_PNG",
                duration=30,
                description="ARFlow HD (PNG Compressed)",
                width=1280,
                height=720,
            ),
        ]
        self.results: List[TestResult] = []
        self.client = GrpcClient(f"{self.server_host}:{self.server_port}")

    async def get_existing_session(self) -> tuple[Any, Device]:
        """Get an existing session from a connected device."""
        print("📱 Looking for existing sessions...")

        # Create gRPC client
        self.client = self.client

        # List existing sessions
        response = await self.client.list_sessions_async()
        sessions = response.sessions  # type: ignore
        

        for session in sessions:
            if session.id.value == "test_session":
                await self.client.delete_session_async("test_session")
        session = (await self.client.create_session_async("test_session", GetDeviceInfo.get_device_info(), "")).session
        print(
            f"✅ Found session: {session.metadata.name} from device: {session.devices[0].name}"
        )

        # Get device info from the session
        device = session.devices[0]

        return session, device

    async def run_test(self, config: TestConfig) -> TestResult:
        """Run a single test configuration."""
        print(f"\n🧪 Testing: {config.name}")
        print(f"   Duration: {config.duration}s")

        errors: List[str] = []
        frames_sent = 0
        bytes_sent = 0
        cpu_usage_samples: List[float] = []
        memory_usage_samples: List[float] = []

        try:
            # Get existing session from
            session, device = await self.get_existing_session()

            # Track metrics
            start_time = time.time()
            process = psutil.Process()
            initial_memory = process.memory_info().rss / 1024 / 1024  # MB

            # Frame callback to track metrics
            async def on_frame(sess: Any, frame: ARFrame, dev: Device) -> None:
                nonlocal frames_sent, bytes_sent
                frames_sent += 1

                # Count bytes sent (SessionRunner sends compressed H.264 data)
                if frame.color_frame and frame.color_frame.image:
                    # This will be the actual compressed data size
                    bytes_sent += len(frame.color_frame.image.planes[0].data)
                await self.client.save_ar_frames_async(
                    session_id=sess.id.value, 
                    ar_frames=[frame], 
                    device=dev
                    )
                

            # Create session runner to monitor the existing session
            print("   Monitoring existing session...")
            runner = SessionRunner(session, device, on_frame)
            runner.camera.set(cv2.CAP_PROP_FRAME_WIDTH, config.width)
            runner.camera.set(cv2.CAP_PROP_FRAME_HEIGHT, config.height)
            stopped = False

            # Start recording
            async def gather_frames_loop():
                while not stopped:
                    await runner.gather_camera_frame_async()
                    await asyncio.sleep(0)  # Yield to event loop

            gather_task = asyncio.create_task(gather_frames_loop())
            # Monitor performance
            end_time = start_time + config.duration
            sample_interval = 1.0  # Sample every second

            while time.time() < end_time:
                # Sample CPU and memory
                try:
                    cpu_percent = process.cpu_percent()
                    memory_mb = process.memory_info().rss / 1024 / 1024

                    cpu_usage_samples.append(cpu_percent)
                    memory_usage_samples.append(memory_mb)
                except Exception as e:
                    print(f"⚠️  Warning: Failed to sample performance: {e}")

                await asyncio.sleep(sample_interval)

            # Stop the runner
            stopped = True
            gather_task.cancel()

            # Calculate metrics
            actual_duration = time.time() - start_time
            fps_achieved = frames_sent / actual_duration if actual_duration > 0 else 0
            cpu_usage_avg = (
                sum(cpu_usage_samples) / len(cpu_usage_samples)
                if cpu_usage_samples
                else 0
            )
            memory_usage_mb = (
                max(memory_usage_samples) - initial_memory
                if memory_usage_samples
                else 0
            )
            bandwidth_mbps = (
                (bytes_sent * 8) / (actual_duration * 1_000_000)
                if actual_duration > 0
                else 0
            )

            print(f"✅ Results:")
            print(f"   • Frames received: {frames_sent}")
            print(f"   • Bytes received: {bytes_sent:,}")
            print(f"   • FPS achieved: {fps_achieved:.2f}")
            print(f"   • CPU usage: {cpu_usage_avg:.1f}%")
            print(f"   • Memory usage: {memory_usage_mb:.1f} MB")
            print(f"   • Bandwidth: {bandwidth_mbps:.2f} Mbps")

        except Exception as e:
            error_msg = f"Test failed: {e}"
            print(f"❌ {error_msg}")
            traceback.print_exc()
            errors.append(error_msg)

            # Set default values for failed test
            fps_achieved = 0.0
            cpu_usage_avg = 0.0
            memory_usage_mb = 0.0
            bandwidth_mbps = 0.0
        return TestResult(
            config=config,
            frames_sent=frames_sent,
            bytes_sent=bytes_sent,
            fps_achieved=fps_achieved,
            cpu_usage_avg=cpu_usage_avg,
            memory_usage_mb=memory_usage_mb,
            bandwidth_mbps=bandwidth_mbps,
            psnr=runner.summed_psnr / frames_sent if runner.summed_psnr != None else None,
            errors=errors,
            width=config.width,
            height=config.height,
        )

    async def run_all_tests(self) -> None:
        """Run all test configurations."""
        print("🚀 Real ARFlow Compression Evaluation")
        print("=" * 50)
        print(f"Server: {self.server_host}:{self.server_port}")
        print(f"Configurations: {len(self.configs)}")
        print("=" * 50)

        # Check if server is reachable
        print("🔍 Checking ARFlow server connection...")
        try:
            client = self.client
            # Try to list sessions to test connection
            response = await client.list_sessions_async()
            sessions = response.sessions  # type: ignore
            print(f"✅ Connected to ARFlow server ({len(sessions)} active sessions)")

        except Exception as e:
            print(f"❌ Cannot connect to ARFlow server: {e}")
            print("Please start the ARFlow server first:")
            print(f"  arflow view --port {self.server_port}")
            return

        # Run tests
        for i, config in enumerate(self.configs, 1):
            print(f"\n[{i}/{len(self.configs)}]")
            result = await self.run_test(config)
            self.results.append(result)

            # Brief pause between tests
            if i < len(self.configs):
                print("⏸️  Pausing between tests...")
                await asyncio.sleep(5)

        self.generate_reports()

    def generate_reports(self) -> None:
        """Generate detailed reports."""
        timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")

        # CSV Report
        csv_file = f"real_arflow_test_{timestamp}.csv"
        with open(csv_file, "w", newline="") as f:
            writer = csv.writer(f)
            writer.writerow(
                [
                    "Configuration",
                    "Duration (s)",
                    "Frames Received",
                    "Bytes Received",
                    "FPS Achieved",
                    "CPU Usage (%)",
                    "Memory Usage (MB)",
                    "Bandwidth (Mbps)",
                    "PSNR (dB)",
                    "Errors",
                ]
            )
            for r in self.results:
                writer.writerow(
                    [
                        r.config.name,
                        r.config.duration,
                        r.frames_sent,
                        r.bytes_sent,
                        f"{r.fps_achieved:.2f}",
                        f"{r.cpu_usage_avg:.1f}",
                        f"{r.memory_usage_mb:.1f}",
                        f"{r.bandwidth_mbps:.2f}",
                        f"{r.psnr:.2f}" if r.psnr != None else "N/A",
                        "; ".join(r.errors) if r.errors else "None",
                    ]
                )

        # JSON Report
        json_file = f"real_arflow_test_{timestamp}.json"
        with open(json_file, "w") as f:
            json.dump(
                {
                    "timestamp": timestamp,
                    "test_type": "real_arflow_compression",
                    "server": f"{self.server_host}:{self.server_port}",
                    "results": [
                        {
                            "config": {
                                "name": r.config.name,
                                "duration": r.config.duration,
                                "description": r.config.description,
                                "width": r.config.width,
                                "height": r.config.height,
                            },
                            "measurements": {
                                "frames_sent": r.frames_sent,
                                "bytes_sent": r.bytes_sent,
                                "fps_achieved": r.fps_achieved,
                                "cpu_usage_avg": r.cpu_usage_avg,
                                "memory_usage_mb": r.memory_usage_mb,
                                "bandwidth_mbps": r.bandwidth_mbps,
                                "psnr": r.psnr,
                                "errors": r.errors,
                            },
                        }
                        for r in self.results
                    ],
                },
                f,
                indent=2,
            )

        print(f"\n📊 Real ARFlow Test Complete!")
        print(f"📄 Reports: {csv_file}, {json_file}")

        self.print_analysis()

    def print_analysis(self) -> None:
        """Print analysis of the results."""
        print(f"\n📈 Real ARFlow Performance Analysis:")
        print("-" * 60)

        if self.results and not self.results[0].errors:
            result = self.results[0]

            print(f"\nARFlow Streaming Performance:")
            print(f"  • Actual FPS: {result.fps_achieved:.2f}")
            print(f"  • Bandwidth usage: {result.bandwidth_mbps:.2f} Mbps")
            print(f"  • CPU usage: {result.cpu_usage_avg:.1f}%")
            print(f"  • Memory usage: {result.memory_usage_mb:.1f} MB")
            print(f"  • Frames processed: {result.frames_sent}")
            print(
                f"  • Average bytes per frame: {result.bytes_sent // result.frames_sent if result.frames_sent > 0 else 0:,}"
            )

            # Estimate uncompressed equivalent
            estimated_width, estimated_height = 640, 480
            uncompressed_bytes_per_frame = estimated_width * estimated_height * 3  # RGB
            total_uncompressed_bytes = uncompressed_bytes_per_frame * result.frames_sent
            compression_ratio = (
                total_uncompressed_bytes / result.bytes_sent
                if result.bytes_sent > 0
                else 0
            )

            print(f"\nCompression Efficiency:")
            print(f"  • Estimated compression ratio: {compression_ratio:.1f}:1")
            print(
                f"  • Bandwidth savings: {((total_uncompressed_bytes - result.bytes_sent) / total_uncompressed_bytes) * 100:.1f}%"
            )
            print(
                f"  • Uncompressed equivalent: {(total_uncompressed_bytes * 8) / (result.config.duration * 1_000_000):.2f} Mbps"
            )
        else:
            print("\n⚠️  Could not analyze results due to test failures")


async def main() -> None:
    """Main entry point."""
    parser = argparse.ArgumentParser(
        description="Test real ARFlow compression performance"
    )
    parser.add_argument(
        "--duration",
        type=int,
        default=30,
        help="Test duration in seconds (default: 30)",
    )
    parser.add_argument(
        "--host",
        type=str,
        default="localhost",
        help="ARFlow server host (default: localhost)",
    )
    parser.add_argument(
        "--port",
        type=int,
        default=8500,
        help="ARFlow server port (default: 8500)",
    )

    args = parser.parse_args()

    # Check dependencies
    try:
        import cv2  # type: ignore

        print("✅ OpenCV available")
    except ImportError:
        print("❌ OpenCV not found. Install with: pip install opencv-python")
        print("This is required for camera capture in SessionRunner")
        sys.exit(1)

    try:
        import psutil  # type: ignore

        print("✅ psutil available")
    except ImportError:
        print("❌ psutil not found. Install with: pip install psutil")
        print("This is required for performance monitoring")
        sys.exit(1)

    # Update test durations
    tester = RealARFlowTester(args.host, args.port)
    for config in tester.configs:
        config.duration = args.duration

    await tester.run_all_tests()


if __name__ == "__main__":
    asyncio.run(main())