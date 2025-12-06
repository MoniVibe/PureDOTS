using PureDOTS.Runtime.Combat;
using PureDOTS.Runtime.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections.LowLevel.Unsafe;

namespace PureDOTS.Systems.Combat
{
    /// <summary>
    /// 6-DoF projectile motion integration with aerodynamic forces.
    /// From Verberne 2020 & HPC 2023: split translation and rotation integration.
    /// Runs in CombatSystemGroup before collision detection.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(CombatSystemGroup))]
    [UpdateBefore(typeof(ProjectileCollisionSystem))]
    public partial struct ProjectileMotionSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Projectile>();
            state.RequireForUpdate<TimeState>();
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

            var timeState = SystemAPI.GetSingleton<TimeState>();
            float dt = timeState.DeltaTime;

            if (!SystemAPI.TryGetSingleton<ProjectileArchetypeCatalog>(out var archetypeCatalog) ||
                !archetypeCatalog.Catalog.IsCreated)
            {
                return;
            }

            // Optional gravity (use SphericalHarmonicGravity or simple float3 Gravity)
            float3 gravity = float3.zero;
            if (SystemAPI.TryGetSingleton<Gravity>(out var gravityComp))
            {
                gravity = gravityComp.Value;
            }

            var job = new ProjectileMotionJob
            {
                DeltaTime = dt,
                Gravity = gravity,
                ArchetypeCatalog = archetypeCatalog.Catalog
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        public partial struct ProjectileMotionJob : IJobEntity
        {
            [ReadOnly] public float DeltaTime;
            [ReadOnly] public float3 Gravity;
            [ReadOnly] public BlobAssetReference<ProjectileArchetypeCatalogBlob> ArchetypeCatalog;

            public void Execute(
                [ChunkIndexInQuery] int chunkIndex,
                ref Projectile projectile,
                ref LocalTransform transform,
                ref ProjectileMetadata metadata)
            {
                // Get projectile spec from archetype
                if (projectile.ArchetypeId >= ArchetypeCatalog.Value.Archetypes.Length)
                {
                    return; // Invalid archetype ID
                }

                ref var spec = ref ArchetypeCatalog.Value.Archetypes[projectile.ArchetypeId];

                // Update age
                metadata.Age += DeltaTime;
                projectile.Lifetime -= DeltaTime;

                // Translation integration: pos += vel * dt
                float3 deltaPos = projectile.Velocity * DeltaTime;
                projectile.Position += deltaPos;
                transform.Position = projectile.Position;
                metadata.DistanceTraveled += math.length(deltaPos);

                // Update previous position for collision detection
                metadata.PrevPos = projectile.Position - deltaPos;

                // Rotation integration: rot = mul(rot, quaternion.AxisAngle(angularAxis, angularVel * dt))
                if (math.lengthsq(spec.AngularVelocity) > 1e-6f)
                {
                    float3 angularAxis = math.normalize(spec.AngularVelocity);
                    float angularSpeed = math.length(spec.AngularVelocity);
                    quaternion deltaRot = quaternion.AxisAngle(angularAxis, angularSpeed * DeltaTime);
                    projectile.Rotation = math.mul(projectile.Rotation, deltaRot);
                    transform.Rotation = projectile.Rotation;
                }

                // Aerodynamic forces (drag, lift)
                float3 vel = projectile.Velocity;
                float speed = math.length(vel);
                if (speed > 1e-6f)
                {
                    float3 velDir = vel / speed;

                    // Drag: -vel * (dragCoeff * length(vel))
                    float3 drag = -vel * (spec.DragCoeff * speed);

                    // Lift: cross(angularAxis, vel) * liftCoeff
                    float3 lift = float3.zero;
                    if (math.lengthsq(spec.AngularVelocity) > 1e-6f && spec.LiftCoeff > 0f)
                    {
                        float3 angularAxis = math.normalize(spec.AngularVelocity);
                        lift = math.cross(angularAxis, vel) * spec.LiftCoeff;
                    }

                    // Apply forces: vel += (drag + lift + gravity) * dt
                    float3 acceleration = (drag + lift + Gravity) * DeltaTime;
                    projectile.Velocity += acceleration;
                }
                else
                {
                    // Apply gravity even if velocity is zero
                    projectile.Velocity += Gravity * DeltaTime;
                }
            }
        }
    }

    /// <summary>
    /// Simple gravity component for projectiles (fallback if SphericalHarmonicGravity not available).
    /// </summary>
    public struct Gravity : IComponentData
    {
        public float3 Value;
    }
}

