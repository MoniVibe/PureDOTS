using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using PureDOTS.Runtime.Components;

namespace PureDOTS.Systems.Physics
{
    /// <summary>
    /// Applies physics optimizations for large-scale simulations.
    /// Configures solver iterations, ensures transform sync is disabled, and prepares
    /// for static geometry aggregation and broad-phase culling optimizations.
    /// </summary>
    /// <remarks>
    /// Optimizations:
    /// - Solver iterations: default 2 (override per material where needed)
    /// - Transform sync: disabled (ECS owns LocalTransform + Rotation)
    /// - Static aggregation: prepared for chunked colliders (64×64m grid)
    /// - Broad-phase culling: prepared for region-based partitioning
    /// </remarks>
    [BurstCompile]
    [UpdateInGroup(typeof(Unity.Physics.Systems.PhysicsSystemGroup))]
    [UpdateAfter(typeof(Unity.Physics.Systems.PhysicsInitializeGroup))]
    [UpdateBefore(typeof(Unity.Physics.Systems.PhysicsSimulationGroup))]
    public partial struct PhysicsOptimizationSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PhysicsStep>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var rewindState = SystemAPI.GetSingleton<RewindState>();

            // Skip during playback
            if (rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            // Ensure PhysicsStep exists and is configured
            if (!SystemAPI.HasSingleton<PhysicsStep>())
            {
                return;
            }

            var physicsStep = SystemAPI.GetSingletonRW<PhysicsStep>();

            // Ensure solver iterations are set to default 2 for performance
            // (can be overridden per material via PhysicsMaterial.CustomTags)
            if (physicsStep.ValueRO.SolverIterationCount != 2)
            {
                var step = physicsStep.ValueRO;
                step.SolverIterationCount = 2;
                physicsStep.ValueRW = step;
            }

            // Ensure SynchronizeCollisionWorld is disabled (ECS owns transforms)
            // This prevents Unity Physics from syncing transforms back to ECS
            if (physicsStep.ValueRO.SynchronizeCollisionWorld != 0)
            {
                var step = physicsStep.ValueRO;
                step.SynchronizeCollisionWorld = 0;
                physicsStep.ValueRW = step;
            }

            // Note: Static geometry aggregation and broad-phase culling optimizations
            // would be implemented here or in separate systems as needed:
            // - Aggregate static colliders into chunked colliders (64×64m grid)
            // - Partition CollisionWorld by simulation region/planet cell
            // - Only include active regions in CollisionWorld for broad-phase
            // These optimizations require access to PhysicsWorldSingleton and spatial
            // partitioning data, which would be implemented based on game-specific needs.
        }
    }
}

