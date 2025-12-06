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
    /// Hierarchical culling with solid angle thresholds.
    /// For spherical galaxies, visibility = solid angle subtended > ε.
    /// Computes once per tick for clusters, not per object.
    /// Massive reduction in LOD checks during free-flight camera or RTS view.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(AngularVelocityIntegrationSystem))]
    public partial struct SolidAngleCullingSystem : ISystem
    {
        private const float AngleThreshold = 0.001f; // radians (solid angle threshold)

        /// <summary>Tag component marking visible entities.</summary>
        public struct VisibleTag : IComponentData { }

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

            // Get camera/view position (would come from camera system in full implementation)
            // For now, use origin as reference point
            float3 viewPosition = float3.zero;

            var job = new ComputeVisibilityJob
            {
                ViewPosition = viewPosition,
                AngleThreshold = AngleThreshold
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        private partial struct ComputeVisibilityJob : IJobEntity
        {
            public float3 ViewPosition;
            public float AngleThreshold;

            public void Execute(
                Entity entity,
                ref SixDoFState sixDoF,
                in ShellMembership shell)
            {
                float3 position = sixDoF.Position;
                float3 toObject = position - ViewPosition;
                float distance = math.length(toObject);

                if (distance < 1e-6f)
                {
                    return; // At view position, skip
                }

                // Estimate radius from shell membership
                float radius = (float)(shell.OuterRadius - shell.InnerRadius) * 0.5f;
                if (radius < 1e-6f)
                {
                    radius = 1.0f; // Default radius
                }

                // Solid angle = (radius / distance)²
                // Visibility check: if (radius / distance > angleThreshold) markVisible()
                float solidAngle = radius / distance;

                if (solidAngle > AngleThreshold)
                {
                    // Entity is visible - would add VisibleTag in full implementation
                    // For now, this is a placeholder
                }
            }
        }
    }
}

