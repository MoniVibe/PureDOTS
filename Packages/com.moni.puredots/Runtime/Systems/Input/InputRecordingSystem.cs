using PureDOTS.Runtime.Components;
using PureDOTS.Input;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Systems.Input
{
    /// <summary>
    /// Recorded input snapshot for a single tick.
    /// </summary>
    public struct InputSnapshotRecord
    {
        public uint Tick;
        public DivineHandInput HandInput;
        public CameraInputState CameraInput;
        public int HandEdgeStart;
        public int HandEdgeCount;
        public int CameraEdgeStart;
        public int CameraEdgeCount;
    }

    /// <summary>
    /// Records input snapshots (state + edges) per tick to a blob or buffer for deterministic replay.
    /// Guards recording with config flag to avoid overhead when not needed.
    /// Note: Runs in HistorySystemGroup, which executes after SimulationSystemGroup where CopyInputToEcsSystem runs.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(HistorySystemGroup))]
    public partial struct InputRecordingSystem : ISystem
    {

        private NativeList<InputSnapshotRecord> _recordedSnapshots;
        private NativeList<HandInputEdge> _recordedHandEdges;
        private NativeList<CameraInputEdge> _recordedCameraEdges;
        private bool _isRecording;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _recordedSnapshots = new NativeList<InputSnapshotRecord>(1024, Allocator.Persistent);
            _recordedHandEdges = new NativeList<HandInputEdge>(1024, Allocator.Persistent);
            _recordedCameraEdges = new NativeList<CameraInputEdge>(1024, Allocator.Persistent);
            _isRecording = false;
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var rewindState = SystemAPI.GetSingleton<RewindState>();
            if (rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            // Check config flag to enable/disable recording
            bool shouldRecord = true;
            if (SystemAPI.HasSingleton<HistorySettings>())
            {
                var historySettings = SystemAPI.GetSingleton<HistorySettings>();
                shouldRecord = historySettings.EnableInputRecording;
            }

            if (!shouldRecord)
            {
                return;
            }

            if (!_isRecording)
            {
                _isRecording = true;
                _recordedSnapshots.Clear();
                _recordedHandEdges.Clear();
                _recordedCameraEdges.Clear();
            }

            var timeState = SystemAPI.GetSingleton<TimeState>();
            uint currentTick = timeState.Tick;

            // Record hand input snapshots
            foreach (var (handInput, handEdges, handEntity) in SystemAPI
                .Query<RefRO<DivineHandInput>, DynamicBuffer<HandInputEdge>>()
                .WithEntityAccess())
            {
                var record = new InputSnapshotRecord
                {
                    Tick = currentTick,
                    HandInput = handInput.ValueRO,
                    CameraInput = default,
                    HandEdgeStart = _recordedHandEdges.Length,
                    HandEdgeCount = 0,
                    CameraEdgeStart = _recordedCameraEdges.Length,
                    CameraEdgeCount = 0
                };

                for (int i = 0; i < handEdges.Length; i++)
                {
                    _recordedHandEdges.Add(handEdges[i]);
                    record.HandEdgeCount++;
                }

                _recordedSnapshots.Add(record);
                break; // Only record first hand entity
            }

            // Record camera input snapshots
            foreach (var (cameraInput, cameraEdges, cameraEntity) in SystemAPI
                .Query<RefRO<CameraInputState>, DynamicBuffer<CameraInputEdge>>()
                .WithEntityAccess())
            {
                // Find or create record for this tick
                int recordIndex = -1;
                for (int i = _recordedSnapshots.Length - 1; i >= 0; i--)
                {
                    if (_recordedSnapshots[i].Tick == currentTick)
                    {
                        recordIndex = i;
                        break;
                    }
                }

                if (recordIndex >= 0)
                {
                    var record = _recordedSnapshots[recordIndex];
                    record.CameraInput = cameraInput.ValueRO;
                    record.CameraEdgeStart = _recordedCameraEdges.Length;
                    record.CameraEdgeCount = 0;
                    for (int i = 0; i < cameraEdges.Length; i++)
                    {
                        _recordedCameraEdges.Add(cameraEdges[i]);
                        record.CameraEdgeCount++;
                    }
                    _recordedSnapshots[recordIndex] = record;
                }
                else
                {
                    var record = new InputSnapshotRecord
                    {
                        Tick = currentTick,
                        HandInput = default,
                        CameraInput = cameraInput.ValueRO,
                        HandEdgeStart = _recordedHandEdges.Length,
                        HandEdgeCount = 0,
                        CameraEdgeStart = _recordedCameraEdges.Length,
                        CameraEdgeCount = 0
                    };

                    for (int i = 0; i < cameraEdges.Length; i++)
                    {
                        _recordedCameraEdges.Add(cameraEdges[i]);
                        record.CameraEdgeCount++;
                    }

                    _recordedSnapshots.Add(record);
                }

                break; // Only record first camera entity
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            if (_recordedSnapshots.IsCreated)
            {
                _recordedSnapshots.Dispose();
            }

            if (_recordedHandEdges.IsCreated)
            {
                _recordedHandEdges.Dispose();
            }

            if (_recordedCameraEdges.IsCreated)
            {
                _recordedCameraEdges.Dispose();
            }
        }

        /// <summary>
        /// Gets recorded snapshots for a tick range along with edge data.
        /// </summary>
        public void GetRecordedData(
            uint startTick,
            uint endTick,
            Allocator allocator,
            out NativeList<InputSnapshotRecord> snapshots,
            out NativeList<HandInputEdge> handEdges,
            out NativeList<CameraInputEdge> cameraEdges)
        {
            snapshots = new NativeList<InputSnapshotRecord>(allocator);
            handEdges = new NativeList<HandInputEdge>(allocator);
            cameraEdges = new NativeList<CameraInputEdge>(allocator);

            for (int i = 0; i < _recordedSnapshots.Length; i++)
            {
                var record = _recordedSnapshots[i];
                if (record.Tick < startTick || record.Tick > endTick)
                {
                    continue;
                }

                // Copy edges into the outgoing lists and remap indices
                var remapped = record;

                remapped.HandEdgeStart = handEdges.Length;
                for (int j = 0; j < record.HandEdgeCount; j++)
                {
                    handEdges.Add(_recordedHandEdges[record.HandEdgeStart + j]);
                }

                remapped.CameraEdgeStart = cameraEdges.Length;
                for (int j = 0; j < record.CameraEdgeCount; j++)
                {
                    cameraEdges.Add(_recordedCameraEdges[record.CameraEdgeStart + j]);
                }

                snapshots.Add(remapped);
            }
        }
    }
}

