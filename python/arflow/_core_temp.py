#         if client_info.config.camera_color.enabled:
#             try:
#                 color_rgb = np.flipud(
#                     decode_rgb_image(
#                         client_info.config.camera_intrinsics.resolution_y,
#                         client_info.config.camera_intrinsics.resolution_x,
#                         client_info.config.camera_color.resize_factor_y,
#                         client_info.config.camera_color.resize_factor_x,
#                         cast(ColorDataType, client_info.config.camera_color.data_type),
#                         request.color,
#                     )
#                 )
#             except ValueError as e:
#                 raise InvalidArgument(str(e))
#
#             rr.log(
#                 f"arflow/{request.uid}/rgb",
#                 rr.Image(color_rgb),
#                 recording=client_info.rerun_stream,
#             )
#
#         if client_info.config.camera_depth.enabled:
#             try:
#                 depth_img = np.flipud(
#                     decode_depth_image(
#                         client_info.config.camera_depth.resolution_y,
#                         client_info.config.camera_depth.resolution_x,
#                         cast(DepthDataType, client_info.config.camera_depth.data_type),
#                         request.depth,
#                     )
#                 )
#             except ValueError as e:
#                 raise InvalidArgument(str(e))
#             rr.log(
#                 f"arflow/{request.uid}/depth",
#                 rr.DepthImage(depth_img, meter=1.0),
#                 recording=client_info.rerun_stream,
#             )
#
#         if client_info.config.camera_transform.enabled:
#             rr.log(
#                 f"arflow/{request.uid}/world/origin",
#                 rr.ViewCoordinates.RIGHT_HAND_Y_DOWN,
#                 recording=client_info.rerun_stream,
#             )
#             # self.logger.log(
#             #     "world/xyz",
#             #     rr.Arrows3D(
#             #         vectors=[[1, 0, 0], [0, 1, 0], [0, 0, 1]],
#             #         colors=[[255, 0, 0], [0, 255, 0], [0, 0, 255]],
#             #     ),
#             # )
#
#             try:
#                 transform = decode_transform(request.transform)
#             except ValueError as e:
#                 raise InvalidArgument(str(e))
#             rr.log(
#                 f"arflow/{request.uid}/world/camera",
#                 rr.Transform3D(mat3x3=transform[:3, :3], translation=transform[:3, 3]),
#                 recording=client_info.rerun_stream,
#             )
#
#             # Won't thow any potential exceptions for now.
#             k = decode_intrinsic(
#                 client_info.config.camera_color.resize_factor_y,
#                 client_info.config.camera_color.resize_factor_x,
#                 client_info.config.camera_intrinsics.focal_length_y,
#                 client_info.config.camera_intrinsics.focal_length_x,
#                 client_info.config.camera_intrinsics.principal_point_y,
#                 client_info.config.camera_intrinsics.principal_point_x,
#             )
#
#             rr.log(
#                 f"arflow/{request.uid}/world/camera",
#                 rr.Pinhole(image_from_camera=k),
#                 recording=client_info.rerun_stream,
#             )
#             if color_rgb is not None:
#                 rr.log(
#                     f"arflow/{request.uid}/world/camera",
#                     rr.Image(np.flipud(color_rgb)),
#                     recording=client_info.rerun_stream,
#                 )
#
#         if client_info.config.camera_point_cloud.enabled:
#             if (
#                 k is not None
#                 and color_rgb is not None
#                 and depth_img is not None
#                 and transform is not None
#             ):
#                 # Won't thow any potential exceptions for now.
#                 point_cloud_pcd, point_cloud_clr = decode_point_cloud(
#                     client_info.config.camera_intrinsics.resolution_y,
#                     client_info.config.camera_intrinsics.resolution_x,
#                     client_info.config.camera_color.resize_factor_y,
#                     client_info.config.camera_color.resize_factor_x,
#                     k,
#                     color_rgb,
#                     depth_img,
#                     transform,
#                 )
#                 rr.log(
#                     f"arflow/{request.uid}/world/point_cloud",
#                     rr.Points3D(point_cloud_pcd, colors=point_cloud_clr),
#                     recording=client_info.rerun_stream,
#                 )
#
#         if client_info.config.camera_plane_detection.enabled:
#             strips: List[PlaneBoundaryPoints3D] = []
#             for plane in request.plane_detection:
#                 boundary_points_2d: List[List[float]] = list(
#                     map(lambda pt: [pt.x, pt.y], plane.boundary_points)
#                 )
#
#                 plane = PlaneInfo(
#                     center=np.array([plane.center.x, plane.center.y, plane.center.z]),
#                     normal=np.array([plane.normal.x, plane.normal.y, plane.normal.z]),
#                     size=np.array([plane.size.x, plane.size.y]),
#                     boundary_points=np.array(boundary_points_2d),
#                 )
#
#                 try:
#                     boundary_3d = convert_2d_to_3d_boundary_points(
#                         plane.boundary_points, plane.normal, plane.center
#                     )
#                 except ValueError as e:
#                     raise InvalidArgument(str(e))
#
#                 # Close the boundary by adding the first point to the end.
#                 if boundary_3d.shape[0] > 0:
#                     boundary_3d = np.vstack([boundary_3d, boundary_3d[0]])
#                 strips.append(boundary_3d)
#             rr.log(
#                 f"arflow/{request.uid}/world/detected-planes",
#                 rr.LineStrips3D(
#                     strips=strips,
#                     colors=[[255, 0, 0]],
#                     radii=rr.Radius.ui_points(5.0),
#                 ),
#                 recording=client_info.rerun_stream,
#             )
#
#         if client_info.config.gyroscope.enabled:
#             gyro_data_proto = request.gyroscope
#             gyro_data = GyroscopeInfo(
#                 attitude=np.array(
#                     [
#                         gyro_data_proto.attitude.x,
#                         gyro_data_proto.attitude.y,
#                         gyro_data_proto.attitude.z,
#                         gyro_data_proto.attitude.w,
#                     ]
#                 ),
#                 rotation_rate=np.array(
#                     [
#                         gyro_data_proto.rotation_rate.x,
#                         gyro_data_proto.rotation_rate.y,
#                         gyro_data_proto.rotation_rate.z,
#                     ]
#                 ),
#                 gravity=np.array(
#                     [
#                         gyro_data_proto.gravity.x,
#                         gyro_data_proto.gravity.y,
#                         gyro_data_proto.gravity.z,
#                     ]
#                 ),
#                 acceleration=np.array(
#                     [
#                         gyro_data_proto.acceleration.x,
#                         gyro_data_proto.acceleration.y,
#                         gyro_data_proto.acceleration.z,
#                     ]
#                 ),
#             )
#             attitude = rr.Quaternion(
#                 xyzw=gyro_data.attitude,
#             )
#             rotation_rate = rr.datatypes.Vec3D(gyro_data.rotation_rate)
#             gravity = rr.datatypes.Vec3D(gyro_data.gravity)
#             acceleration = rr.datatypes.Vec3D(gyro_data.acceleration)
#             # Attitute is displayed as a box, and the other acceleration variables are displayed as arrows.
#             rr.log(
#                 f"arflow/{request.uid}/world/rotations/gyroscope/attitude",
#                 rr.Boxes3D(half_sizes=[0.5, 0.5, 0.5], quaternions=[attitude]),
#                 recording=client_info.rerun_stream,
#             )
#             rr.log(
#                 f"arflow/{request.uid}/world/rotations/gyroscope/rotation_rate",
#                 rr.Arrows3D(vectors=[rotation_rate], colors=[[0, 255, 0]]),
#                 recording=client_info.rerun_stream,
#             )
#             rr.log(
#                 f"arflow/{request.uid}/world/rotations/gyroscope/gravity",
#                 rr.Arrows3D(vectors=[gravity], colors=[[0, 0, 255]]),
#                 recording=client_info.rerun_stream,
#             )
#             rr.log(
#                 f"arflow/{request.uid}/world/rotations/gyroscope/acceleration",
#                 rr.Arrows3D(vectors=[acceleration], colors=[[255, 255, 0]]),
#                 recording=client_info.rerun_stream,
#             )
#
#         if client_info.config.audio.enabled:
#             audio_data = np.array(request.audio_data)
#             for i in audio_data:
#                 rr.log(
#                     f"arflow/{request.uid}/world/audio",
#                     rr.Scalar(i),
#                     recording=client_info.rerun_stream,
#                 )
#
#         if client_info.config.meshing.enabled:
#             logger.debug("Number of meshes: %s", len(request.meshes))
#             # Binary arrays can be empty if no mesh is sent. This could be due to non-supporting devices. We can log this in the future.
#             binary_arrays = request.meshes
#             for index, mesh_data in enumerate(binary_arrays):
#                 # We are ignoring type because DracoPy is written with Cython, and Pyright cannot infer types from a native module.
#                 dracoMesh = DracoPy.decode(mesh_data.data)  # pyright: ignore [reportUnknownMemberType, reportUnknownVariableType]
#
#                 mesh = Mesh(
#                     faces=dracoMesh.faces,  # pyright: ignore [reportUnknownMemberType, reportUnknownArgumentType]
#                     points=dracoMesh.points,  # pyright: ignore [reportUnknownMemberType, reportUnknownArgumentType]
#                     normals=dracoMesh.normals,  # pyright: ignore [reportUnknownMemberType, reportUnknownArgumentType]
#                     tex_coord=dracoMesh.tex_coord,  # pyright: ignore [reportUnknownMemberType, reportUnknownArgumentType]
#                     colors=dracoMesh.colors,  # pyright: ignore [reportUnknownMemberType, reportUnknownArgumentType]
#                 )
#
#                 rr.log(
#                     f"arflow/{request.uid}/world/mesh/mesh-{index}",
#                     rr.Mesh3D(
#                         vertex_positions=mesh.points,
#                         triangle_indices=mesh.faces,
#                         vertex_normals=mesh.normals,
#                         vertex_colors=mesh.colors,
#                         vertex_texcoords=mesh.tex_coord,
#                     ),
#                     recording=client_info.rerun_stream,
#                 )
#
#         # Call the for user extension code.
#         self.on_frame_received(
#             DecodedDataFrame(
#                 color_rgb=color_rgb,
#                 depth_img=depth_img,
#                 transform=transform,
#                 intrinsic=k,
#                 point_cloud_pcd=point_cloud_pcd,
#                 point_cloud_clr=point_cloud_clr,
#             )
#         )
#
#         return ProcessFrameResponse(message="OK")
