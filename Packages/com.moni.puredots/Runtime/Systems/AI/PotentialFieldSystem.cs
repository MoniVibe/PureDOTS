using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Core;
using PureDOTS.Runtime.Spatial;
using PureDOTS.Systems;

namespace PureDOTS.Systems.AI
{
    /// <summary>
    /// Burst-compiled potential field system updating scalar/vector fields across spatial grid.
    /// Each agent limb/ship module emits potential field (attraction to goals, repulsion from threats).
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(AISystemGroup))]
    [UpdateAfter(typeof(SpatialGridBuildSystem))]
    public partial struct PotentialFieldSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SpatialGridState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var tickState = SystemAPI.GetSingleton<TickTimeState>();
            var rewindState = SystemAPI.GetSingleton<RewindState>();
            
            if (rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            // Update potential fields from emitters
            var emitterQuery = state.GetEntityQuery(
                typeof(PotentialFieldEmitter),
                typeof(LocalTransform));

            if (emitterQuery.IsEmpty)
            {
                return;
            }

            var spatialConfig = SystemAPI.GetSingleton<SpatialGridConfig>();
            var spatialState = SystemAPI.GetSingleton<SpatialGridState>();

            var job = new UpdatePotentialFieldsJob
            {
                SpatialConfig = spatialConfig,
                SpatialState = spatialState,
                CurrentTick = tickState.Tick
            };

            state.Dependency = job.ScheduleParallel(emitterQuery, state.Dependency);

            // Compute gradients from scalar fields
            ComputeGradients(ref state, tickState.Tick);
        }

        [BurstCompile]
        private void ComputeGradients(ref SystemState state, uint currentTick)
        {
            // Compute gradient vectors from scalar potential fields
            // In full implementation, would:
            // 1. Read PotentialFieldScalar from spatial grid cells
            // 2. Compute gradient using finite differences
            // 3. Store gradient in PotentialFieldVector components
            // 4. Make gradients available for Mind ECS sampling
        }

        [BurstCompile]
        private partial struct UpdatePotentialFieldsJob : IJobEntity
        {
            public SpatialGridConfig SpatialConfig;
            public SpatialGridState SpatialState;
            public uint CurrentTick;

            public void Execute(
                in PotentialFieldEmitter emitter,
                in LocalTransform transform)
            {
                // Update potential field contribution from this emitter
                // In full implementation, would:
                // 1. Find spatial grid cells within InfluenceRadius
                // 2. Compute field contribution based on distance and coefficients
                // 3. Accumulate contributions into PotentialFieldScalar components
                // 4. Apply behavior field coefficients (social bias, aggression, fear)
            }
        }
    }
}

