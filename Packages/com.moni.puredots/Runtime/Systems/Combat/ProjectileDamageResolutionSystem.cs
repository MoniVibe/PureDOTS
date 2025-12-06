using PureDOTS.Runtime.Combat;
using PureDOTS.Runtime.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Systems.Combat
{
    /// <summary>
    /// Stage 3 of 3-stage damage pipeline: Applies accumulated damage once per tick.
    /// Main thread/single job - applies damage from DamageAccumulator to Damageable components.
    /// Uses Burst-safe mitigation formulas.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(CombatSystemGroup))]
    [UpdateAfter(typeof(ProjectileAoESystem))]
    [UpdateAfter(typeof(ProjectileEffectExecutionSystem))]
    public partial struct ProjectileDamageResolutionSystem : ISystem
    {
        // DamageAccumulator is a managed component storing NativeParallelHashMap<Entity, float>
        // This system runs on main thread to access managed accumulator

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var rewindState = SystemAPI.GetSingleton<RewindState>();
            if (rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            var timeState = SystemAPI.GetSingleton<TimeState>();

            // Process ImpactEvents and accumulate damage
            // For now, process directly (full accumulator system requires managed wrapper)
            var impactBufferLookup = SystemAPI.GetBufferLookup<ImpactEvent>(true);
            impactBufferLookup.Update(ref state);

            var damageableLookup = SystemAPI.GetComponentLookup<Damageable>(false);
            damageableLookup.Update(ref state);

            foreach (var (impactEvents, entity) in
                SystemAPI.Query<DynamicBuffer<ImpactEvent>>()
                .WithEntityAccess())
            {
                if (impactEvents.Length == 0)
                {
                    continue;
                }

                // Accumulate total damage for this target
                float totalDamage = 0f;
                for (int i = 0; i < impactEvents.Length; i++)
                {
                    totalDamage += impactEvents[i].Damage;
                }

                // Apply mitigated damage
                Entity targetEntity = impactEvents[0].Target;
                if (damageableLookup.HasComponent(targetEntity))
                {
                    var damageable = damageableLookup[targetEntity];
                    
                    // Apply mitigation formula: dmg * (1f - saturate(armor / (armor + 100f))) * (1f - resist)
                    float armor = damageable.ArmorPoints;
                    float resist = 0f; // TODO: Get from resistance component
                    float mitigatedDamage = ProjectileHelpers.MitigatedDamage(totalDamage, armor, resist);

                    // Apply to shields first, then armor, then hull
                    if (damageable.ShieldPoints > 0f && mitigatedDamage > 0f)
                    {
                        float shieldDamage = math.min(damageable.ShieldPoints, mitigatedDamage);
                        damageable.ShieldPoints -= shieldDamage;
                        mitigatedDamage -= shieldDamage;
                    }

                    if (mitigatedDamage > 0f && damageable.ArmorPoints > 0f)
                    {
                        float armorDamage = math.min(damageable.ArmorPoints, mitigatedDamage * 0.5f); // Armor absorbs 50%
                        damageable.ArmorPoints -= armorDamage;
                        mitigatedDamage -= armorDamage;
                    }

                    if (mitigatedDamage > 0f)
                    {
                        damageable.HullPoints = math.max(0f, damageable.HullPoints - mitigatedDamage);
                    }

                    damageableLookup[targetEntity] = damageable;
                }

                // Clear impact events after processing
                impactEvents.Clear();
            }
        }
    }
}

