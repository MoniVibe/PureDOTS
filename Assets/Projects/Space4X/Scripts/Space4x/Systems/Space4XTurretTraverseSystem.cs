using PureDOTS.Runtime.Combat;
using PureDOTS.Runtime.Components;
using PureDOTS.Systems;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Space4X.Systems
{
    /// <summary>
    /// Handles turret traversal: rotation limits, muzzle socket resolution, target tracking.
    /// Fixed-step, Burst-compiled for determinism.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(CombatSystemGroup))]
    [UpdateBefore(typeof(Space4XWeaponFireSystem))]
    public partial struct Space4XTurretTraverseSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TurretState>();
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            if (timeState.IsPaused)
            {
                return;
            }

            var rewindState = SystemAPI.GetSingleton<RewindState>();
            if (rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            if (!SystemAPI.TryGetSingleton<TurretCatalog>(out var turretCatalog))
            {
                return;
            }

            var deltaTime = timeState.FixedDeltaTime;

            var job = new TurretTraverseJob
            {
                TurretCatalog = turretCatalog.Catalog,
                DeltaTime = deltaTime
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        public partial struct TurretTraverseJob : IJobEntity
        {
            [ReadOnly] public BlobAssetReference<TurretCatalogBlob> TurretCatalog;
            public float DeltaTime;

            public void Execute(
                ref TurretState turretState,
                in LocalTransform transform,
                in WeaponMount weaponMount)
            {
                // Find turret spec
                if (!TryFindTurretSpec(TurretCatalog, turretState.TurretId, out var turretSpec))
                {
                    return;
                }

                // Calculate desired rotation toward target
                if (weaponMount.TargetEntity != Entity.Null)
                {
                    var targetPos = weaponMount.TargetPosition;
                    var toTarget = targetPos - transform.Position;
                    var distance = math.length(toTarget);

                    if (distance > 0.001f)
                    {
                        var desiredForward = math.normalize(toTarget);
                        var desiredRotation = quaternion.LookRotationSafe(desiredForward, math.up());

                        // Apply arc limits
                        desiredRotation = ApplyArcLimits(
                            transform.Rotation,
                            desiredRotation,
                            turretSpec.ArcYawDeg,
                            turretSpec.ArcPitchDeg);

                        turretState.TargetRotation = desiredRotation;
                    }
                }

                // Rotate toward target rotation
                var currentRot = turretState.CurrentRotation;
                var targetRot = turretState.TargetRotation;
                var maxRotSpeed = math.radians(turretSpec.TraverseDegPerS);
                var maxElevSpeed = math.radians(turretSpec.ElevDegPerS);

                // Slerp toward target with speed limits
                turretState.CurrentRotation = math.slerp(currentRot, targetRot, math.min(1f, maxRotSpeed * DeltaTime));

                // Update muzzle position and forward direction
                turretState.MuzzlePosition = transform.Position;
                turretState.MuzzleForward = math.mul(turretState.CurrentRotation, math.forward());
            }

            private bool TryFindTurretSpec(
                BlobAssetReference<TurretCatalogBlob> catalog,
                FixedString32Bytes turretId,
                out TurretSpec spec)
            {
                spec = default;
                if (!catalog.IsCreated)
                {
                    return false;
                }

                var turrets = catalog.Value.Turrets;
                for (int i = 0; i < turrets.Length; i++)
                {
                    if (turrets[i].Id.Equals(turretId))
                    {
                        spec = turrets[i];
                        return true;
                    }
                }

                return false;
            }

            private quaternion ApplyArcLimits(
                quaternion currentRotation,
                quaternion desiredRotation,
                float arcYawDeg,
                float arcPitchDeg)
            {
                // Simple arc limiting: clamp yaw and pitch separately
                // TODO: Implement proper arc limits
                return desiredRotation;
            }
        }
    }
}

