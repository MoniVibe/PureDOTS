using PureDOTS.Runtime.Combat;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.LowLevel;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Systems.Combat
{
    /// <summary>
    /// Evaluates nearby projectiles and populates DeflectionThreat buffers.
    /// Pure data only: no physics queries, simple heuristics.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(CombatSystemGroup))]
    [UpdateBefore(typeof(DeflectionPlannerSystem))]
    public partial struct DeflectionThreatEvaluationSystem : ISystem
    {
        private ComponentLookup<LocalTransform> _transformLookup;
        private ComponentLookup<ProjectileActive> _activeLookup;
        private ComponentLookup<DeflectionSense> _senseLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
            _transformLookup = state.GetComponentLookup<LocalTransform>(true);
            _activeLookup = state.GetComponentLookup<ProjectileActive>(true);
            _senseLookup = state.GetComponentLookup<DeflectionSense>(true);
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

            _transformLookup.Update(ref state);
            _activeLookup.Update(ref state);
            _senseLookup.Update(ref state);

            bool hasCatalog = SystemAPI.TryGetSingleton<ProjectileCatalog>(out var projectileCatalog) &&
                              projectileCatalog.Catalog.IsCreated;
            bool hasAmmoCatalog = SystemAPI.TryGetSingleton<AmmoCatalog>(out var ammoCatalog) &&
                                  ammoCatalog.Catalog.IsCreated;

            var bufferLookup = SystemAPI.GetBufferLookup<DeflectionThreat>(false);
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (transform, entity) in SystemAPI.Query<RefRO<LocalTransform>>()
                         .WithAll<DeflectionProfile>()
                         .WithEntityAccess())
            {
                if (!bufferLookup.HasBuffer(entity))
                {
                    ecb.AddBuffer<DeflectionThreat>(entity);
                    continue;
                }

                var threats = bufferLookup[entity];
                threats.Clear();

                float range = 1000f;
                int maxThreats = 8;

                if (_senseLookup.HasComponent(entity))
                {
                    var sense = _senseLookup[entity];
                    if (sense.Range > 0f)
                    {
                        range = sense.Range;
                    }
                    if (sense.MaxThreats > 0)
                    {
                        maxThreats = sense.MaxThreats;
                    }
                }

                float3 defenderPos = transform.ValueRO.Position;
                float rangeSq = range * range;

                foreach (var (projectile, projTransform, active, projectileEntity) in
                         SystemAPI.Query<RefRO<ProjectileEntity>, RefRO<LocalTransform>, EnabledRefRO<ProjectileActive>>()
                             .WithEntityAccess())
                {
                    if (!active.ValueRO)
                    {
                        continue;
                    }

                    float3 projectilePos = projTransform.ValueRO.Position;
                    float3 toDefender = defenderPos - projectilePos;
                    float distSq = math.lengthsq(toDefender);
                    if (distSq > rangeSq)
                    {
                        continue;
                    }

                    float distance = math.sqrt(distSq);
                    float3 incomingDir = math.normalizesafe(toDefender, new float3(0f, 0f, 1f));

                    float speed = math.length(projectile.ValueRO.Velocity);
                    float baseDamage = 1f;
                    float dodgeDifficulty = 0.4f;
                    float deflectResistance = 0.3f;
                    float controlResistance = 0.2f;
                    float damageMultiplier = 1f;
                    var behaviorProfile = WeaponBehaviorProfiles.Resolve(WeaponBehaviorArchetype.Default);

                    if (hasCatalog)
                    {
                        ref var spec = ref FindProjectileSpec(projectileCatalog.Catalog, projectile.ValueRO.ProjectileId);
                        if (!UnsafeRef.IsNull(ref spec))
                        {
                            if (spec.Speed > 0f && speed <= 1e-4f)
                            {
                                speed = spec.Speed;
                            }

                            baseDamage = math.max(0.1f, spec.Damage.BaseDamage);
                            switch ((ProjectileKind)spec.Kind)
                            {
                                case ProjectileKind.Homing:
                                    dodgeDifficulty = 0.7f;
                                    deflectResistance = 0.6f;
                                    controlResistance = 0.4f;
                                    break;
                                case ProjectileKind.Beam:
                                    dodgeDifficulty = 0.9f;
                                    deflectResistance = 0.8f;
                                    controlResistance = 0.9f;
                                    break;
                                default:
                                    dodgeDifficulty = 0.4f;
                                    deflectResistance = 0.3f;
                                    controlResistance = 0.2f;
                                    break;
                            }

                            var archetype = (ProjectileKind)spec.Kind switch
                            {
                                ProjectileKind.Beam => WeaponBehaviorArchetype.Energy,
                                ProjectileKind.Homing => WeaponBehaviorArchetype.GuidedMissile,
                                ProjectileKind.Ballistic => WeaponBehaviorArchetype.Kinetic,
                                _ => WeaponBehaviorArchetype.Default
                            };
                            behaviorProfile = WeaponBehaviorProfiles.Resolve(archetype);
                        }
                    }

                    if (hasAmmoCatalog && projectile.ValueRO.AmmoId.Length > 0)
                    {
                        ref var ammoSpec = ref FindAmmoSpec(ammoCatalog.Catalog, projectile.ValueRO.AmmoId);
                        if (!UnsafeRef.IsNull(ref ammoSpec))
                        {
                            damageMultiplier = ammoSpec.DamageMultiplier;
                        }
                    }

                    if (speed <= 1e-4f)
                    {
                        speed = 1f;
                    }

                    float timeToImpact = distance / speed;
                    float approachDot = 0f;
                    if (math.lengthsq(projectile.ValueRO.Velocity) > 1e-6f)
                    {
                        approachDot = math.saturate(math.dot(math.normalizesafe(projectile.ValueRO.Velocity), incomingDir));
                    }

                    dodgeDifficulty = math.saturate(dodgeDifficulty + (1f - approachDot) * 0.1f);
                    deflectResistance = math.saturate(deflectResistance + math.saturate(speed * 0.01f));
                    deflectResistance = WeaponBehaviorProfiles.ResolveDeflectResistance(deflectResistance, behaviorProfile);
                    controlResistance = math.saturate(controlResistance + math.saturate(speed * 0.005f));

                    float threatScore = baseDamage * damageMultiplier * math.rcp(1f + timeToImpact);

                    var threat = new DeflectionThreat
                    {
                        Projectile = projectileEntity,
                        Source = projectile.ValueRO.SourceEntity,
                        ThreatScore = threatScore,
                        TimeToImpact = timeToImpact,
                        Distance = distance,
                        DodgeDifficulty = dodgeDifficulty,
                        DeflectResistance = deflectResistance,
                        ControlResistance = controlResistance,
                        IncomingDirection = incomingDir,
                        SampleTick = timeState.Tick
                    };

                    if (threats.Length < maxThreats)
                    {
                        threats.Add(threat);
                    }
                    else
                    {
                        int minIndex = 0;
                        float minScore = threats[0].ThreatScore;
                        for (int i = 1; i < threats.Length; i++)
                        {
                            if (threats[i].ThreatScore < minScore)
                            {
                                minScore = threats[i].ThreatScore;
                                minIndex = i;
                            }
                        }

                        if (threat.ThreatScore > minScore)
                        {
                            threats[minIndex] = threat;
                        }
                    }
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

        private static ref AmmoSpec FindAmmoSpec(
            BlobAssetReference<AmmoCatalogBlob> catalog,
            FixedString32Bytes ammoId)
        {
            if (!catalog.IsCreated)
            {
                return ref UnsafeRef.Null<AmmoSpec>();
            }

            ref var ammos = ref catalog.Value.Ammunition;
            for (int i = 0; i < ammos.Length; i++)
            {
                ref var ammoSpec = ref ammos[i];
                if (ammoSpec.Id.Equals(ammoId))
                {
                    return ref ammoSpec;
                }
            }

            return ref UnsafeRef.Null<AmmoSpec>();
        }
    }
}
