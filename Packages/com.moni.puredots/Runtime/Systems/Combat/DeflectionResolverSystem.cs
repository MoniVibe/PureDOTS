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
    /// Resolves deflection requests into projectile adjustments and outcome events.
    /// Pure data only: no VFX, no presentation, deterministic under fixed ticks.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(CombatSystemGroup))]
    [UpdateBefore(typeof(ProjectileFlightSystem))]
    public partial struct DeflectionResolverSystem : ISystem
    {
        private ComponentLookup<ProjectileEntity> _projectileLookup;
        private ComponentLookup<ProjectileControlState> _controlLookup;
        private ComponentLookup<DeflectionBudget> _budgetLookup;
        private ComponentLookup<LocalTransform> _transformLookup;
        private ComponentLookup<ProjectileActive> _activeLookup;
        private ComponentLookup<ProjectileRecycleTag> _recycleLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
            state.RequireForUpdate<DeflectionRequest>();

            _projectileLookup = state.GetComponentLookup<ProjectileEntity>();
            _controlLookup = state.GetComponentLookup<ProjectileControlState>();
            _budgetLookup = state.GetComponentLookup<DeflectionBudget>();
            _transformLookup = state.GetComponentLookup<LocalTransform>(true);
            _activeLookup = state.GetComponentLookup<ProjectileActive>();
            _recycleLookup = state.GetComponentLookup<ProjectileRecycleTag>();
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
            _budgetLookup.Update(ref state);
            _transformLookup.Update(ref state);
            _activeLookup.Update(ref state);
            _recycleLookup.Update(ref state);

            var hasCatalog = SystemAPI.TryGetSingleton<ProjectileCatalog>(out var projectileCatalog) &&
                             projectileCatalog.Catalog.IsCreated;

            var poolingEnabled = SystemAPI.TryGetSingleton<ProjectilePoolConfig>(out var poolConfig) &&
                                 poolConfig.Capacity > 0 &&
                                 poolConfig.Prefab != Entity.Null;

            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            var eventBufferLookup = SystemAPI.GetBufferLookup<DeflectionEvent>(false);

            foreach (var (requests, ownerEntity) in SystemAPI.Query<DynamicBuffer<DeflectionRequest>>().WithEntityAccess())
            {
                if (requests.Length == 0)
                {
                    continue;
                }

                DynamicBuffer<DeflectionEvent> events;
                if (eventBufferLookup.HasBuffer(ownerEntity))
                {
                    events = eventBufferLookup[ownerEntity];
                }
                else
                {
                    events = ecb.AddBuffer<DeflectionEvent>(ownerEntity);
                }

                for (int i = 0; i < requests.Length; i++)
                {
                    var request = requests[i];
                    if (request.Mode == DeflectionMode.None)
                    {
                        continue;
                    }

                    var actor = request.Protector != Entity.Null ? request.Protector :
                        (request.Source != Entity.Null ? request.Source : ownerEntity);

                    bool success = true;

                    if (_budgetLookup.HasComponent(actor))
                    {
                        var budget = _budgetLookup[actor];
                        if (budget.Energy < request.CostEnergy ||
                            budget.Mana < request.CostMana ||
                            budget.Focus < request.CostFocus)
                        {
                            success = false;
                        }
                        else
                        {
                            budget.Energy = math.max(0f, budget.Energy - request.CostEnergy);
                            budget.Mana = math.max(0f, budget.Mana - request.CostMana);
                            budget.Focus = math.max(0f, budget.Focus - request.CostFocus);
                            budget.LastSpendTime = timeState.WorldSeconds;
                            _budgetLookup[actor] = budget;
                        }
                    }

                    float3 resultDirection = float3.zero;
                    if (success)
                    {
                        success = TryApplyDeflection(
                            request,
                            actor,
                            hasCatalog,
                            projectileCatalog.Catalog,
                            poolingEnabled,
                            ecb,
                            timeState.WorldSeconds,
                            ref resultDirection);
                    }

                    events.Add(new DeflectionEvent
                    {
                        Mode = request.Mode,
                        Projectile = request.Projectile,
                        Actor = actor,
                        ResultDirection = resultDirection,
                        Result = (byte)(success ? 1 : 0),
                        Tick = timeState.Tick
                    });
                }

                requests.Clear();
            }
        }

        private bool TryApplyDeflection(
            DeflectionRequest request,
            Entity actor,
            bool hasCatalog,
            BlobAssetReference<ProjectileCatalogBlob> catalog,
            bool poolingEnabled,
            EntityCommandBuffer ecb,
            float worldTime,
            ref float3 resultDirection)
        {
            if (request.Projectile == Entity.Null)
            {
                return false;
            }

            if (!_projectileLookup.HasComponent(request.Projectile))
            {
                return false;
            }

            var projectile = _projectileLookup[request.Projectile];
            float3 currentVel = projectile.Velocity;
            float3 aimDir = request.AimDirection;

            if (math.lengthsq(aimDir) <= 1e-6f)
            {
                if (math.lengthsq(currentVel) > 1e-6f)
                {
                    aimDir = -math.normalizesafe(currentVel, new float3(0f, 0f, 1f));
                }
                else if (_transformLookup.HasComponent(request.Projectile))
                {
                    aimDir = _transformLookup[request.Projectile].Forward();
                }
            }

            float3 direction = math.normalizesafe(aimDir, math.normalizesafe(currentVel, new float3(0f, 0f, 1f)));
            if (math.lengthsq(direction) <= 1e-6f)
            {
                return false;
            }

            float speed = math.length(currentVel);
            if (speed <= 1e-4f && hasCatalog)
            {
                ref var spec = ref FindProjectileSpec(catalog, projectile.ProjectileId);
                if (!UnsafeRef.IsNull(ref spec) && spec.Speed > 0f)
                {
                    speed = spec.Speed;
                }
            }

            if (speed <= 1e-4f)
            {
                speed = 1f;
            }

            switch (request.Mode)
            {
                case DeflectionMode.Deflect:
                case DeflectionMode.Redirect:
                    projectile.Velocity = direction * speed;
                    projectile.TargetEntity = Entity.Null;
                    _projectileLookup[request.Projectile] = projectile;
                    resultDirection = direction;
                    return true;

                case DeflectionMode.Control:
                    projectile.Velocity = direction * speed;
                    _projectileLookup[request.Projectile] = projectile;

                    var controlState = new ProjectileControlState
                    {
                        Controller = actor,
                        Mode = ProjectileControlMode.Hijack,
                        ControlStrength = 1f,
                        GuidanceJitter = 0f,
                        MaxTurnRateDeg = 0f,
                        ControlTick = request.RequestTick,
                        ControlUntilTime = worldTime + 0.25f,
                        TargetPosition = float3.zero
                    };

                    if (_controlLookup.HasComponent(request.Projectile))
                    {
                        _controlLookup[request.Projectile] = controlState;
                    }
                    else
                    {
                        ecb.AddComponent(request.Projectile, controlState);
                    }

                    resultDirection = direction;
                    return true;

                case DeflectionMode.Block:
                case DeflectionMode.Shield:
                case DeflectionMode.Intercept:
                    resultDirection = direction;
                    return RetireProjectile(request.Projectile, poolingEnabled, ecb);

                case DeflectionMode.Dodge:
                    resultDirection = direction;
                    return true;

                default:
                    return false;
            }
        }

        private bool RetireProjectile(Entity projectileEntity, bool poolingEnabled, EntityCommandBuffer ecb)
        {
            if (poolingEnabled && _activeLookup.HasComponent(projectileEntity) && _recycleLookup.HasComponent(projectileEntity))
            {
                if (_projectileLookup.HasComponent(projectileEntity))
                {
                    var projectile = _projectileLookup[projectileEntity];
                    projectile.TargetEntity = Entity.Null;
                    projectile.Velocity = float3.zero;
                    projectile.HitsLeft = 0f;
                    _projectileLookup[projectileEntity] = projectile;
                }

                ecb.SetComponentEnabled<ProjectileActive>(projectileEntity, false);
                ecb.SetComponentEnabled<ProjectileRecycleTag>(projectileEntity, true);
                return true;
            }

            ecb.DestroyEntity(projectileEntity);
            return true;
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
