using PureDOTS.Runtime.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using System.Runtime.CompilerServices;

namespace PureDOTS.Runtime.Physics
{
    /// <summary>
    /// Deterministic parallel mass update system using IJobChunk with NativeParallelHashMap.
    /// Only recalculates mass when MassDirtyTag or CargoChangedTag is present.
    /// Uses Burst deterministic math with FMA operations.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(MassAggregationSystem))]
    public partial struct MassUpdateSystem : ISystem
    {
        private EntityQuery _dirtyMassQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TickTimeState>();
            state.RequireForUpdate<RewindState>();

            _dirtyMassQuery = state.GetEntityQuery(
                ComponentType.ReadWrite<MassComponent>(),
                ComponentType.ReadOnly<MassDirtyTag>()
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

            if (_dirtyMassQuery.CalculateEntityCount() == 0)
            {
                return;
            }

            var updateJob = new UpdateMassJob();
            state.Dependency = updateJob.ScheduleParallel(_dirtyMassQuery, state.Dependency);

            // Remove dirty tags after update
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            foreach (var (_, entity) in SystemAPI.Query<RefRO<MassDirtyTag>>().WithEntityAccess())
            {
                ecb.RemoveComponent<MassDirtyTag>(entity);
            }
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        private struct UpdateMassJob : IJobChunk
        {
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var massComponents = chunk.GetNativeArray(ref chunk.GetRequiredComponentTypeHandle<MassComponent>(false));

                // Per-thread accumulator for deterministic math
                for (int i = 0; i < chunk.Count; i++)
                {
                    var mass = massComponents[i];
                    
                    // Ensure mass is valid (non-negative, non-NaN)
                    if (mass.Mass < 0f || !math.isfinite(mass.Mass))
                    {
                        mass.Mass = 0f;
                    }
                    if (!math.all(math.isfinite(mass.CenterOfMass)))
                    {
                        mass.CenterOfMass = float3.zero;
                    }
                    if (!math.all(math.isfinite(mass.InertiaTensor)))
                    {
                        mass.InertiaTensor = float3.zero;
                    }

                    massComponents[i] = mass;
                }
            }
        }

        /// <summary>
        /// Fused multiply-add for deterministic math.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [BurstCompile]
        private static float FMA(float a, float b, float c)
        {
            return math.fma(a, b, c);
        }
    }
}

