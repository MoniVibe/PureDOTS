using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Crew;
using PureDOTS.Systems;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Space4X.Systems
{
    /// <summary>
    /// Awards XP to crew members based on combat/mining/hauling events.
    /// Fixed-step, Burst-compiled for determinism.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    public partial struct Space4XCrewXpAwardSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CrewXpAward>();
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

            if (!SystemAPI.TryGetSingleton<CrewCatalog>(out var crewCatalog))
            {
                return;
            }

            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var ecbParallel = ecb.AsParallelWriter();

            var job = new CrewXpAwardJob
            {
                CrewCatalog = crewCatalog.Catalog,
                ECB = ecbParallel
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
            state.Dependency.Complete();

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        public partial struct CrewXpAwardJob : IJobEntity
        {
            [ReadOnly] public BlobAssetReference<CrewCatalogBlob> CrewCatalog;
            public EntityCommandBuffer.ParallelWriter ECB;

            public void Execute(
                [EntityIndexInQuery] int entityInQueryIndex,
                Entity entity,
                in CrewXpAward award)
            {
                // Apply XP to crew entity
                if (award.CrewEntity != Entity.Null)
                {
                    // TODO: Update CrewState component with XP
                    // For now, just mark award as processed
                }

                // Destroy award entity
                ECB.DestroyEntity(entityInQueryIndex, entity);
            }
        }
    }
}

