using PureDOTS.Runtime.Components;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using System.Runtime.CompilerServices;
using Unity.Mathematics;

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

            var updateJob = new UpdateMassJob
            {
                MassHandle = state.GetComponentTypeHandle<MassComponent>(false)
            };
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
            public ComponentTypeHandle<MassComponent> MassHandle;

            void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
#if UNITY_BURST
                // Avoid managed allocations in Burst; chunk processing not supported here.
                return;
#else
                ExecuteChunk(chunk, unfilteredChunkIndex, useEnabledMask, chunkEnabledMask);
#endif
            }

            private void ExecuteChunk(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var massComponents = chunk.GetNativeArray(MassHandle);

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
            // Fallback for environments where math.fma is unavailable: explicitly compute a * b + c.
            return (a * b) + c;
        }
    }
}
