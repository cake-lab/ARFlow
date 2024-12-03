"""Session helps participating devices stream to the same Rerun recording."""

import DracoPy
import numpy as np
import numpy.typing as npt
import rerun as rr

from arflow._types import (
    ARFrameType,
    DecodedColorFrames,
    DecodedDepthFrames,
    DecodedTransformFrames,
)
from cakelab.arflow_grpc.v1.ar_trackable_pb2 import ARTrackable
from cakelab.arflow_grpc.v1.audio_frame_pb2 import AudioFrame
from cakelab.arflow_grpc.v1.device_pb2 import Device
from cakelab.arflow_grpc.v1.gyroscope_frame_pb2 import GyroscopeFrame
from cakelab.arflow_grpc.v1.mesh_detection_frame_pb2 import MeshDetectionFrame
from cakelab.arflow_grpc.v1.plane_detection_frame_pb2 import PlaneDetectionFrame
from cakelab.arflow_grpc.v1.point_cloud_detection_frame_pb2 import (
    PointCloudDetectionFrame,
)
from cakelab.arflow_grpc.v1.session_pb2 import Session
from cakelab.arflow_grpc.v1.timeline_pb2 import Timeline
from cakelab.arflow_grpc.v1.xr_cpu_image_pb2 import XRCpuImage


class SessionStream:
    """All devices in a session share a stream."""

    def __init__(self, info: Session, stream: rr.RecordingStream):
        self.info = info
        """Session information."""
        self.stream = stream
        """Stream handle to the Rerun recording associated with this session."""

    def save_transform_frames(
        self,
        frames: DecodedTransformFrames,
        device: Device,
        device_timestamps: list[float],
    ):
        if len(frames) == 0 or len(frames) != len(device_timestamps):
            raise ValueError(
                "The decoded transform frames and device timestamps must have the same length"
            )

        entity_path = rr.new_entity_path(
            [
                self.info.id.value,
                device.uid,
                ARFrameType.TRANSFORM_FRAME,
            ]
        )
        rr.log(
            entity_path,
            [rr.Transform3D.indicator()],
            static=True,
            recording=self.stream,
        )
        rr.send_columns(
            entity_path,
            times=[
                rr.TimeSecondsColumn(
                    timeline=Timeline.Name(Timeline.TIMELINE_DEVICE_TIMESTAMP),
                    times=device_timestamps,
                ),
            ],
            components=[
                rr.components.TransformMat3x3Batch(
                    data=frames.reshape(len(device_timestamps), -1)
                )
            ],
            recording=self.stream,
        )

    def save_color_frames(
        self,
        frames: DecodedColorFrames,
        device: Device,
        format: XRCpuImage.Format,
        width: int,
        height: int,
        device_timestamps: list[float],
        image_timestamps: list[float],
    ):
        """Save color frames to the stream. Assumes that the device is in the session and the data is "nice" (e.g., all frames have the same format, width, height, and originating device).

        Raises:
            ValueError: If the decoded color frames, device timestamps, and image timestamps do not have the same length or if the frame format is not supported.

        @private
        """
        if (
            len(frames) == 0
            or len(frames) != len(image_timestamps)
            or len(frames) != len(device_timestamps)
        ):
            raise ValueError(
                "The decoded color frames, device timestamps, and image timestamps must have the same length"
            )

        entity_path = rr.new_entity_path(
            [
                self.info.id.value,
                device.uid,
                ARFrameType.COLOR_FRAME,
                f"{width}x{height}",
            ]
        )

        if format == XRCpuImage.FORMAT_ANDROID_YUV_420_888:
            format_static = rr.components.ImageFormat(
                width=width,
                height=height,
                pixel_format=rr.PixelFormat.Y_U_V12_LimitedRange,
            )
        elif format == XRCpuImage.FORMAT_IOS_YP_CBCR_420_8BI_PLANAR_FULL_RANGE:
            format_static = rr.components.ImageFormat(
                width=width,
                height=height,
                pixel_format=rr.PixelFormat.NV12,
            )
        else:
            raise ValueError(f"Unsupported frame format: {format}")

        rr.log(
            entity_path,
            [format_static, rr.Image.indicator()],
            static=True,
            recording=self.stream,
        )
        # TODO: What is the behavior here when using view mode but for multiple RecordingStreams?
        rr.send_columns(
            entity_path,
            times=[
                rr.TimeSecondsColumn(
                    timeline=Timeline.Name(Timeline.TIMELINE_DEVICE_TIMESTAMP),
                    times=device_timestamps,
                ),
                rr.TimeSecondsColumn(
                    timeline=Timeline.Name(Timeline.TIMELINE_IMAGE_TIMESTAMP),
                    times=image_timestamps,
                ),
            ],
            components=[
                rr.components.ImageBufferBatch(
                    data=frames.reshape(len(image_timestamps), -1)
                )
            ],
            recording=self.stream,
        )

    def save_depth_frames(
        self,
        frames: DecodedDepthFrames,
        device: Device,
        format: XRCpuImage.Format,
        width: int,
        height: int,
        device_timestamps: list[float],
        image_timestamps: list[float],
    ):
        """Save depth frames to the stream. Assumes that the device is in the session and the data is "nice" (e.g., all frames have the same format, width, height, and originating device).

        Raises:
            ValueError: If the decoded depth frames, device timestamps, and image timestamps do not have the same length or if the frame format is not supported.

        @private
        """
        if (
            len(frames) == 0
            or len(frames) != len(image_timestamps)
            or len(frames) != len(device_timestamps)
        ):
            raise ValueError(
                "The decoded depth frames, device timestamps, and image timestamps must have the same length"
            )

        entity_path = rr.new_entity_path(
            [
                self.info.id.value,
                device.uid,
                ARFrameType.DEPTH_FRAME,
                f"{width}x{height}",
            ]
        )

        if format == XRCpuImage.FORMAT_DEPTHFLOAT32:
            format_static = rr.components.ImageFormat(
                width=width,
                height=height,
                color_model=rr.ColorModel.L,
                channel_datatype=rr.ChannelDatatype.F32,
            )
        elif format == XRCpuImage.FORMAT_DEPTHUINT16:
            format_static = rr.components.ImageFormat(
                width=width,
                height=height,
                color_model=rr.ColorModel.L,
                channel_datatype=rr.ChannelDatatype.U16,
            )
        else:
            raise ValueError(f"Unsupported frame format: {format}")

        rr.log(
            entity_path,
            [format_static, rr.DepthImage.indicator()],
            static=True,
            recording=self.stream,
        )
        rr.send_columns(
            entity_path,
            times=[
                rr.TimeSecondsColumn(
                    timeline=Timeline.Name(Timeline.TIMELINE_DEVICE_TIMESTAMP),
                    times=device_timestamps,
                ),
                rr.TimeSecondsColumn(
                    timeline=Timeline.Name(Timeline.TIMELINE_IMAGE_TIMESTAMP),
                    times=image_timestamps,
                ),
            ],
            components=[
                rr.components.ImageBufferBatch(
                    data=frames.reshape(len(image_timestamps), -1)
                ),
                # rr.components.DepthMeterBatch()
            ],
            recording=self.stream,
        )

    def save_gyroscope_frames(
        self,
        frames: list[GyroscopeFrame],
        device: Device,
    ):
        if len(frames) == 0:
            return

        entity_path = rr.new_entity_path(
            [
                self.info.id.value,
                device.uid,
                ARFrameType.GYROSCOPE_FRAME,
            ]
        )
        device_timestamps = [
            f.device_timestamp.seconds + f.device_timestamp.nanos / 1e9 for f in frames
        ]
        attitude_entity_path = rr.new_entity_path([entity_path, "attitude"])
        rr.log(
            attitude_entity_path,
            [rr.Boxes3D.indicator()],
            static=True,
            recording=self.stream,
        )
        rr.send_columns(
            attitude_entity_path,
            times=[
                rr.TimeSecondsColumn(
                    timeline=Timeline.Name(Timeline.TIMELINE_DEVICE_TIMESTAMP),
                    times=device_timestamps,
                ),
            ],
            components=[
                rr.components.RotationQuatBatch(
                    data=[
                        [
                            frame.attitude.x,
                            frame.attitude.y,
                            frame.attitude.z,
                            frame.attitude.w,
                        ]
                        for frame in frames
                    ]
                ),
                rr.components.HalfSize3DBatch(data=[[0.5, 0.5, 0.5] for _ in frames]),
            ],
            recording=self.stream,
        )
        rotation_rate_entity_path = rr.new_entity_path([entity_path, "rotation_rate"])
        rr.log(
            rotation_rate_entity_path,
            [rr.Arrows3D.indicator()],
            static=True,
            recording=self.stream,
        )
        rr.send_columns(
            rotation_rate_entity_path,
            times=[
                rr.TimeSecondsColumn(
                    timeline=Timeline.Name(Timeline.TIMELINE_DEVICE_TIMESTAMP),
                    times=device_timestamps,
                ),
            ],
            components=[
                rr.components.Vector3DBatch(
                    data=[
                        [
                            frame.rotation_rate.x,
                            frame.rotation_rate.y,
                            frame.rotation_rate.z,
                        ]
                        for frame in frames
                    ]
                ),
                rr.components.ColorBatch(
                    data=[[0, 255, 0] for _ in frames],
                ),
            ],
            recording=self.stream,
        )
        gravity_entity_path = rr.new_entity_path([entity_path, "gravity"])
        rr.log(
            gravity_entity_path,
            [rr.Arrows3D.indicator()],
            static=True,
            recording=self.stream,
        )
        rr.send_columns(
            gravity_entity_path,
            times=[
                rr.TimeSecondsColumn(
                    timeline=Timeline.Name(Timeline.TIMELINE_DEVICE_TIMESTAMP),
                    times=device_timestamps,
                ),
            ],
            components=[
                rr.components.Vector3DBatch(
                    data=[
                        [
                            frame.gravity.x,
                            frame.gravity.y,
                            frame.gravity.z,
                        ]
                        for frame in frames
                    ]
                ),
                rr.components.ColorBatch(
                    data=[[0, 0, 255] for _ in frames],
                ),
            ],
            recording=self.stream,
        )
        acceleration_entity_path = rr.new_entity_path([entity_path, "acceleration"])
        rr.log(
            acceleration_entity_path,
            [rr.Arrows3D.indicator()],
            static=True,
            recording=self.stream,
        )
        rr.send_columns(
            acceleration_entity_path,
            times=[
                rr.TimeSecondsColumn(
                    timeline=Timeline.Name(Timeline.TIMELINE_DEVICE_TIMESTAMP),
                    times=device_timestamps,
                ),
            ],
            components=[
                rr.components.Vector3DBatch(
                    data=[
                        [
                            frame.acceleration.x,
                            frame.acceleration.y,
                            frame.acceleration.z,
                        ]
                        for frame in frames
                    ]
                ),
                rr.components.ColorBatch(
                    data=[[255, 255, 0] for _ in frames],
                ),
            ],
            recording=self.stream,
        )

    def save_audio_frames(
        self,
        frames: list[AudioFrame],
        device: Device,
    ):
        if len(frames) == 0:
            return

        entity_path = rr.new_entity_path(
            [
                self.info.id.value,
                device.uid,
                ARFrameType.AUDIO_FRAME,
            ]
        )
        rr.log(
            entity_path,
            [rr.Scalar.indicator()],
            static=True,
            recording=self.stream,
        )
        rr.send_columns(
            entity_path,
            times=[
                rr.TimeSecondsColumn(
                    timeline=Timeline.Name(Timeline.TIMELINE_DEVICE_TIMESTAMP),
                    times=[
                        f.device_timestamp.seconds + f.device_timestamp.nanos / 1e9
                        for f in frames
                    ],
                ),
            ],
            components=[
                rr.components.ScalarBatch(data=[frame.data for frame in frames])
            ],
            recording=self.stream,
        )

    def save_plane_detection_frames(
        self,
        frames: list[PlaneDetectionFrame],
        device: Device,
    ):
        if len(frames) == 0:
            return

        entity_path = rr.new_entity_path(
            [
                self.info.id.value,
                device.uid,
                ARFrameType.PLANE_DETECTION_FRAME,
            ]
        )
        rr.log(
            entity_path,
            [rr.LineStrips3D.indicator()],
            static=True,
            recording=self.stream,
        )
        positively_changed_frames = filter(
            lambda f: f.state == PlaneDetectionFrame.STATE_ADDED
            or f.state == PlaneDetectionFrame.STATE_UPDATED,
            frames,
        )
        rr.send_columns(
            entity_path,
            times=[
                rr.TimeSecondsColumn(
                    timeline=Timeline.Name(Timeline.TIMELINE_DEVICE_TIMESTAMP),
                    times=[
                        f.device_timestamp.seconds + f.device_timestamp.nanos / 1e9
                        for f in positively_changed_frames
                    ],
                ),
            ],
            components=[
                rr.components.EntityPathBatch(
                    data=[
                        rr.new_entity_path(
                            [
                                f.plane.trackable.trackable_id.sub_id_1,
                                f.plane.trackable.trackable_id.sub_id_2,
                            ]
                        )
                        for f in positively_changed_frames
                    ]
                ),
                # TODO: notice ARTrackable.Pose
                rr.components.LineStrip3DBatch(
                    data=[_to_boundary_points_3d(f) for f in positively_changed_frames]
                ),
                rr.components.ColorBatch(
                    data=[
                        [0, 255, 0]  # green
                        if f.plane.trackable.tracking_state
                        == ARTrackable.TRACKING_STATE_TRACKING
                        else [255, 0, 0]  # red
                        for f in positively_changed_frames
                    ],
                ),
                rr.components.TextBatch(
                    data=[
                        str(f.plane.trackable.tracking_state)
                        for f in positively_changed_frames
                    ]
                ),
            ],
            recording=self.stream,
        )
        negatively_changed_frames = filter(
            lambda f: f.state == PlaneDetectionFrame.STATE_REMOVED, frames
        )
        rr.send_columns(
            entity_path,
            times=[
                rr.TimeSecondsColumn(
                    timeline=Timeline.Name(Timeline.TIMELINE_DEVICE_TIMESTAMP),
                    times=[
                        f.device_timestamp.seconds + f.device_timestamp.nanos / 1e9
                        for f in negatively_changed_frames
                    ],
                ),
            ],
            components=[
                rr.components.EntityPathBatch(
                    data=[
                        rr.new_entity_path(
                            [
                                f.plane.trackable.trackable_id.sub_id_1,
                                f.plane.trackable.trackable_id.sub_id_2,
                            ]
                        )
                        for f in negatively_changed_frames
                    ]
                ),
                rr.components.ClearIsRecursiveBatch(
                    data=[True for _ in negatively_changed_frames]
                ),
            ],
            recording=self.stream,
        )

    def save_point_cloud_detection_frames(
        self,
        frames: list[PointCloudDetectionFrame],
        device: Device,
    ):
        if len(frames) == 0:
            return

        entity_path = rr.new_entity_path(
            [
                self.info.id.value,
                device.uid,
                ARFrameType.POINT_CLOUD_DETECTION_FRAME,
            ]
        )
        rr.log(
            entity_path,
            [rr.Points3D.indicator()],
            static=True,
            recording=self.stream,
        )
        positively_changed_frames = filter(
            lambda f: f.state == PointCloudDetectionFrame.STATE_ADDED
            or f.state == PointCloudDetectionFrame.STATE_UPDATED,
            frames,
        )
        # for each point cloud
        rr.send_columns(
            entity_path,
            times=[
                rr.TimeSecondsColumn(
                    timeline=Timeline.Name(Timeline.TIMELINE_DEVICE_TIMESTAMP),
                    times=[
                        f.device_timestamp.seconds + f.device_timestamp.nanos / 1e9
                        for f in positively_changed_frames
                    ],
                ),
            ],
            components=[
                rr.components.EntityPathBatch(
                    data=[
                        rr.new_entity_path(
                            [
                                f.point_cloud.trackable.trackable_id.sub_id_1,
                                f.point_cloud.trackable.trackable_id.sub_id_2,
                            ]
                        )
                        for f in positively_changed_frames
                    ]
                ),
                # TODO: notice ARTrackable.Pose
                rr.components.ColorBatch(
                    data=[
                        [
                            [0, 255, 0]  # green
                            if f.point_cloud.trackable.tracking_state
                            == ARTrackable.TRACKING_STATE_TRACKING
                            else [255, 0, 0]  # red
                            for f in positively_changed_frames
                        ]
                    ],
                ),
                rr.components.TextBatch(
                    data=[
                        [
                            str(f.point_cloud.trackable.tracking_state)
                            for f in positively_changed_frames
                        ],
                    ]
                ),
            ],
            recording=self.stream,
        )
        # for each point in the cloud
        rr.send_columns(
            entity_path,
            times=[],
            components=[
                rr.components.EntityPathBatch(
                    data=[
                        rr.new_entity_path(
                            [
                                f.point_cloud.trackable.trackable_id.sub_id_1,
                                f.point_cloud.trackable.trackable_id.sub_id_2,
                                i,
                            ]
                        )
                        for f in positively_changed_frames
                        for i in f.point_cloud.identifiers
                    ]
                ),
                rr.components.Position3DBatch(
                    data=[
                        [
                            p.x,
                            p.y,
                            p.z,
                        ]
                        for f in positively_changed_frames
                        for p in f.point_cloud.positions
                    ]
                ),
                rr.components.TextBatch(
                    data=[
                        f.point_cloud.confidence_values
                        for f in positively_changed_frames
                    ],
                ),
            ],
            recording=self.stream,
        )
        negatively_changed_frames = filter(
            lambda f: f.state == PointCloudDetectionFrame.STATE_REMOVED, frames
        )
        rr.send_columns(
            entity_path,
            times=[
                rr.TimeSecondsColumn(
                    timeline=Timeline.Name(Timeline.TIMELINE_DEVICE_TIMESTAMP),
                    times=[
                        f.device_timestamp.seconds + f.device_timestamp.nanos / 1e9
                        for f in negatively_changed_frames
                    ],
                ),
            ],
            components=[
                rr.components.EntityPathBatch(
                    data=[
                        rr.new_entity_path(
                            [
                                f.point_cloud.trackable.trackable_id.sub_id_1,
                                f.point_cloud.trackable.trackable_id.sub_id_2,
                            ]
                        )
                        for f in negatively_changed_frames
                    ]
                ),
                rr.components.ClearIsRecursiveBatch(
                    data=[True for _ in negatively_changed_frames]
                ),
            ],
            recording=self.stream,
        )

    def save_mesh_detection_frames(
        self,
        frames: list[MeshDetectionFrame],
        device: Device,
    ):
        if len(frames) == 0:
            return

        entity_path = rr.new_entity_path(
            [
                self.info.id.value,
                device.uid,
                ARFrameType.MESH_DETECTION_FRAME,
            ]
        )
        rr.log(
            entity_path,
            [rr.Mesh3D.indicator()],
            static=True,
            recording=self.stream,
        )
        positively_changed_frames = filter(
            lambda f: f.state == MeshDetectionFrame.STATE_ADDED
            or f.state == MeshDetectionFrame.STATE_UPDATED,
            frames,
        )
        for f in positively_changed_frames:
            rr.set_time_seconds(
                Timeline.Name(Timeline.TIMELINE_DEVICE_TIMESTAMP),
                seconds=f.device_timestamp.seconds + f.device_timestamp.nanos / 1e9,
                recording=self.stream,
            )
            for sub_mesh in f.mesh_filter.mesh.sub_meshes:
                # We are ignoring type because DracoPy is written with Cython, and Pyright cannot infer types from a native module.
                decoded_mesh = DracoPy.decode(sub_mesh.data)  # pyright: ignore [reportUnknownMemberType, reportUnknownVariableType]
                rr.log(
                    rr.new_entity_path([entity_path, f.mesh_filter.instance_id]),
                    rr.Mesh3D(
                        vertex_positions=decoded_mesh.points,  # pyright: ignore [reportUnknownMemberType, reportUnknownArgumentType]
                        triangle_indices=decoded_mesh.faces,  # pyright: ignore [reportUnknownMemberType, reportUnknownArgumentType]
                        vertex_normals=decoded_mesh.normals,  # pyright: ignore [reportUnknownMemberType, reportUnknownArgumentType]
                        vertex_colors=decoded_mesh.colors,  # pyright: ignore [reportUnknownMemberType, reportUnknownArgumentType]
                        vertex_texcoords=decoded_mesh.tex_coord,  # pyright: ignore [reportUnknownMemberType, reportUnknownArgumentType]
                    ),
                    recording=self.stream,
                )
        negatively_changed_frames = filter(
            lambda f: f.state == PointCloudDetectionFrame.STATE_REMOVED, frames
        )
        rr.send_columns(
            entity_path,
            times=[
                rr.TimeSecondsColumn(
                    timeline=Timeline.Name(Timeline.TIMELINE_DEVICE_TIMESTAMP),
                    times=[
                        f.device_timestamp.seconds for f in negatively_changed_frames
                    ],
                ),
            ],
            components=[
                rr.components.EntityPathBatch(
                    data=[
                        rr.new_entity_path([f.mesh_filter.instance_id])
                        for f in negatively_changed_frames
                    ]
                ),
                rr.components.ClearIsRecursiveBatch(
                    data=[True for _ in negatively_changed_frames]
                ),
            ],
            recording=self.stream,
        )


# TODO: Happy path programming. Please add error handling
def _to_boundary_points_3d(
    plane: PlaneDetectionFrame,
) -> npt.NDArray[np.float32]:
    normal_as_np = np.array(
        [plane.plane.normal.x, plane.plane.normal.y, plane.plane.normal.z]
    )
    normalized_normal_as_np = normal_as_np / np.linalg.norm(normal_as_np)
    arbitary_vector = (
        np.array([1, 0, 0])
        if not np.allclose(normalized_normal_as_np, [1, 0, 0])
        else np.array([0, 1, 0])
    )
    u = np.cross(normalized_normal_as_np, arbitary_vector)
    u = u / np.linalg.norm(u)
    v = np.cross(normalized_normal_as_np, u)
    center_as_np = np.array(
        [plane.plane.center.x, plane.plane.center.y, plane.plane.center.z]
    )
    boundary_points_3d = np.array(
        [
            center_as_np + point_2d.x * u + point_2d.y * v
            for point_2d in plane.plane.boundary
        ],
        dtype=np.float32,
    )
    return boundary_points_3d
