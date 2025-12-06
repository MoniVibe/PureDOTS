using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Physics;

namespace PureDOTS.Systems.Physics
{
    /// <summary>
    /// Syncs MassComponent to Unity Physics PhysicsMass each tick.
    /// Updates physics mass properties from ECS mass data deterministically.
    /// </summary>
    /// <remarks>
    /// Runs after PhysicsInitializeGroup to update mass properties before simulation.
    /// Handles MassDirtyTag for efficient dirty tracking.
    /// Calculates inverse mass and inverse inertia from MassComponent data.
    /// </remarks>
    [BurstCompile]
    [UpdateInGroup(typeof(Unity.Physics.Systems.PhysicsSystemGroup))]
    [UpdateAfter(typeof(Unity.Physics.Systems.PhysicsInitializeGroup))]
    [UpdateBefore(typeof(Unity.Physics.Systems.PhysicsSimulationGroup))]
    public partial struct CustomMassSyncSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // Query for entities with both MassComponent and PhysicsMass
            state.RequireForUpdate<PhysicsStep>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var rewindState = SystemAPI.GetSingleton<RewindState>();

            // Skip during playback (physics state reconstructed from ECS)
            if (rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            // Sync mass for all entities with MassComponent and PhysicsMass
            foreach (var (mass, physMass) in SystemAPI.Query<RefRO<MassComponent>, RefRW<PhysicsMass>>())
            {
                var massValue = mass.ValueRO.Mass;
                var minMass = 0.0001f; // Prevent division by zero
                var clampedMass = math.max(minMass, massValue);
                var inverseMass = 1f / clampedMass;

                // Convert diagonalized inertia tensor to inverse inertia
                // PhysicsMass stores inverse inertia as float3 diagonal
                var inertia = mass.ValueRO.InertiaTensor;
                var minInertia = 0.0001f; // Prevent division by zero
                var clampedInertia = math.max(inertia, new float3(minInertia));
                var inverseInertia = 1f / clampedInertia;

                // Update PhysicsMass with new values
                // Preserve Transform (center of mass offset) and AngularExpansionFactor
                var currentMass = physMass.ValueRO;
                physMass.ValueRW = new PhysicsMass
                {
                    Transform = new RigidTransform
                    {
                        pos = mass.ValueRO.CenterOfMass,
                        rot = quaternion.identity
                    },
                    InverseMass = inverseMass,
                    InverseInertia = inverseInertia,
                    AngularExpansionFactor = currentMass.AngularExpansionFactor
                };
            }

            // Remove MassDirtyTag after sync (if present)
            // Note: Using ECB requires non-Burst code, so OnUpdate is not Burst-compiled
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (_, entity) in SystemAPI.Query<RefRO<MassDirtyTag>>().WithEntityAccess())
            {
                ecb.RemoveComponent<MassDirtyTag>(entity);
            }
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}

