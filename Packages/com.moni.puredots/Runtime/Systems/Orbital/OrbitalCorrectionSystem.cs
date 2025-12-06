using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Components.Orbital;
using PureDOTS.Runtime.Core;
using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace PureDOTS.Systems.Orbital
{
    /// <summary>
    /// Event-driven orbital correction system.
    /// Only recomputes gravitational interactions when:
    /// - A body enters/leaves another's sphere of influence, or
    /// - Player interaction/miracle applies delta-v
    /// Otherwise, entities propagate analytically.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(AngularVelocityIntegrationSystem))]
    public partial struct OrbitalCorrectionSystem : ISystem
    {
        private const float SphereOfInfluenceThreshold = 1000f; // meters

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SixDoFState>();
            state.RequireForUpdate<TickTimeState>();
            state.RequireForUpdate<RewindState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var rewindState = SystemAPI.GetSingleton<RewindState>();
            if (rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            // Process entities marked as dirty
            var job = new CorrectOrbitalTrajectoryJob
            {
                SphereOfInfluenceThreshold = SphereOfInfluenceThreshold
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        private partial struct CorrectOrbitalTrajectoryJob : IJobEntity
        {
            public float SphereOfInfluenceThreshold;

            public void Execute(ref SixDoFState sixDoF, in OrbitalDirtyTag dirtyTag)
            {
                // When an entity is marked dirty, recompute its orbital parameters
                // This would involve:
                // 1. Finding nearby massive bodies
                // 2. Computing new orbital elements
                // 3. Updating velocity to match new orbit
                // For now, this is a placeholder that would be extended with full orbital mechanics

                // Example: if entity enters sphere of influence, adjust velocity
                // In full implementation, this would compute new orbital elements
            }
        }

    }
}

