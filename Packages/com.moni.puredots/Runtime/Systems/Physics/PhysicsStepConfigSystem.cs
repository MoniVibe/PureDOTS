using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using PureDOTS.Runtime.Components;

namespace PureDOTS.Systems.Physics
{
    /// <summary>
    /// Configures Unity Physics for deterministic simulation with fixed timestep.
    /// Ensures PhysicsStep singleton exists with correct settings for determinism.
    /// </summary>
    /// <remarks>
    /// Determinism requirements:
    /// - Fixed Δt only (no variable step) - uses TimeState.FixedDeltaTime
    /// - SimulationType.UnityPhysics (not Auto)
    /// - Solver iterations tuned for performance (default 2, override per material)
    /// - Burst/math versions locked (Entities 1.4.2 / Burst 1.8.24)
    /// - Platform parity (same CPU architecture)
    /// </remarks>
    [BurstCompile]
    [UpdateInGroup(typeof(TimeSystemGroup))]
    [UpdateAfter(typeof(TimeTickSystem))]
    public partial struct PhysicsStepConfigSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            var rewindState = SystemAPI.GetSingleton<RewindState>();

            // Skip configuration during playback (physics state reconstructed from ECS)
            if (rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            // Ensure PhysicsStep singleton exists
            if (!SystemAPI.HasSingleton<PhysicsStep>())
            {
                var entity = state.EntityManager.CreateEntity();
                state.EntityManager.AddComponent<PhysicsStep>(entity);
            }

            var physicsStep = SystemAPI.GetSingletonRW<PhysicsStep>();

            // Configure for deterministic Unity Physics simulation
            physicsStep.ValueRW = new PhysicsStep
            {
                SimulationType = SimulationType.UnityPhysics, // Explicit, not Auto
                Gravity = new float3(0f, -9.81f, 0f), // Standard gravity
                SolverIterationCount = 2, // Default 2 for performance (override per material)
                SolverStabilizationHeuristicSettings = Solver.StabilizationHeuristicSettings.Default,
                MultiThreaded = 1, // Enable multi-threading for Burst jobs
                CollisionTolerance = CollisionWorld.DefaultCollisionTolerance,
                SynchronizeCollisionWorld = 0, // ECS owns transforms, no sync needed
                IncrementalDynamicBroadphase = false,
                IncrementalStaticBroadphase = false
            };

            // Note: The actual timestep used by physics comes from FixedStepSimulationSystemGroup.Timestep,
            // which is synchronized with TimeState.FixedDeltaTime by GameplayFixedStepSyncSystem.
            // Unity Physics StepPhysicsWorld system reads SystemAPI.Time.DeltaTime which uses this fixed timestep.
        }
    }
}

