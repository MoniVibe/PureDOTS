using PureDOTS.Runtime.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Runtime.Physics
{
    /// <summary>
    /// Hierarchical mass aggregation system using parallel reduction.
    /// Aggregates mass from child entities (cargo, inventory items) into parent entities (ships, containers).
    /// Uses IJobChunk with NativeParallelMultiHashMap for concurrent summation.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(InventoryMassSystem))]
    public partial struct MassAggregationSystem : ISystem
    {
        private EntityQuery _massQuery;
        private EntityQuery _parentQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TickTimeState>();
            state.RequireForUpdate<RewindState>();

            _massQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<MassComponent>(),
                ComponentType.ReadOnly<LocalTransform>(),
                ComponentType.ReadOnly<Parent>()
            );

            _parentQuery = state.GetEntityQuery(
                ComponentType.ReadWrite<MassComponent>(),
                ComponentType.ReadOnly<LocalTransform>()
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

            if (_massQuery.CalculateEntityCount() == 0)
            {
                return;
            }

            // Accumulate mass from children into parents
            var accumulatorMap = new NativeParallelMultiHashMap<Entity, MassAccumulator>(1024, Allocator.TempJob);

            var accumulateJob = new AccumulateMassJob
            {
                AccumulatorMap = accumulatorMap.AsParallelWriter(),
                TransformLookup = state.GetComponentLookup<LocalTransform>(true),
                MassLookup = state.GetComponentLookup<MassComponent>(true)
            };

            state.Dependency = accumulateJob.ScheduleParallel(_massQuery, state.Dependency);

            // Reduce accumulated mass into parent entities
            var reduceJob = new ReduceMassJob
            {
                AccumulatorMap = accumulatorMap,
                MassLookup = state.GetComponentLookup<MassComponent>(false)
            };

            state.Dependency = reduceJob.Schedule(_parentQuery, state.Dependency);
            state.Dependency.Complete();

            accumulatorMap.Dispose();
        }

        [BurstCompile]
        private struct AccumulateMassJob : IJobChunk
        {
            [NativeDisableParallelForRestriction]
            public NativeParallelMultiHashMap<Entity, MassAccumulator>.ParallelWriter AccumulatorMap;

            [ReadOnly]
            public ComponentLookup<LocalTransform> TransformLookup;

            [ReadOnly]
            public ComponentLookup<MassComponent> MassLookup;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var massComponents = chunk.GetNativeArray(ref chunk.GetRequiredComponentTypeHandle<MassComponent>(true));
                var transforms = chunk.GetNativeArray(ref chunk.GetRequiredComponentTypeHandle<LocalTransform>(true));
                var parents = chunk.GetNativeArray(ref chunk.GetRequiredComponentTypeHandle<Parent>(true));
                var entities = chunk.GetEntityArray();

                for (int i = 0; i < chunk.Count; i++)
                {
                    var entity = entities[i];
                    var mass = massComponents[i];
                    var transform = transforms[i];
                    var parent = parents[i];

                    if (parent.Value == Entity.Null)
                    {
                        continue;
                    }

                    // Calculate world position relative to parent
                    var worldPos = transform.Position;
                    if (TransformLookup.HasComponent(parent.Value))
                    {
                        var parentTransform = TransformLookup[parent.Value];
                        worldPos -= parentTransform.Position;
                    }

                    var accumulator = new MassAccumulator();
                    accumulator.Add(mass, worldPos);

                    AccumulatorMap.TryAdd(parent.Value, accumulator);
                }
            }
        }

        [BurstCompile]
        private struct ReduceMassJob : IJobChunk
        {
            [ReadOnly]
            public NativeParallelMultiHashMap<Entity, MassAccumulator> AccumulatorMap;

            public ComponentLookup<MassComponent> MassLookup;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var entities = chunk.GetEntityArray();
                var massComponents = chunk.GetNativeArray(ref chunk.GetRequiredComponentTypeHandle<MassComponent>(false));

                for (int i = 0; i < chunk.Count; i++)
                {
                    var entity = entities[i];
                    if (!AccumulatorMap.ContainsKey(entity))
                    {
                        continue;
                    }

                    // Combine all accumulators for this parent entity
                    var combined = new MassAccumulator();
                    if (MassLookup.HasComponent(entity))
                    {
                        var existingMass = MassLookup[entity];
                        combined.Add(existingMass, float3.zero);
                    }

                    // Sum all child contributions
                    if (AccumulatorMap.TryGetFirstValue(entity, out var accumulator, out var iterator))
                    {
                        do
                        {
                            combined.Combine(accumulator);
                        }
                        while (AccumulatorMap.TryGetNextValue(out accumulator, ref iterator));
                    }

                    // Update parent mass component
                    var finalMass = combined.ToMassComponent();
                    massComponents[i] = finalMass;
                }
            }
        }
    }
}

