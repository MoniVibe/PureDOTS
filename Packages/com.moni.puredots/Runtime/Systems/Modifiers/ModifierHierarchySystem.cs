using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Modifiers;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Systems.Modifiers
{
    /// <summary>
    /// Propagates modifiers from parent to child entities (villager → village → kingdom).
    /// Child entities reference parent via Entity ID (not recursion).
    /// Parent recomputes only when children flag ModifierDirtyTag.
    /// Children cache inherited results.
    /// O(log n) propagation instead of O(n²).
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    [UpdateAfter(typeof(ModifierAggregationSystem))]
    public partial struct ModifierHierarchySystem : ISystem
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

            // Process hierarchical propagation
            new PropagateHierarchyJob
            {
                CurrentTick = currentTick
            }.ScheduleParallel();
        }

        [BurstCompile]
        public partial struct PropagateHierarchyJob : IJobEntity
        {
            public uint CurrentTick;

            void Execute(
                Entity entity,
                [EntityIndexInQuery] int entityInQueryIndex,
                in ModifierAggregator aggregator,
                ref ModifierCategoryAccumulator accumulator)
            {
                // If this entity has children with dirty modifiers, recompute aggregate
                // For now, this is a placeholder - full implementation would:
                // 1. Query child entities
                // 2. Sum their ModifierCategoryAccumulator values
                // 3. Write to parent's ModifierAggregator
                // 4. Cache result in children's inherited modifier cache

                // Update last recompute tick
                // aggregator.LastRecomputeTick = CurrentTick;
            }
        }
    }
}

