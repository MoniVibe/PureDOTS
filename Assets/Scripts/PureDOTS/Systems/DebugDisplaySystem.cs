using PureDOTS.Runtime.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Systems
{
    /// <summary>
    /// Updates debug display singleton with current simulation state.
    /// Runs in presentation group to provide data for UI layers.
    /// Deterministic and Burst-safe.
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial struct DebugDisplaySystem : ISystem
    {
        private EntityQuery _villagerQuery;
        private EntityQuery _storehouseQuery;

        public void OnCreate(ref SystemState state)
        {
            // Create singleton if it doesn't exist
            if (!SystemAPI.HasSingleton<DebugDisplayData>())
            {
                var entity = state.EntityManager.CreateEntity();
                state.EntityManager.AddComponent<DebugDisplayData>(entity);
            }

            // Cache queries for performance
            _villagerQuery = SystemAPI.QueryBuilder()
                .WithAll<VillagerId>()
                .Build();

            _storehouseQuery = SystemAPI.QueryBuilder()
                .WithAll<StorehouseInventory>()
                .Build();
        }

        public void OnUpdate(ref SystemState state)
        {
            // Ensure singleton exists
            if (!SystemAPI.HasSingleton<DebugDisplayData>())
            {
                return;
            }

            var debugData = SystemAPI.GetSingletonRW<DebugDisplayData>();

            // Update time state
            if (SystemAPI.HasSingleton<TimeState>())
            {
                var timeState = SystemAPI.GetSingleton<TimeState>();
                debugData.ValueRW.CurrentTick = timeState.Tick;
                debugData.ValueRW.IsPaused = timeState.IsPaused;
                
                // Format with FixedString (Burst-safe)
                var text = new FixedString128Bytes();
                text.Append("Tick: ");
                text.Append(timeState.Tick);
                text.Append(" | Speed: ");
                // Format float to 2 decimal places
                var speedRounded = math.round(timeState.CurrentSpeedMultiplier * 100f) / 100f;
                text.Append(speedRounded);
                text.Append(" | ");
                text.Append(timeState.IsPaused ? "Paused" : "Running");
                debugData.ValueRW.TimeStateText = text;
            }

            // Update rewind state
            if (SystemAPI.HasSingleton<RewindState>())
            {
                var rewindState = SystemAPI.GetSingleton<RewindState>();
                
                // Format with FixedString (Burst-safe)
                var text = new FixedString128Bytes();
                text.Append("Mode: ");
                switch (rewindState.Mode)
                {
                    case RewindMode.Record:
                        text.Append("Record");
                        break;
                    case RewindMode.Playback:
                        text.Append("Playback");
                        break;
                    case RewindMode.CatchUp:
                        text.Append("CatchUp");
                        break;
                    default:
                        text.Append("Unknown");
                        break;
                }
                text.Append(" | Playback Tick: ");
                text.Append(rewindState.PlaybackTick);
                debugData.ValueRW.RewindStateText = text;
            }

            // Update villager count
            debugData.ValueRW.VillagerCount = _villagerQuery.CalculateEntityCount();

            // Update storehouse totals
            float totalStored = 0f;
            foreach (var inventory in SystemAPI.Query<RefRO<StorehouseInventory>>())
            {
                totalStored += inventory.ValueRO.TotalStored;
            }
            debugData.ValueRW.TotalResourcesStored = totalStored;
        }
    }
}
