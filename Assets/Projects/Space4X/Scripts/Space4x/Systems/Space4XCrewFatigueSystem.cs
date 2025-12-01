using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Crew;
using PureDOTS.Systems;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Space4X.Systems
{
    /// <summary>
    /// Handles crew fatigue accumulation and recovery at stations.
    /// Fixed-step, Burst-compiled for determinism.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    public partial struct Space4XCrewFatigueSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CrewState>();
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

            var deltaTime = timeState.FixedDeltaTime;
            var hoursPerTick = deltaTime / 3600f; // Convert seconds to hours

            var job = new CrewFatigueJob
            {
                CrewCatalog = crewCatalog.Catalog,
                HoursPerTick = hoursPerTick
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        public partial struct CrewFatigueJob : IJobEntity
        {
            [ReadOnly] public BlobAssetReference<CrewCatalogBlob> CrewCatalog;
            public float HoursPerTick;

            public void Execute(ref CrewState crewState)
            {
                // Find crew spec
                if (!TryFindCrewSpec(CrewCatalog, crewState.CrewSpecId, out var crewSpec))
                {
                    return;
                }

                // Accumulate fatigue
                var fatigueIncrease = crewSpec.FatiguePerHour * HoursPerTick;
                crewState.Fatigue = math.min(1f, crewState.Fatigue + fatigueIncrease);

                // TODO: Check if crew is at station for recovery
                // If at station, reduce fatigue
            }

            private bool TryFindCrewSpec(
                BlobAssetReference<CrewCatalogBlob> catalog,
                FixedString32Bytes crewSpecId,
                out CrewSpec spec)
            {
                spec = default;
                if (!catalog.IsCreated)
                {
                    return false;
                }

                ref var specs = ref catalog.Value.CrewSpecs;
                for (int i = 0; i < specs.Length; i++)
                {
                    if (specs[i].Id.Equals(crewSpecId))
                    {
                        spec = specs[i];
                        return true;
                    }
                }

                return false;
            }
        }
    }
}

