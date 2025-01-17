using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Draco.Encode;
using Google.Protobuf.WellKnownTypes;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace CakeLab.ARFlow.DataBuffers
{
    using Clock;
    using Grpc.V1;
    using Utilities;
    using GrpcMeshFilter = Grpc.V1.MeshFilter;
    using UnityMeshFilter = UnityEngine.MeshFilter;

    public enum MeshDetectionState
    {
        Unspecified,
        Added,
        Updated,
        Removed,
    }

    public struct RawMeshDetectionFrame
    {
        public int MeshFilterId;
        public MeshDetectionState State;
        public DateTime DeviceTimestamp;
        public List<byte[]> EncodedSubMeshes;

        public static explicit operator Grpc.V1.MeshDetectionFrame(RawMeshDetectionFrame rawFrame)
        {
            var subMeshes = rawFrame.EncodedSubMeshes.Select(
                encodedMesh => new Grpc.V1.MeshFilter.Types.EncodedMesh.Types.EncodedSubMesh
                {
                    Data = Google.Protobuf.ByteString.CopyFrom(encodedMesh),
                }
            );
            var encodedMesh = new Grpc.V1.MeshFilter.Types.EncodedMesh();
            encodedMesh.SubMeshes.AddRange(subMeshes);
            var meshFilterGrpc = new GrpcMeshFilter
            {
                InstanceId = rawFrame.MeshFilterId,
                Mesh = encodedMesh,
            };
            var meshDetectionFrameGrpc = new Grpc.V1.MeshDetectionFrame
            {
                State = (MeshDetectionFrame.Types.State)rawFrame.State,
                DeviceTimestamp = Timestamp.FromDateTime(rawFrame.DeviceTimestamp),
                MeshFilter = meshFilterGrpc,
            };
            return meshDetectionFrameGrpc;
        }

        public static explicit operator Grpc.V1.ARFrame(RawMeshDetectionFrame rawFrame)
        {
            var arFrame = new Grpc.V1.ARFrame
            {
                MeshDetectionFrame = (Grpc.V1.MeshDetectionFrame)rawFrame,
            };
            return arFrame;
        }
    }

    public class MeshDetectionBuffer : IARFrameBuffer<RawMeshDetectionFrame>
    {
        ARMeshManager m_MeshManager;

        public ARMeshManager MeshManager
        {
            get => m_MeshManager;
            set => m_MeshManager = value;
        }

        IClock m_Clock;

        public IClock Clock
        {
            get => m_Clock;
            set => m_Clock = value;
        }

        private ConcurrentQueue<RawMeshDetectionFrame> m_Buffer;

        public ConcurrentQueue<RawMeshDetectionFrame> Buffer => m_Buffer;

        public MeshDetectionBuffer(ARMeshManager meshManager, IClock clock)
        {
            m_Buffer = new ConcurrentQueue<RawMeshDetectionFrame>();
            m_MeshManager = meshManager;
            m_Clock = clock;
        }

        public void StartCapture()
        {
            m_MeshManager.meshesChanged += OnMeshesChanged;
        }

        public void StopCapture()
        {
            m_MeshManager.meshesChanged -= OnMeshesChanged;
        }

        private async void OnMeshesChanged(ARMeshesChangedEventArgs changes)
        {
            var deviceTime = m_Clock.UtcNow;
            var tasks = new List<Task>
            {
                AddToBuffer(changes.added, deviceTime, MeshDetectionState.Added),
                AddToBuffer(changes.updated, deviceTime, MeshDetectionState.Updated),
            };
            await Task.WhenAll(tasks);
            AddToBuffer(changes.removed, deviceTime);
        }

        private async Task AddToBuffer(
            List<UnityMeshFilter> meshFilters,
            DateTime deviceTimestampAtCapture,
            MeshDetectionState state
        )
        {
            if (meshFilters == null || meshFilters.Count == 0)
            {
                return;
            }
            // https://docs.unity3d.com/Packages/com.unity.cloud.draco@5.1/manual/use-case-encoding.html#encode-using-the-advanced-mesh-api
            using var meshDataArray = Mesh.AcquireReadOnlyMeshData(
                meshFilters.Select(mf => mf.sharedMesh).ToArray()
            );
            var encodeTasks = new List<Task>();
            for (int i = 0; i < meshFilters.Count; i++)
            {
                var encodeTask = Task.Run(async () =>
                {
                    var results = await DracoEncoder.EncodeMesh(
                        meshFilters[i].sharedMesh,
                        meshDataArray[i]
                    );
                    if (results == null)
                    {
                        InternalDebug.LogWarning(
                            $"Encoding failed for mesh filter with ID {meshFilters[i].GetInstanceID()}"
                        );
                        return;
                    }
                    m_Buffer.Enqueue(new RawMeshDetectionFrame
                    {
                        State = state,
                        // TODO: How does the transform/pose for MeshFilter look like?
                        // Pose = meshFilters[i].transform,
                        MeshFilterId = meshFilters[i].GetInstanceID(),
                        DeviceTimestamp = deviceTimestampAtCapture,
                        EncodedSubMeshes = results.Select(r => r.data.ToArray()).ToList(),
                    });
                    foreach (var result in results)
                    {
                        result.Dispose();
                    }
                });
                encodeTasks.Add(encodeTask);
            }
            await Task.WhenAll(encodeTasks);
        }

        private void AddToBuffer(
            List<UnityMeshFilter> meshFilters,
            DateTime deviceTimestampAtCapture
        )
        {
            foreach (var meshFilter in meshFilters)
            {
                m_Buffer.Enqueue(new RawMeshDetectionFrame
                {
                    State = MeshDetectionState.Removed,
                    // TODO: How does the transform/pose for MeshFilter look like?
                    // Pose = meshFilter.Value.pose,
                    MeshFilterId = meshFilter.GetInstanceID(),
                    DeviceTimestamp = deviceTimestampAtCapture,
                });
            }
        }

        public RawMeshDetectionFrame TryAcquireLatestFrame()
        {
            return m_Buffer.LastOrDefault();
        }

        public ARFrame[] TakeARFrames()
        {
            ConcurrentQueue<RawMeshDetectionFrame> oldFrames;
            lock (m_Buffer)
            {
                oldFrames = m_Buffer;
                m_Buffer = new();
            }
            return oldFrames.Select(frame => (ARFrame)frame).ToArray();
        }

        public void Dispose()
        {
            StopCapture();
            m_Buffer.Clear();
        }
    }
}
