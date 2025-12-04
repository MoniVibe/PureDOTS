using Unity.Burst;
using Unity.Entities;

namespace PureDOTS.Runtime.Individual
{
    /// <summary>
    /// Checks promotion thresholds and promotes Villagers to SimIndividuals.
    /// When promoted: adds SimIndividual components, removes lightweight Villager-only components.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct PromotionSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new Unity.Collections.LowLevel.Unsafe.UnsafeList<EntityCommandBuffer>(1, Unity.Collections.Allocator.Temp);
            var ecbParallel = state.GetEntityCommandBuffer(state.WorldUnmanaged.UpdateAllocator.ToAllocator);

            var job = new CheckPromotionJob
            {
                Ecb = ecbParallel.AsParallelWriter()
            };
            job.ScheduleParallel();

            state.Dependency.Complete();
        }

        [BurstCompile]
        partial struct CheckPromotionJob : IJobEntity
        {
            public Unity.Entities.EntityCommandBuffer.ParallelWriter Ecb;

            void Execute(
                Entity entity,
                in PromotionCandidate candidate,
                [ChunkIndexInQuery] int chunkIndex)
            {
                // Check if any promotion threshold is met
                bool shouldPromote = candidate.FameThresholdMet ||
                                    candidate.KillsThresholdMet ||
                                    candidate.AchievementThresholdMet ||
                                    candidate.PlayerSelected;

                if (shouldPromote)
                {
                    // Mark entity for promotion (actual promotion handled by PromotionHelpers)
                    Ecb.AddComponent<PromotionPending>(chunkIndex, entity);
                    Ecb.RemoveComponent<PromotionCandidate>(chunkIndex, entity);
                }
            }
        }
    }

    /// <summary>
    /// Tag component indicating entity is pending promotion.
    /// </summary>
    public struct PromotionPending : IComponentData
    {
    }
}

