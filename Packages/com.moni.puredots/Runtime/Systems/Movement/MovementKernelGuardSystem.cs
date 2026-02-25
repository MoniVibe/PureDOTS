using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Movement;
using PureDOTS.Systems;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace PureDOTS.Systems.Movement
{
    /// <summary>
    /// Detects and optionally rolls back external transform writes on MovementKernelOwned entities.
    /// Runs late so the kernel pose remains authoritative for the frame.
    /// </summary>
    [UpdateInGroup(typeof(LateSimulationSystemGroup), OrderLast = true)]
    public partial struct MovementKernelGuardSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<MovementKernelGuardConfig>();
            state.RequireForUpdate<MovementKernelOwned>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var tick = SystemAPI.GetSingleton<TimeState>().Tick;
            var configEntity = SystemAPI.GetSingletonEntity<MovementKernelGuardConfig>();
            var config = state.EntityManager.GetComponentData<MovementKernelGuardConfig>(configEntity);
            var stats = state.EntityManager.GetComponentData<MovementKernelGuardStats>(configEntity);
            var violations = state.EntityManager.GetBuffer<MovementKernelViolation>(configEntity);

            if (stats.LastTick != tick)
            {
                stats.LastTick = tick;
                stats.ViolationsThisTick = 0;
                violations.Clear();
            }

            if (config.Enabled == 0)
            {
                state.EntityManager.SetComponentData(configEntity, stats);
                return;
            }

            foreach (var (transform, pose, entity) in SystemAPI
                         .Query<RefRW<LocalTransform>, RefRO<MovementKernelPose>>()
                         .WithAll<MovementKernelOwned>()
                         .WithEntityAccess())
            {
                if (pose.ValueRO.CapturedTick != tick)
                {
                    continue;
                }

                var posDelta = math.length(transform.ValueRO.Position - pose.ValueRO.Position);
                var scaleDelta = math.abs(transform.ValueRO.Scale - pose.ValueRO.Scale);
                var rotDeltaDeg = ResolveRotationDeltaDegrees(transform.ValueRO.Rotation, pose.ValueRO.Rotation);

                var hasViolation = posDelta > config.PositionEpsilon ||
                                   rotDeltaDeg > config.RotationEpsilonDeg ||
                                   scaleDelta > config.ScaleEpsilon;
                if (!hasViolation)
                {
                    continue;
                }

                stats.ViolationsThisTick++;
                stats.TotalViolations++;

                if (violations.Length < config.MaxViolationsPerTick)
                {
                    violations.Add(new MovementKernelViolation
                    {
                        Entity = entity,
                        Tick = tick,
                        PositionDelta = posDelta,
                        RotationDeltaDeg = rotDeltaDeg,
                        ScaleDelta = scaleDelta
                    });
                }

                if (config.StrictRollback != 0)
                {
                    transform.ValueRW = LocalTransform.FromPositionRotationScale(
                        pose.ValueRO.Position,
                        pose.ValueRO.Rotation,
                        pose.ValueRO.Scale);
                }
            }

            if (config.LogViolations != 0 && stats.ViolationsThisTick > 0)
            {
                global::UnityEngine.Debug.LogWarning(
                    $"[MovementKernelGuard] tick={tick} violations={stats.ViolationsThisTick} total={stats.TotalViolations} strict={config.StrictRollback}.");
            }

            state.EntityManager.SetComponentData(configEntity, stats);
        }

        private static float ResolveRotationDeltaDegrees(in quaternion a, in quaternion b)
        {
            var dot = math.abs(math.dot(a.value, b.value));
            dot = math.clamp(dot, -1f, 1f);
            return math.degrees(2f * math.acos(dot));
        }
    }
}
