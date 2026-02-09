using PureDOTS.Runtime.Combat;
using PureDOTS.Runtime.Components;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Systems.Combat
{
    /// <summary>
    /// Plans deflection actions from evaluated threats.
    /// Emits DeflectionRequest buffers consumed by DeflectionResolverSystem.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(CombatSystemGroup))]
    [UpdateAfter(typeof(DeflectionThreatEvaluationSystem))]
    [UpdateBefore(typeof(DeflectionResolverSystem))]
    public partial struct DeflectionPlannerSystem : ISystem
    {
        private ComponentLookup<ProjectileEntity> _projectileLookup;
        private ComponentLookup<LocalTransform> _transformLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
            _projectileLookup = state.GetComponentLookup<ProjectileEntity>(true);
            _transformLookup = state.GetComponentLookup<LocalTransform>(true);
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
            _transformLookup.Update(ref state);

            var requestLookup = SystemAPI.GetBufferLookup<DeflectionRequest>(false);
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (profile, budget, threats, transform, entity) in
                     SystemAPI.Query<RefRO<DeflectionProfile>, RefRW<DeflectionBudget>, DynamicBuffer<DeflectionThreat>, RefRO<LocalTransform>>()
                         .WithEntityAccess())
            {
                if (threats.Length == 0)
                {
                    continue;
                }

                float cooldown = math.max(0f, profile.ValueRO.CooldownSec);
                float maxActionsPerSecond = profile.ValueRO.MaxActionsPerSecond;
                if (maxActionsPerSecond > 0f)
                {
                    cooldown = math.max(cooldown, 1f / maxActionsPerSecond);
                }

                if (timeState.WorldSeconds - budget.ValueRO.LastSpendTime < cooldown)
                {
                    continue;
                }

                int bestIndex = -1;
                float bestThreat = profile.ValueRO.MinThreatScore;
                for (int i = 0; i < threats.Length; i++)
                {
                    var threat = threats[i];
                    if (threat.ThreatScore > bestThreat)
                    {
                        bestThreat = threat.ThreatScore;
                        bestIndex = i;
                    }
                }

                if (bestIndex < 0)
                {
                    continue;
                }

                var selected = threats[bestIndex];
                if (selected.Projectile == Entity.Null)
                {
                    continue;
                }

                float normalizedThreat = math.saturate(selected.ThreatScore / math.max(0.001f, profile.ValueRO.MaxThreatScore));

                float dodgeScore = profile.ValueRO.DodgeBias * (1f - selected.DodgeDifficulty);
                float blockScore = profile.ValueRO.BlockBias * (1f - selected.DeflectResistance) * 0.8f;
                float deflectScore = profile.ValueRO.DeflectBias * (1f - selected.DeflectResistance);
                float redirectScore = profile.ValueRO.RedirectBias * (1f - selected.DeflectResistance) * 0.9f;
                float controlScore = profile.ValueRO.ControlBias * (1f - selected.ControlResistance);

                dodgeScore *= normalizedThreat;
                blockScore *= normalizedThreat;
                deflectScore *= normalizedThreat;
                redirectScore *= normalizedThreat;
                controlScore *= normalizedThreat;

                var chosen = DeflectionMode.Dodge;
                float bestScore = dodgeScore;

                if (blockScore > bestScore)
                {
                    bestScore = blockScore;
                    chosen = DeflectionMode.Block;
                }
                if (deflectScore > bestScore)
                {
                    bestScore = deflectScore;
                    chosen = DeflectionMode.Deflect;
                }
                if (redirectScore > bestScore)
                {
                    bestScore = redirectScore;
                    chosen = DeflectionMode.Redirect;
                }
                if (controlScore > bestScore)
                {
                    bestScore = controlScore;
                    chosen = DeflectionMode.Control;
                }

                if (!requestLookup.HasBuffer(entity))
                {
                    ecb.AddBuffer<DeflectionRequest>(entity);
                    continue;
                }

                var requests = requestLookup[entity];

                float3 aimDirection = selected.IncomingDirection;
                if (_projectileLookup.HasComponent(selected.Projectile))
                {
                    var projectile = _projectileLookup[selected.Projectile];
                    if (math.lengthsq(projectile.Velocity) > 1e-6f)
                    {
                        aimDirection = -math.normalizesafe(projectile.Velocity);
                    }
                    else if (_transformLookup.HasComponent(selected.Projectile))
                    {
                        aimDirection = math.normalizesafe(transform.ValueRO.Position -
                                                          _transformLookup[selected.Projectile].Position,
                                                          selected.IncomingDirection);
                    }
                }

                float costEnergy = 0f;
                float costMana = 0f;
                float costFocus = 0f;

                switch (chosen)
                {
                    case DeflectionMode.Dodge:
                        costFocus = normalizedThreat * 0.1f;
                        break;
                    case DeflectionMode.Control:
                        costMana = normalizedThreat * 0.3f;
                        break;
                    case DeflectionMode.Deflect:
                    case DeflectionMode.Redirect:
                        costEnergy = normalizedThreat * 0.2f;
                        break;
                    case DeflectionMode.Block:
                    case DeflectionMode.Shield:
                    case DeflectionMode.Intercept:
                        costEnergy = normalizedThreat * 0.15f;
                        break;
                }

                requests.Add(new DeflectionRequest
                {
                    Mode = chosen,
                    Projectile = selected.Projectile,
                    Source = entity,
                    Protector = entity,
                    AimDirection = aimDirection,
                    CostEnergy = costEnergy,
                    CostMana = costMana,
                    CostFocus = costFocus,
                    RequestTick = timeState.Tick
                });
            }
        }
    }
}
