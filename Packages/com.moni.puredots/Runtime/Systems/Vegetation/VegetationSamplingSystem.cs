using PureDOTS.Environment;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Time;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using PureDOTS.Systems.Environment;

namespace PureDOTS.Systems.Vegetation
{
    /// <summary>
    /// Statistical sampling system for vegetation growth.
    /// Updates representative patches (10K samples) instead of all entities.
    /// Replicates patch updates to other plants probabilistically.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(VegetationSystemGroup))]
    [UpdateBefore(typeof(VegetationGrowthSystem))]
    public partial struct VegetationSamplingSystem : ISystem
    {
        private const int PatchSize = 8; // 8×8 cells per patch
        private const int MaxSamples = 10000; // Maximum representative samples

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            var rewindState = SystemAPI.GetSingleton<RewindState>();

            if (rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            if (timeState.IsPaused)
            {
                return;
            }

            // Check temporal LOD
            var lodConfig = SystemAPI.HasSingleton<TemporalLODConfig>()
                ? SystemAPI.GetSingleton<TemporalLODConfig>()
                : new TemporalLODConfig { VegetationDivisor = 20 };

            if (!TemporalLODHelpers.ShouldUpdate(timeState.Tick, lodConfig.VegetationDivisor))
            {
                return;
            }

            // Query vegetation entities
            var vegetationQuery = SystemAPI.QueryBuilder()
                .WithAll<VegetationId, VegetationLifecycle, LocalTransform>()
                .WithNone<VegetationDeadTag, PlaybackGuardTag>()
                .Build();

            if (vegetationQuery.IsEmpty)
            {
                return;
            }

            // Group vegetation into patches
            var patches = new NativeHashMap<int2, Entity>(MaxSamples, Allocator.TempJob);
            var patchCounts = new NativeHashMap<int2, byte>(MaxSamples, Allocator.TempJob);

            foreach (var (transform, entity) in SystemAPI.Query<RefRO<LocalTransform>>().WithAll<VegetationId>().WithEntityAccess())
            {
                var pos = transform.ValueRO.Position;
                var patchCoord = new int2(
                    (int)math.floor(pos.x / PatchSize),
                    (int)math.floor(pos.z / PatchSize)
                );

                if (!patches.ContainsKey(patchCoord))
                {
                    patches.Add(patchCoord, entity);
                    patchCounts.Add(patchCoord, 1);
                }
                else
                {
                    patchCounts[patchCoord] = (byte)(patchCounts[patchCoord] + 1);
                }
            }

            // Update representative samples
            var representativeEntities = patches.GetValueArray(Allocator.TempJob);
            if (representativeEntities.Length > 0)
            {
                var job = new UpdateRepresentativeSamplesJob
                {
                    RepresentativeEntities = representativeEntities,
                    PatchCoords = patches.GetKeyArray(Allocator.TempJob),
                    PatchCounts = patchCounts,
                    DeltaTime = timeState.FixedDeltaTime * lodConfig.VegetationDivisor,
                    CurrentTick = timeState.Tick
                };

                state.Dependency = job.ScheduleParallel(representativeEntities.Length, 64, state.Dependency);
            }

            // Replicate updates to patch members probabilistically
            // This would be implemented in a follow-up job that reads patch updates
            // and applies them to other entities in the same patch with probability
        }

        [BurstCompile]
        private struct UpdateRepresentativeSamplesJob : IJobFor
        {
            [ReadOnly] public NativeArray<Entity> RepresentativeEntities;
            [ReadOnly] public NativeArray<int2> PatchCoords;
            [ReadOnly] public NativeHashMap<int2, byte> PatchCounts;
            public float DeltaTime;
            public uint CurrentTick;

            public void Execute(int index)
            {
                var entity = RepresentativeEntities[index];
                var patchCoord = PatchCoords[index];
                var sampleCount = PatchCounts[patchCoord];

                // Representative sample update logic would go here
                // For now, this is a placeholder structure
                // In full implementation, would update FloraState and replicate to patch members
            }
        }
    }
}

