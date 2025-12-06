using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Modifiers;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Systems.Modifiers
{
    /// <summary>
    /// Hot path system: applies active modifiers to numeric stats every tick (60Hz).
    /// Formula: total = baseValue * (1 + sumMul) + sumAdd
    /// Only touches entities with active modifiers and ModifierDirtyTag.
    /// 
    /// OUTPUT:
    /// Writes aggregated modifier sums to ModifierCategoryAccumulator component.
    /// Other systems read from accumulator to apply modifiers to stats.
    /// 
    /// USAGE:
    /// var accumulator = SystemAPI.GetComponent&lt;ModifierCategoryAccumulator&gt;(entity);
    /// float finalValue = baseValue * (1f + accumulator.MilitaryMul) + accumulator.MilitaryAdd;
    /// 
    /// See: Docs/Guides/ModifierSystemAPI.md for API reference.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(ModifierHotPathGroup))]
    public partial struct ModifierHotPathSystem : ISystem
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
                return;
            }

            var timeState = SystemAPI.GetSingleton<TimeState>();
            var currentTick = timeState.Tick;

            var ecbSingleton = SystemAPI.GetSingletonRW<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.ValueRW.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

            // Process entities with active modifiers and dirty tag
            // Query includes ModifierDirtyTag requirement
            new ApplyModifiersJob
            {
                Catalog = catalogRef.Blob,
                CurrentTick = currentTick,
                Ecb = ecb
            }.ScheduleParallel();

            state.Dependency.Complete();
        }

        [BurstCompile]
        public partial struct ApplyModifiersJob : IJobEntity
        {
            [ReadOnly]
            public BlobAssetReference<ModifierCatalogBlob> Catalog;

            public uint CurrentTick;
            public EntityCommandBuffer.ParallelWriter Ecb;

            void Execute(
                Entity entity,
                [EntityIndexInQuery] int entityInQueryIndex,
                in DynamicBuffer<ModifierInstance> modifiers,
                ref ModifierCategoryAccumulator accumulator)
            {
                ref var catalog = ref Catalog.Value;

                // Reset accumulator
                accumulator = new ModifierCategoryAccumulator
                {
                    LastUpdateTick = CurrentTick
                };

                // Process all active modifiers
                for (int i = 0; i < modifiers.Length; i++)
                {
                    var modifier = modifiers[i];

                    // Skip if modifier ID is invalid
                    if (modifier.ModifierId >= catalog.Modifiers.Length)
                    {
                        continue;
                    }

                    ref var spec = ref catalog.Modifiers[modifier.ModifierId];

                    // Apply modifier based on operation type and category
                    switch ((ModifierOperation)spec.Operation)
                    {
                        case ModifierOperation.Add:
                            ApplyAdditiveModifier(ref accumulator, (ModifierCategory)spec.Category, modifier.Value);
                            break;

                        case ModifierOperation.Multiply:
                            ApplyMultiplicativeModifier(ref accumulator, (ModifierCategory)spec.Category, modifier.Value);
                            break;

                        case ModifierOperation.Override:
                            // Override is handled separately if needed
                            ApplyAdditiveModifier(ref accumulator, (ModifierCategory)spec.Category, modifier.Value);
                            break;
                    }
                }

                // Remove dirty tag after processing
                Ecb.RemoveComponent<ModifierDirtyTag>(entityInQueryIndex, entity);
            }

            private static void ApplyAdditiveModifier(ref ModifierCategoryAccumulator accumulator, ModifierCategory category, float value)
            {
                switch (category)
                {
                    case ModifierCategory.Economy:
                        accumulator.EconomicAdd += value;
                        break;
                    case ModifierCategory.Military:
                        accumulator.MilitaryAdd += value;
                        break;
                    case ModifierCategory.Environment:
                        accumulator.EnvironmentAdd += value;
                        break;
                }
            }

            private static void ApplyMultiplicativeModifier(ref ModifierCategoryAccumulator accumulator, ModifierCategory category, float value)
            {
                switch (category)
                {
                    case ModifierCategory.Economy:
                        accumulator.EconomicMul += value;
                        break;
                    case ModifierCategory.Military:
                        accumulator.MilitaryMul += value;
                        break;
                    case ModifierCategory.Environment:
                        accumulator.EnvironmentMul += value;
                        break;
                }
            }
        }
    }
}

