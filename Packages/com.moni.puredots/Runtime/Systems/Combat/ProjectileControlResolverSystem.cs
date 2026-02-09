using PureDOTS.Runtime.Combat;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.LowLevel;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Systems.Combat
{
    /// <summary>
    /// Resolves projectile control requests and applies control steering effects.
    /// Pure data only: no VFX or presentation.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(CombatSystemGroup))]
    [UpdateAfter(typeof(DeflectionResolverSystem))]
    [UpdateBefore(typeof(ProjectileFlightSystem))]
    public partial struct ProjectileControlResolverSystem : ISystem
    {
        private ComponentLookup<ProjectileEntity> _projectileLookup;
        private ComponentLookup<ProjectileControlState> _controlLookup;
        private ComponentLookup<LocalTransform> _transformLookup;
        private ComponentLookup<ProjectileActive> _activeLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
            _projectileLookup = state.GetComponentLookup<ProjectileEntity>();
            _controlLookup = state.GetComponentLookup<ProjectileControlState>();
            _transformLookup = state.GetComponentLookup<LocalTransform>(true);
            _activeLookup = state.GetComponentLookup<ProjectileActive>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            if (timeState.IsPaused)
            {
                return;
            }

            if (!SystemAPI.TryGetSingleton<RewindState>(out var rewindState) || rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            _projectileLookup.Update(ref state);
            _controlLookup.Update(ref state);
            _transformLookup.Update(ref state);
            _activeLookup.Update(ref state);

            bool hasCatalog = SystemAPI.TryGetSingleton<ProjectileCatalog>(out var projectileCatalog) &&
                              projectileCatalog.Catalog.IsCreated;

            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (requests, controller) in SystemAPI.Query<DynamicBuffer<ProjectileControlRequest>>().WithEntityAccess())
            {
                if (requests.Length == 0)
                {
                    continue;
                }

                for (int i = 0; i < requests.Length; i++)
                {
                    var request = requests[i];
                    if (request.Projectile == Entity.Null)
                    {
                        continue;
                    }

                    if (!_projectileLookup.HasComponent(request.Projectile))
                    {
                        continue;
                    }

                    float duration = request.DurationSec > 0f ? request.DurationSec : 0.25f;
                    var controlState = new ProjectileControlState
                    {
                        Controller = controller,
                        Mode = request.Mode,
                        ControlStrength = math.saturate(request.ControlStrength),
                        GuidanceJitter = request.Mode == ProjectileControlMode.Disrupt ? 0.15f : 0f,
                        MaxTurnRateDeg = 0f,
                        ControlTick = request.RequestTick,
                        ControlUntilTime = timeState.WorldSeconds + duration,
                        TargetPosition = request.TargetPosition
                    };

                    if (_controlLookup.HasComponent(request.Projectile))
                    {
                        _controlLookup[request.Projectile] = controlState;
                    }
                    else
                    {
                        ecb.AddComponent(request.Projectile, controlState);
                    }
                }

                requests.Clear();
            }

            foreach (var (projectile, control, entity, active) in
                     SystemAPI.Query<RefRW<ProjectileEntity>, RefRW<ProjectileControlState>, EnabledRefRO<ProjectileActive>>()
                         .WithEntityAccess())
            {
                if (!active.ValueRO)
                {
                    continue;
                }

                if (control.ValueRO.ControlUntilTime > 0f && timeState.WorldSeconds > control.ValueRO.ControlUntilTime)
                {
                    ecb.RemoveComponent<ProjectileControlState>(entity);
                    continue;
                }

                float speed = math.length(projectile.ValueRO.Velocity);
                if (speed <= 1e-4f && hasCatalog)
                {
                    ref var spec = ref FindProjectileSpec(projectileCatalog.Catalog, projectile.ValueRO.ProjectileId);
                    if (!UnsafeRef.IsNull(ref spec) && spec.Speed > 0f)
                    {
                        speed = spec.Speed;
                    }
                }

                if (speed <= 1e-4f)
                {
                    speed = 1f;
                }

                float3 currentDir = math.normalizesafe(projectile.ValueRO.Velocity, new float3(0f, 0f, 1f));
                float3 desiredDir = currentDir;

                if (math.lengthsq(control.ValueRO.TargetPosition) > 1e-6f && _transformLookup.HasComponent(entity))
                {
                    var projPos = _transformLookup[entity].Position;
                    desiredDir = math.normalizesafe(control.ValueRO.TargetPosition - projPos, currentDir);
                }
                else if (projectile.ValueRO.TargetEntity != Entity.Null && _transformLookup.HasComponent(projectile.ValueRO.TargetEntity))
                {
                    var projPos = _transformLookup[entity].Position;
                    var targetPos = _transformLookup[projectile.ValueRO.TargetEntity].Position;
                    desiredDir = math.normalizesafe(targetPos - projPos, currentDir);
                }

                float3 newDir = desiredDir;
                if (control.ValueRO.Mode == ProjectileControlMode.Disrupt)
                {
                    uint seed = projectile.ValueRO.Seed ^ (uint)timeState.Tick ^ (uint)entity.Index;
                    if (seed == 0u)
                    {
                        seed = 1u;
                    }
                    var rng = Unity.Mathematics.Random.CreateFromIndex(seed);
                    float3 jitter = rng.NextFloat3Direction() * math.max(0.01f, control.ValueRO.GuidanceJitter);
                    newDir = math.normalizesafe(currentDir + jitter, currentDir);
                }
                else
                {
                    float blend = math.saturate(control.ValueRO.ControlStrength);
                    newDir = math.normalizesafe(math.lerp(currentDir, desiredDir, blend), desiredDir);

                    if (control.ValueRO.MaxTurnRateDeg > 0f)
                    {
                        float dot = math.clamp(math.dot(currentDir, newDir), -1f, 1f);
                        float angle = math.acos(dot);
                        float maxTurn = math.radians(control.ValueRO.MaxTurnRateDeg) * timeState.DeltaTime;
                        float t = angle <= 1e-5f ? 1f : math.saturate(maxTurn / angle);
                        newDir = math.normalizesafe(math.lerp(currentDir, newDir, t), newDir);
                    }
                }

                projectile.ValueRW.Velocity = newDir * speed;
                if (control.ValueRO.Mode != ProjectileControlMode.Disrupt)
                {
                    projectile.ValueRW.TargetEntity = Entity.Null;
                }
            }
        }

        private static ref ProjectileSpec FindProjectileSpec(
            BlobAssetReference<ProjectileCatalogBlob> catalog,
            FixedString64Bytes projectileId)
        {
            if (!catalog.IsCreated)
            {
                return ref UnsafeRef.Null<ProjectileSpec>();
            }

            ref var projectiles = ref catalog.Value.Projectiles;
            for (int i = 0; i < projectiles.Length; i++)
            {
                ref var projectileSpec = ref projectiles[i];
                if (projectileSpec.Id.Equals(projectileId))
                {
                    return ref projectileSpec;
                }
            }

            return ref UnsafeRef.Null<ProjectileSpec>();
        }
    }
}
