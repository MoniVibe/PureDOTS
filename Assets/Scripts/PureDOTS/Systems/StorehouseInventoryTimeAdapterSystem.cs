using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Time;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Systems
{
    /// <summary>
    /// Snapshots storehouse inventory state to guarantee rewind restores deposits and withdrawals.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(HistorySystemGroup))]
    [UpdateAfter(typeof(StorehouseHistoryRecordingSystem))]
    public partial struct StorehouseInventoryTimeAdapterSystem : ISystem
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

            state.RequireForUpdate<StorehouseInventory>();
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
            var records = new NativeList<StorehouseInventoryRecord>(Allocator.Temp);
            foreach (var (inventory, entity) in SystemAPI.Query<RefRO<StorehouseInventory>>().WithEntityAccess())
            {
                records.Add(new StorehouseInventoryRecord
                {
                    Storehouse = entity,
                    Inventory = inventory.ValueRO
                });
            }

            var writer = new TimeStreamWriter(ref _serializedState);
            writer.Write(records.Length);
            for (int i = 0; i < records.Length; i++)
            {
                writer.Write(records[i]);
            }

            records.Dispose();
        }

        private void Load(ref SystemState state)
        {
            if (_serializedState.Length == 0)
            {
                return;
            }

            using var bytes = new NativeArray<byte>(_serializedState.AsArray(), Allocator.Temp);
            var reader = new TimeStreamReader(bytes);

            var count = reader.Read<int>();
            for (int i = 0; i < count; i++)
            {
                var record = reader.Read<StorehouseInventoryRecord>();
                if (!SystemAPI.Exists(record.Storehouse) ||
                    !SystemAPI.HasComponent<StorehouseInventory>(record.Storehouse))
                {
                    continue;
                }

                SystemAPI.SetComponent(record.Storehouse, record.Inventory);
            }
        }

        private struct StorehouseInventoryRecord
        {
            public Entity Storehouse;
            public StorehouseInventory Inventory;
        }
    }
}
