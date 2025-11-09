using PureDOTS.Runtime.Components;
using PureDOTS.Input;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Systems.Input
{
    /// <summary>
    /// Feeds recorded input snapshots into ECS instead of live input during playback mode.
    /// Integrates with RewindState to enable deterministic replay.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    [UpdateBefore(typeof(CopyInputToEcsSystem))]
    public partial struct InputPlaybackSystem : ISystem
    {
        private NativeHashMap<uint, InputSnapshotRecord> _playbackSnapshots;
        private NativeList<HandInputEdge> _playbackHandEdges;
        private NativeList<CameraInputEdge> _playbackCameraEdges;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _playbackSnapshots = new NativeHashMap<uint, InputSnapshotRecord>(1024, Allocator.Persistent);
            _playbackHandEdges = new NativeList<HandInputEdge>(1024, Allocator.Persistent);
            _playbackCameraEdges = new NativeList<CameraInputEdge>(1024, Allocator.Persistent);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var rewindState = SystemAPI.GetSingleton<RewindState>();
            if (rewindState.Mode != RewindMode.Playback)
            {
                return;
            }
            var timeState = SystemAPI.GetSingleton<TimeState>();
            uint currentTick = timeState.Tick;

            if (!_playbackSnapshots.TryGetValue(currentTick, out var snapshot))
            {
                return; // No recorded input for this tick
            }

            foreach (var (handInputRef, handEntity) in SystemAPI.Query<RefRW<DivineHandInput>>().WithEntityAccess())
            {
                handInputRef.ValueRW = snapshot.HandInput;

                if (state.EntityManager.HasBuffer<HandInputEdge>(handEntity))
                {
                    var edges = state.EntityManager.GetBuffer<HandInputEdge>(handEntity);
                    edges.Clear();
                    for (int i = 0; i < snapshot.HandEdgeCount; i++)
                    {
                        edges.Add(_playbackHandEdges[snapshot.HandEdgeStart + i]);
                    }
                }
                else if (snapshot.HandEdgeCount > 0)
                {
                    var edges = state.EntityManager.AddBuffer<HandInputEdge>(handEntity);
                    for (int i = 0; i < snapshot.HandEdgeCount; i++)
                    {
                        edges.Add(_playbackHandEdges[snapshot.HandEdgeStart + i]);
                    }
                }
            }

            foreach (var (cameraInputRef, cameraEntity) in SystemAPI.Query<RefRW<CameraInputState>>().WithEntityAccess())
            {
                cameraInputRef.ValueRW = snapshot.CameraInput;

                if (state.EntityManager.HasBuffer<CameraInputEdge>(cameraEntity))
                {
                    var edges = state.EntityManager.GetBuffer<CameraInputEdge>(cameraEntity);
                    edges.Clear();
                    for (int i = 0; i < snapshot.CameraEdgeCount; i++)
                    {
                        edges.Add(_playbackCameraEdges[snapshot.CameraEdgeStart + i]);
                    }
                }
                else if (snapshot.CameraEdgeCount > 0)
                {
                    var edges = state.EntityManager.AddBuffer<CameraInputEdge>(cameraEntity);
                    for (int i = 0; i < snapshot.CameraEdgeCount; i++)
                    {
                        edges.Add(_playbackCameraEdges[snapshot.CameraEdgeStart + i]);
                    }
                }
            }
        }

        /// <summary>
        /// Loads recorded snapshots for playback.
        /// </summary>
        public void LoadPlaybackData(
            NativeList<InputSnapshotRecord> recordedSnapshots,
            NativeList<HandInputEdge> recordedHandEdges,
            NativeList<CameraInputEdge> recordedCameraEdges)
        {
            _playbackSnapshots.Clear();
            _playbackHandEdges.Clear();
            _playbackCameraEdges.Clear();

            for (int i = 0; i < recordedSnapshots.Length; i++)
            {
                var record = recordedSnapshots[i];
                var remapped = record;

                if (record.HandEdgeCount > 0)
                {
                    remapped.HandEdgeStart = _playbackHandEdges.Length;
                    for (int j = 0; j < record.HandEdgeCount; j++)
                    {
                        _playbackHandEdges.Add(recordedHandEdges[record.HandEdgeStart + j]);
                    }
                }
                else
                {
                    remapped.HandEdgeStart = _playbackHandEdges.Length;
                }

                if (record.CameraEdgeCount > 0)
                {
                    remapped.CameraEdgeStart = _playbackCameraEdges.Length;
                    for (int j = 0; j < record.CameraEdgeCount; j++)
                    {
                        _playbackCameraEdges.Add(recordedCameraEdges[record.CameraEdgeStart + j]);
                    }
                }
                else
                {
                    remapped.CameraEdgeStart = _playbackCameraEdges.Length;
                }

                _playbackSnapshots.TryAdd(remapped.Tick, remapped);
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            if (_playbackSnapshots.IsCreated)
            {
                _playbackSnapshots.Dispose();
            }

            if (_playbackHandEdges.IsCreated)
            {
                _playbackHandEdges.Dispose();
            }

            if (_playbackCameraEdges.IsCreated)
            {
                _playbackCameraEdges.Dispose();
            }
        }
    }
}

