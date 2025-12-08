using PureDOTS.Runtime.Components;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Physics
{
    /// <summary>
    /// LOD aggregation system for massive entities.
    /// Groups entities of the same material/resource type within chunks.
    /// Replaces per-entity updates with aggregate physics proxies.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PhysicsIntegrationSystem))]
    public partial struct LODAggregationSystem : ISystem
    {
        private EntityQuery _aggregateQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TickTimeState>();
            state.RequireForUpdate<RewindState>();

            _aggregateQuery = state.GetEntityQuery(
                ComponentType.ReadWrite<MassComponent>(),
                ComponentType.ReadOnly<AggregateProxyTag>(),
                ComponentType.ReadOnly<MaterialId>()
            );
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var rewindState = SystemAPI.GetSingleton<RewindState>();
            if (rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            if (_aggregateQuery.CalculateEntityCount() == 0)
            {
                return;
            }

            // Group entities by material/resource type within chunks
            var aggregateMap = new NativeParallelMultiHashMap<FixedString64Bytes, MassAccumulator>(1024, Allocator.TempJob);

            var aggregateJob = new AggregateMassJob
            {
                AggregateMap = aggregateMap.AsParallelWriter(),
                MassHandle = state.GetComponentTypeHandle<MassComponent>(true),
                MaterialHandle = state.GetComponentTypeHandle<MaterialId>(true)
            };

            state.Dependency = aggregateJob.ScheduleParallel(_aggregateQuery, state.Dependency);

            // Create or update mass proxies
            var proxyJob = new CreateProxyJob
            {
                AggregateMap = aggregateMap,
                MassHandle = state.GetComponentTypeHandle<MassComponent>(false),
                MaterialHandle = state.GetComponentTypeHandle<MaterialId>(true)
            };

            state.Dependency = proxyJob.Schedule(_aggregateQuery, state.Dependency);
            state.Dependency.Complete();

            aggregateMap.Dispose();
        }

        [BurstCompile]
        private struct AggregateMassJob : IJobChunk
        {
            [NativeDisableParallelForRestriction]
            public NativeParallelMultiHashMap<FixedString64Bytes, MassAccumulator>.ParallelWriter AggregateMap;

            public ComponentTypeHandle<MassComponent> MassHandle;
            [ReadOnly]
            public ComponentTypeHandle<MaterialId> MaterialHandle;

            void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                ExecuteChunk(chunk, unfilteredChunkIndex, useEnabledMask, chunkEnabledMask);
            }

            private void ExecuteChunk(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var massComponents = chunk.GetNativeArray(MassHandle);
                var materialIds = chunk.GetNativeArray(MaterialHandle);

                // Group by material ID within chunk
                for (int i = 0; i < chunk.Count; i++)
                {
                    var materialId = materialIds[i];
                    var mass = massComponents[i];

                    var accumulator = new MassAccumulator();
                    accumulator.Add(mass, float3.zero);

                    AggregateMap.Add(materialId.Value, accumulator);
                }
            }
        }

        [BurstCompile]
        private struct CreateProxyJob : IJobChunk
        {
            [ReadOnly]
            public NativeParallelMultiHashMap<FixedString64Bytes, MassAccumulator> AggregateMap;

            public ComponentTypeHandle<MassComponent> MassHandle;
            [ReadOnly]
            public ComponentTypeHandle<MaterialId> MaterialHandle;

            void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                ExecuteChunk(chunk, unfilteredChunkIndex, useEnabledMask, chunkEnabledMask);
            }

            private void ExecuteChunk(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var massComponents = chunk.GetNativeArray(MassHandle);
                var materialIds = chunk.GetNativeArray(MaterialHandle);

                for (int i = 0; i < chunk.Count; i++)
                {
                    var materialId = materialIds[i];

                    if (!AggregateMap.ContainsKey(materialId.Value))
                    {
                        continue;
                    }

                    // Combine all accumulators for this material type
                    var combined = new MassAccumulator();
                    int count = 0;

                    if (AggregateMap.TryGetFirstValue(materialId.Value, out var accumulator, out var iterator))
                    {
                        do
                        {
                            combined.Combine(accumulator);
                            count++;
                        }
                        while (AggregateMap.TryGetNextValue(out accumulator, ref iterator));
                    }

                    // Create aggregate mass proxy
                    var proxyMass = combined.ToMassComponent();
                    var scaleFactor = count > 0 ? 1f / count : 1f;

                    // Update mass component with aggregate values
                    massComponents[i] = proxyMass;

                    // Add or update MassProxy component
                    // Note: This would require ECB in a real implementation
                    // For now, we just update the mass component
                }
            }
        }
    }
}

