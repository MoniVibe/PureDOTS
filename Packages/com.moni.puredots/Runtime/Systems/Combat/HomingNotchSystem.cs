using PureDOTS.Runtime.Combat;
using PureDOTS.Runtime.Components;
using PureDOTS.Systems;
using Space4X.Knowledge;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Systems.Combat
{
    /// <summary>
    /// For missiles in homing cone: computes lateral "notch" maneuver.
    /// V_lat = normalize(cross(vel_missile, up))
    /// Blends into V_avoid with weight = HomingRisk.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(CombatSystemGroup))]
    [UpdateAfter(typeof(AvoidanceSenseSystem))]
    public partial struct HomingNotchSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
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

            // Find homing projectiles
            var projectileQuery = SystemAPI.QueryBuilder()
                .WithAll<ProjectileEntity, LocalTransform>()
                .Build();

            if (projectileQuery.IsEmpty)
            {
                return;
            }

            var transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
            transformLookup.Update(ref state);

            var job = new HomingNotchJob
            {
                TransformLookup = transformLookup
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        public partial struct HomingNotchJob : IJobEntity
        {
            [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;

            void Execute(
                Entity entity,
                ref HazardAvoidanceState avoidanceState,
                in ProjectileEntity projectile,
                in LocalTransform transform)
            {
                // Only apply to homing projectiles
                // TODO: Check ProjectileSpec to confirm homing type
                if (projectile.TargetEntity == Entity.Null)
                {
                    return;
                }

                // Get missile velocity
                float3 missileVel = projectile.Velocity;
                float velLength = math.length(missileVel);
                if (velLength < 1e-6f)
                {
                    return;
                }

                float3 missileDir = math.normalize(missileVel);

                // Compute lateral vector (perpendicular to velocity in horizontal plane)
                float3 up = math.up();
                float3 lateral = math.cross(missileDir, up);
                float lateralLength = math.length(lateral);

                if (lateralLength < 1e-6f)
                {
                    // Velocity is vertical, use right vector instead
                    lateral = math.cross(missileDir, math.right());
                    lateralLength = math.length(lateral);
                }

                if (lateralLength > 1e-6f)
                {
                    lateral = math.normalize(lateral);

                    // Blend lateral notch into avoidance vector
                    // Weight based on homing risk (simplified - would sample from hazard grid)
                    float homingRisk = avoidanceState.AvoidanceUrgency * 0.5f; // Reduced weight for notch
                    float3 notchVector = lateral * homingRisk;

                    // Combine with existing avoidance
                    float3 combinedAvoidance = avoidanceState.CurrentAdjustment + notchVector;
                    float combinedLength = math.length(combinedAvoidance);

                    if (combinedLength > 1e-6f)
                    {
                        avoidanceState.CurrentAdjustment = math.normalize(combinedAvoidance) * math.min(combinedLength, 1f);
                    }
                }
            }
        }
    }
}

