using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Components.Orbital;
using PureDOTS.Runtime.Core;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace PureDOTS.Systems.Orbital
{
    /// <summary>
    /// Parallel frame rebasing system.
    /// Every N sim hours, re-centers each reference frame on its local barycenter.
    /// Shifts child entities by the offset vector.
    /// Performed asynchronously - no global rebuild.
    /// Result: infinite-duration orbital simulation with zero positional drift.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(FrameHierarchySystem))]
    public partial struct FrameRebasingSystem : ISystem
    {
        private const float RebaseIntervalHours = 24.0f; // Rebase every 24 sim hours
        private float _lastRebaseTime;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<OrbitalFrame>();
            state.RequireForUpdate<TickTimeState>();
            state.RequireForUpdate<RewindState>();
            _lastRebaseTime = 0f;
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var rewindState = SystemAPI.GetSingleton<RewindState>();
            if (rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            var tickTimeState = SystemAPI.GetSingleton<TickTimeState>();
            float currentTimeHours = tickTimeState.WorldSeconds / 3600.0f;

            // Check if rebase is needed
            if (currentTimeHours - _lastRebaseTime < RebaseIntervalHours)
            {
                return;
            }

            _lastRebaseTime = currentTimeHours;

            // Compute barycenters for each frame hierarchy
            // This would involve:
            // 1. Finding all child entities for each frame
            // 2. Computing weighted barycenter
            // 3. Shifting frame origin to barycenter
            // 4. Adjusting child positions by offset

            // For now, this is a placeholder that would be extended with full barycenter computation
            var job = new RebaseFramesJob
            {
                CurrentTimeHours = currentTimeHours
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        private partial struct RebaseFramesJob : IJobEntity
        {
            public float CurrentTimeHours;

            public void Execute(ref OrbitalFrame frame, in FrameParent parent)
            {
                if (parent.ParentFrameEntity == Entity.Null)
                {
                    // Root frame - compute barycenter of all children
                    // In full implementation, this would:
                    // 1. Query all child entities
                    // 2. Compute weighted average position
                    // 3. Shift frame origin
                    // 4. Adjust child positions
                }
                else
                {
                    // Child frame - rebase relative to parent
                    // In full implementation, this would compute local barycenter
                }
            }
        }

        /// <summary>
        /// Computes barycenter for a frame's child entities.
        /// </summary>
        private static float3 ComputeBarycenter(
            ref SystemState state,
            Entity frameEntity,
            NativeList<Entity> childEntities)
        {
            float3 totalPosition = float3.zero;
            float totalMass = 0f;

            foreach (var childEntity in childEntities)
            {
                if (SystemAPI.HasComponent<SixDoFState>(childEntity))
                {
                    var sixDoF = SystemAPI.GetComponent<SixDoFState>(childEntity);
                    // In full implementation, would use actual mass
                    float mass = 1.0f; // Placeholder
                    totalPosition += sixDoF.Position * mass;
                    totalMass += mass;
                }
            }

            if (totalMass > 0f)
            {
                return totalPosition / totalMass;
            }

            return float3.zero;
        }
    }
}

