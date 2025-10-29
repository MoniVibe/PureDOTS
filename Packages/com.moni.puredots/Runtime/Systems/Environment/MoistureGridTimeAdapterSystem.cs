using PureDOTS.Environment;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Time;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Systems.Environment
{
    /// <summary>
    /// Serialises and restores moisture grid state across rewind phases to guarantee determinism.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(HistorySystemGroup))]
    public partial struct MoistureGridTimeAdapterSystem : ISystem
    {
        private NativeList<byte> _serializedState;
        private TimeAwareController _controller;
        private uint _lastRecordedTick;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _serializedState = new NativeList<byte>(Allocator.Persistent);
            _controller = new TimeAwareController(
                TimeAwareExecutionPhase.Record | TimeAwareExecutionPhase.CatchUp | TimeAwareExecutionPhase.Playback,
                TimeAwareExecutionOptions.SkipWhenPaused);
            _lastRecordedTick = uint.MaxValue;

            state.RequireForUpdate<MoistureGrid>();
            state.RequireForUpdate<MoistureGridSimulationState>();
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            if (_serializedState.IsCreated)
            {
                _serializedState.Dispose();
            }
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            var rewindState = SystemAPI.GetSingleton<RewindState>();

            if (!_controller.TryBegin(timeState, rewindState, out var context))
            {
                return;
            }

            if (context.IsRecordPhase)
            {
                if (context.Time.Tick == _lastRecordedTick)
                {
                    return;
                }

                _lastRecordedTick = context.Time.Tick;
                Save(ref state);
            }
            else if (context.IsCatchUpPhase || context.IsPlaybackPhase)
            {
                Load(ref state);
            }

            if (context.ModeChangedThisFrame && context.PreviousMode == RewindMode.Playback && context.IsRecordPhase)
            {
                _lastRecordedTick = uint.MaxValue;
            }
        }

        private void Save(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingletonEntity<MoistureGrid>(out var gridEntity))
            {
                return;
            }

            var cells = SystemAPI.GetBuffer<MoistureGridRuntimeCell>(gridEntity);
            var writer = new TimeStreamWriter(ref _serializedState);
            writer.Write(cells.Length);

            for (int i = 0; i < cells.Length; i++)
            {
                writer.Write(cells[i]);
            }

            var simulation = SystemAPI.GetSingleton<MoistureGridSimulationState>();
            writer.Write(simulation);
        }

        private void Load(ref SystemState state)
        {
            if (_serializedState.Length == 0)
            {
                return;
            }

            if (!SystemAPI.TryGetSingletonEntity<MoistureGrid>(out var gridEntity))
            {
                return;
            }

            using var array = new NativeArray<byte>(_serializedState.AsArray(), Allocator.Temp);
            var reader = new TimeStreamReader(array);

            var length = reader.Read<int>();
            var buffer = SystemAPI.GetBuffer<MoistureGridRuntimeCell>(gridEntity);
            buffer.ResizeUninitialized(length);

            for (int i = 0; i < length; i++)
            {
                buffer[i] = reader.Read<MoistureGridRuntimeCell>();
            }

            var simulation = reader.Read<MoistureGridSimulationState>();
            var simRef = SystemAPI.GetSingletonRW<MoistureGridSimulationState>();
            simRef.ValueRW = simulation;
        }
    }
}
