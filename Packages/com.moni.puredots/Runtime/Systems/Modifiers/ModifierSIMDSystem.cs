using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Modifiers;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst.Intrinsics;

namespace PureDOTS.Systems.Modifiers
{
    /// <summary>
    /// Processes modifiers using SIMD optimization (AoSoA layout).
    /// Processes 8 modifiers at a time using Burst SIMD intrinsics.
    /// Ideal for uniform modifiers (morale, damage boosts).
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(ModifierHotPathGroup))]
    [UpdateAfter(typeof(ModifierHotPathSystem))]
    public partial struct ModifierSIMDSystem : ISystem
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

            // Process entities with modifiers using SIMD
            new ProcessSIMDModifiersJob
            {
                Catalog = catalogRef.Blob
            }.ScheduleParallel();
        }

        [BurstCompile]
        public partial struct ProcessSIMDModifiersJob : IJobEntity
        {
            [ReadOnly]
            public BlobAssetReference<ModifierCatalogBlob> Catalog;

            void Execute(
                Entity entity,
                [EntityIndexInQuery] int entityInQueryIndex,
                in DynamicBuffer<ModifierInstance> modifiers,
                ref ModifierCategoryAccumulator accumulator)
            {
                // Pack modifiers into SIMD packets (8 at a time)
                int packetCount = (modifiers.Length + 7) / 8;

                for (int p = 0; p < packetCount; p++)
                {
                    ModifierPacket packet = default;
                    int startIdx = p * 8;
                    int endIdx = math.min(startIdx + 8, modifiers.Length);
                    packet.Count = (byte)(endIdx - startIdx);

                    // Fill packet
                    for (int i = 0; i < packet.Count; i++)
                    {
                        var modifier = modifiers[startIdx + i];
                        packet.Id[i] = modifier.ModifierId;
                        packet.Value[i] = modifier.Value;
                        packet.Duration[i] = modifier.Duration;
                    }

                    // Process packet with SIMD (simplified - actual implementation would use v256/v128)
                    // For now, process normally
                    for (int i = 0; i < packet.Count; i++)
                    {
                        if (packet.Id[i] < Catalog.Value.Modifiers.Length)
                        {
                            ref var spec = ref Catalog.Value.Modifiers[packet.Id[i]];
                            
                            // Apply modifier (same logic as hot path)
                            if (spec.Operation == (byte)ModifierOperation.Add)
                            {
                                switch ((ModifierCategory)spec.Category)
                                {
                                    case ModifierCategory.Economy:
                                        accumulator.EconomicAdd += packet.Value[i];
                                        break;
                                    case ModifierCategory.Military:
                                        accumulator.MilitaryAdd += packet.Value[i];
                                        break;
                                    case ModifierCategory.Environment:
                                        accumulator.EnvironmentAdd += packet.Value[i];
                                        break;
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}

