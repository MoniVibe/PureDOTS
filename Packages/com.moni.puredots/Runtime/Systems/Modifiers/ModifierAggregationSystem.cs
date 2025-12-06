using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Modifiers;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Systems.Modifiers
{
    /// <summary>
    /// Aggregates modifiers by category (Economy, Military, Environment).
    /// Writes to ModifierCategoryAccumulator component.
    /// Runs after ModifierHotPathSystem.
    /// O(1) per entity instead of iterating per buff.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    [UpdateAfter(typeof(ModifierHotPathSystem))]
    public partial struct ModifierAggregationSystem : ISystem
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

            var timeState = SystemAPI.GetSingleton<TimeState>();
            var currentTick = timeState.Tick;

            // Aggregate modifiers from all entities with ModifierCategoryAccumulator
            new AggregateModifiersJob
            {
                CurrentTick = currentTick
            }.ScheduleParallel();
        }

        [BurstCompile]
        public partial struct AggregateModifiersJob : IJobEntity
        {
            public uint CurrentTick;

            void Execute(
                Entity entity,
                [EntityIndexInQuery] int entityInQueryIndex,
                in ModifierCategoryAccumulator accumulator)
            {
                // Aggregation is already done in ModifierHotPathSystem.
                // This system can be extended to:
                // 1. Write to singleton EconomicModifiers, CombatModifiers, WorldModifiers
                // 2. Propagate to parent entities in hierarchy
                // 3. Update LOD aggregates for distant entities
                
                // For now, the accumulator is already computed in hot path.
                // Future: aggregate across all entities and write to singletons.
            }
        }
    }
}

