using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Resource;
using PureDOTS.Systems;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Space4X.Systems
{
    /// <summary>
    /// Handles mining work ticks: progress mining, emit resources, deplete deposits.
    /// Fixed-step, Burst-compiled for determinism.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(ResourceSystemGroup))]
    [UpdateAfter(typeof(Space4XDepositSpawnerSystem))]
    public partial struct Space4XMiningSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<HarvestNode>();
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            if (timeState.IsPaused)
            {
                return;
            }

            var rewindState = SystemAPI.GetSingleton<RewindState>();
            if (rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            if (!SystemAPI.TryGetSingleton<DepositCatalog>(out var depositCatalog))
            {
                return;
            }

            var deltaTime = timeState.FixedDeltaTime;

            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var ecbParallel = ecb.AsParallelWriter();

            var job = new MiningWorkJob
            {
                DepositCatalog = depositCatalog.Catalog,
                DeltaTime = deltaTime,
                ECB = ecbParallel
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
            state.Dependency.Complete();

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        public partial struct MiningWorkJob : IJobEntity
        {
            [ReadOnly] public BlobAssetReference<DepositCatalogBlob> DepositCatalog;
            public float DeltaTime;
            public EntityCommandBuffer.ParallelWriter ECB;

            public void Execute(
                [EntityIndexInQuery] int entityInQueryIndex,
                Entity entity,
                ref HarvestNode harvestNode,
                in DepositEntity deposit)
            {
                // Find deposit spec
                if (!TryFindDepositSpec(DepositCatalog, deposit.DepositId, out var depositSpec))
                {
                    return;
                }

                // Apply work progress
                var workThisTick = harvestNode.WorkRate * DeltaTime;
                harvestNode.WorkProgress += workThisTick / depositSpec.Hardness; // Hardness increases work required

                // Check if work is complete
                if (harvestNode.WorkProgress >= 1f)
                {
                    // Emit resource
                    var resourceAmount = depositSpec.Richness * deposit.CurrentRichness;
                    
                    // TODO: Create ResourceEntity or add to carrier hold
                    // For now, just mark as complete
                    harvestNode.WorkProgress = 0f; // Reset for next cycle

                    // Deplete deposit
                    // Note: Depletion would be handled by a separate system that reads work completion
                }
            }

            private bool TryFindDepositSpec(
                BlobAssetReference<DepositCatalogBlob> catalog,
                FixedString32Bytes depositId,
                out DepositSpec spec)
            {
                spec = default;
                if (!catalog.IsCreated)
                {
                    return false;
                }

                var deposits = catalog.Value.Deposits;
                for (int i = 0; i < deposits.Length; i++)
                {
                    if (deposits[i].Id.Equals(depositId))
                    {
                        spec = deposits[i];
                        return true;
                    }
                }

                return false;
            }
        }
    }
}

