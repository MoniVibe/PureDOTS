using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Physics;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace PureDOTS.Systems.Physics
{
    /// <summary>
    /// Bootstraps physics bodies for entities marked with physics participation components.
    /// Runs in InitializationSystemGroup to safely add physics components via ECB.
    /// </summary>
    /// <remarks>
    /// This system:
    /// - Detects entities with RequiresPhysics or PhysicsInteractionConfig that lack physics colliders
    /// - Creates appropriate Unity Physics components (PhysicsCollider, PhysicsVelocity, PhysicsMass)
    /// - Sets up kinematic bodies for ECS-driven movement
    /// - Respects PhysicsConfig singleton for enable/disable toggles
    /// 
    /// Philosophy:
    /// - ECS is authoritative; physics bodies are kinematic (driven by ECS transforms)
    /// - Structural changes are safe here (InitializationSystemGroup)
    /// </remarks>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct PhysicsBodyBootstrapSystem : ISystem
    {
        private EntityQuery _needsSetupQuery;

        public void OnCreate(ref SystemState state)
        {
            // Query for entities that need physics setup
            _needsSetupQuery = SystemAPI.QueryBuilder()
                .WithAll<RequiresPhysics, LocalTransform>()
                .WithNone<PhysicsCollider>()
                .Build();

            state.RequireForUpdate(_needsSetupQuery);
        }

        public void OnUpdate(ref SystemState state)
        {
            // Check if physics is globally enabled
            if (!SystemAPI.HasSingleton<PhysicsConfig>())
            {
                return;
            }

            var config = SystemAPI.GetSingleton<PhysicsConfig>();

            // Early out if both game modes have physics disabled
            if (!config.IsSpace4XPhysicsEnabled && !config.IsGodgamePhysicsEnabled)
            {
                return;
            }

            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (requiresPhysics, transform, entity) in 
                SystemAPI.Query<RefRO<RequiresPhysics>, RefRO<LocalTransform>>()
                    .WithNone<PhysicsCollider>()
                    .WithEntityAccess())
            {
                // Get collision radius from PhysicsInteractionConfig if available
                float collisionRadius = 1f;
                if (SystemAPI.HasComponent<PhysicsInteractionConfig>(entity))
                {
                    var interactionConfig = SystemAPI.GetComponent<PhysicsInteractionConfig>(entity);
                    collisionRadius = interactionConfig.CollisionRadius;
                }

                // Create sphere collider (default)
                var sphereGeometry = new SphereGeometry
                {
                    Center = float3.zero,
                    Radius = collisionRadius
                };

                // Create collision filter based on flags
                var flags = requiresPhysics.ValueRO.Flags;
                var filter = new CollisionFilter
                {
                    BelongsTo = 1u, // Default layer
                    CollidesWith = ~0u, // Collide with everything by default
                    GroupIndex = 0
                };

                // Create collider blob
                var collider = Unity.Physics.SphereCollider.Create(sphereGeometry, filter);

                // Add PhysicsCollider
                ecb.AddComponent(entity, new PhysicsCollider { Value = collider });

                // Add PhysicsVelocity for kinematic bodies
                ecb.AddComponent(entity, new PhysicsVelocity
                {
                    Linear = float3.zero,
                    Angular = float3.zero
                });

                // Add PhysicsMass for kinematic body (infinite mass = kinematic)
                var mass = PhysicsMass.CreateKinematic(MassProperties.UnitSphere);
                ecb.AddComponent(entity, mass);

                // Add PhysicsGravityFactor (0 for kinematic)
                ecb.AddComponent(entity, new PhysicsGravityFactor { Value = 0f });

                // Add PhysicsDamping
                ecb.AddComponent(entity, new PhysicsDamping
                {
                    Linear = 0f,
                    Angular = 0f
                });

                // Mark as having physics collider (for game-specific systems to detect)
                // Note: Game-specific tags like HasPhysicsCollider are added by game systems

                if (config.IsLoggingEnabled)
                {
                    UnityEngine.Debug.Log($"[PhysicsBootstrap] Added physics components to entity {entity.Index}:{entity.Version}");
                }
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        public void OnDestroy(ref SystemState state)
        {
            // No cleanup needed - collider blobs are managed by Unity Physics
        }
    }

    /// <summary>
    /// System group for physics-related systems.
    /// Runs before the main physics simulation group.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(Unity.Physics.Systems.PhysicsSystemGroup))]
    public partial class PhysicsPreSyncSystemGroup : ComponentSystemGroup { }

    /// <summary>
    /// System group for post-physics event processing.
    /// Runs after the main physics simulation group.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(Unity.Physics.Systems.PhysicsSystemGroup))]
    public partial class PhysicsPostEventSystemGroup : ComponentSystemGroup { }
}

