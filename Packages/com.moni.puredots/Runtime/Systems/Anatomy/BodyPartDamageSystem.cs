using PureDOTS.Runtime.Anatomy;
using PureDOTS.Runtime.Combat;
using PureDOTS.Runtime.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Systems.Anatomy
{
    /// <summary>
    /// Applies routed damage to body parts and triggers death when vital parts are destroyed.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(CombatSystemGroup))]
    [UpdateAfter(typeof(DamageApplicationSystem))]
    [UpdateBefore(typeof(DeathSystem))]
    public partial struct BodyPartDamageSystem : ISystem
    {
        private ComponentLookup<Health> _healthLookup;
        private BufferLookup<DeathEvent> _deathBufferLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
            _healthLookup = state.GetComponentLookup<Health>(false);
            _deathBufferLookup = state.GetBufferLookup<DeathEvent>(false);
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
            var currentTick = timeState.Tick;

            _healthLookup.Update(ref state);
            _deathBufferLookup.Update(ref state);

            using var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (damageEvents, parts, entity) in SystemAPI
                         .Query<DynamicBuffer<BodyPartDamageEvent>, DynamicBuffer<BodyPartState>>()
                         .WithEntityAccess())
            {
                if (damageEvents.Length == 0)
                {
                    continue;
                }

                var hasHealth = _healthLookup.HasComponent(entity);

                for (int i = 0; i < damageEvents.Length; i++)
                {
                    var damageEvent = damageEvents[i];
                    if (damageEvent.Damage <= 0f)
                    {
                        continue;
                    }

                    var partIndex = FindPartIndex(parts, damageEvent.PartId);
                    if (partIndex < 0)
                    {
                        if (hasHealth)
                        {
                            var health = _healthLookup[entity];
                            health.Current = math.max(0f, health.Current - damageEvent.Damage);
                            health.LastDamageTick = currentTick;
                            _healthLookup[entity] = health;
                        }
                        continue;
                    }

                    var part = parts[partIndex];
                    var multiplier = part.DamageMultiplier <= 0f ? 1f : part.DamageMultiplier;
                    var appliedDamage = damageEvent.Damage * multiplier;
                    part.CurrentHealth = math.max(0f, part.CurrentHealth - appliedDamage);
                    parts[partIndex] = part;

                    if (part.CurrentHealth <= 0f && (part.Flags & BodyPartFlags.Vital) != 0)
                    {
                        if (hasHealth)
                        {
                            var health = _healthLookup[entity];
                            if (health.Current > 0f)
                            {
                                health.Current = 0f;
                                health.LastDamageTick = currentTick;
                                _healthLookup[entity] = health;
                            }
                        }

                        if (!_deathBufferLookup.HasBuffer(entity))
                        {
                            ecb.AddBuffer<DeathEvent>(entity);
                        }

                        ecb.AppendToBuffer(entity, new DeathEvent
                        {
                            DeadEntity = entity,
                            KillerEntity = damageEvent.SourceEntity,
                            KillingBlowType = damageEvent.DamageType,
                            DeathTick = currentTick
                        });
                    }
                }

                damageEvents.Clear();
            }

            ecb.Playback(state.EntityManager);
        }

        [BurstCompile]
        private static int FindPartIndex(DynamicBuffer<BodyPartState> parts, FixedString64Bytes partId)
        {
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].PartId.Equals(partId))
                {
                    return i;
                }
            }

            return -1;
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }
    }
}
