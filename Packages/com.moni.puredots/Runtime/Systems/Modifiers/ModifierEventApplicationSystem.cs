using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Modifiers;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace PureDOTS.Systems.Modifiers
{
    /// <summary>
    /// Processes ApplyModifierEvent buffer and applies modifiers to target entities.
    /// Runs in EventSystemGroup before gameplay systems.
    /// Event-driven: modifiers applied only when source/target changes, not every tick.
    /// 
    /// USAGE:
    /// 1. Get ModifierEventCoordinator entity: SystemAPI.GetSingletonEntity&lt;ModifierEventCoordinator&gt;()
    /// 2. Get event buffer: SystemAPI.GetBuffer&lt;ApplyModifierEvent&gt;(coordinatorEntity)
    /// 3. Add events: events.Add(new ApplyModifierEvent { Target = entity, ModifierId = id, ... })
    /// 4. Events are processed automatically by this system
    /// 
    /// See: Docs/Guides/ModifierSystemGuide.md for detailed usage examples.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(EventSystemGroup))]
    public partial struct ModifierEventApplicationSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var rewindState = SystemAPI.GetSingleton<RewindState>();
            if (rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            // Get modifier catalog
            if (!SystemAPI.TryGetSingleton<ModifierCatalogRef>(out var catalogRef) ||
                !catalogRef.Blob.IsCreated)
            {
                return; // No modifier catalog configured
            }

            // Get event coordinator entity
            var coordinatorQuery = SystemAPI.QueryBuilder()
                .WithAll<ModifierEventCoordinator>()
                .Build();

            if (coordinatorQuery.IsEmpty)
            {
                return; // No coordinator entity exists
            }

            var coordinatorEntity = coordinatorQuery.GetSingletonEntity();
            var events = SystemAPI.GetBuffer<ApplyModifierEvent>(coordinatorEntity);

            if (events.Length == 0)
            {
                return; // No events to process
            }

            var ecbSingleton = SystemAPI.GetSingletonRW<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.ValueRW.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

            // Process all events in parallel job
            new ProcessModifierEventsJob
            {
                Catalog = catalogRef.Blob,
                Events = events.AsNativeArray(),
                Ecb = ecb
            }.ScheduleParallel(events.Length, 64, state.Dependency).Complete();

            // Clear processed events
            events.Clear();
        }

        [BurstCompile]
        public partial struct ProcessModifierEventsJob : IJobFor
        {
            [ReadOnly]
            public BlobAssetReference<ModifierCatalogBlob> Catalog;

            [ReadOnly]
            public NativeArray<ApplyModifierEvent> Events;

            public EntityCommandBuffer.ParallelWriter Ecb;

            public void Execute(int index)
            {
                var evt = Events[index];
                
                // Validate modifier ID
                ref var catalog = ref Catalog.Value;
                if (evt.ModifierId >= catalog.Modifiers.Length)
                {
                    return; // Invalid modifier ID
                }

                // Ensure target entity has modifier buffer
                if (!Ecb.HasComponent<DynamicBuffer<ModifierInstance>>(evt.Target))
                {
                    Ecb.AddBuffer<ModifierInstance>(index, evt.Target);
                }

                // Determine modifier value (use override if provided, otherwise use BaseValue from catalog)
                ref var modifierSpec = ref catalog.Modifiers[evt.ModifierId];
                float modifierValue = evt.Value != 0f ? evt.Value : modifierSpec.BaseValue;

                // Add modifier instance to target entity's buffer
                Ecb.AppendToBuffer(index, evt.Target, new ModifierInstance
                {
                    ModifierId = evt.ModifierId,
                    Value = modifierValue,
                    Duration = evt.Duration
                });

                // Mark entity as dirty for hot path recomputation
                Ecb.AddComponent<ModifierDirtyTag>(index, evt.Target);
            }
        }
    }
}

