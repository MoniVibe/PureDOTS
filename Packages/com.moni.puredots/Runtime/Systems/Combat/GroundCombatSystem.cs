using PureDOTS.Runtime;
using PureDOTS.Runtime.Aggregate;
using PureDOTS.Runtime.Combat;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Groups;
using PureDOTS.Runtime.Time;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Systems.Combat
{
    /// <summary>
    /// Ground combat system for Godgame bands/villagers.
    /// Handles melee/range combat based on group stance.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct GroundCombatSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
            state.RequireForUpdate<DemoScenarioState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            var rewindState = SystemAPI.GetSingleton<RewindState>();
            var demoState = SystemAPI.GetSingleton<DemoScenarioState>();

            if (timeState.IsPaused || rewindState.Mode != RewindMode.Record || !demoState.EnableGodgame)
            {
                return;
            }

            var currentTime = timeState.ElapsedTime;
            var ecb = new EntityCommandBuffer(Allocator.TempJob);

            // Query entities with Health, AttackStats, and GroupMembership
            foreach (var (health, attackStats, transform, groupMembership, entity) in SystemAPI.Query<
                RefRW<Health>,
                RefRW<AttackStats>,
                RefRO<LocalTransform>,
                RefRO<GroupMembership>>()
                .WithEntityAccess())
            {
                // Check if entity's group has Attack stance
                if (!state.EntityManager.Exists(groupMembership.ValueRO.Group))
                {
                    continue;
                }

                var groupStance = state.EntityManager.GetComponentData<GroupStanceState>(groupMembership.ValueRO.Group);
                if (groupStance.Stance != GroupStance.Attack)
                {
                    continue;
                }

                // Check cooldown
                ref var attackStatsRef = ref attackStats.ValueRW;
                if (currentTime - attackStatsRef.LastAttackTime < attackStatsRef.AttackCooldown)
                {
                    continue;
                }

                // Find nearest enemy within range
                Entity nearestEnemy = Entity.Null;
                float nearestDistance = float.MaxValue;

                foreach (var (enemyHealth, enemyTransform, enemyEntity) in SystemAPI.Query<RefRO<Health>, RefRO<LocalTransform>>().WithEntityAccess())
                {
                    // Skip self
                    if (enemyEntity == entity)
                    {
                        continue;
                    }

                    float distance = math.distance(transform.ValueRO.Position, enemyTransform.ValueRO.Position);
                    if (distance <= attackStatsRef.Range && distance < nearestDistance)
                    {
                        nearestEnemy = enemyEntity;
                        nearestDistance = distance;
                    }
                }

                // Attack if enemy found
                if (nearestEnemy != Entity.Null && state.EntityManager.Exists(nearestEnemy))
                {
                    var enemyHealth = state.EntityManager.GetComponentData<Health>(nearestEnemy);
                    float damage = attackStatsRef.Damage;

                    // Apply defense if enemy has DefenseStats
                    if (state.EntityManager.HasComponent<DefenseStats>(nearestEnemy))
                    {
                        var defense = state.EntityManager.GetComponentData<DefenseStats>(nearestEnemy);
                        damage = math.max(0f, damage - defense.Armor);
                    }

                    enemyHealth.Current = math.max(0f, enemyHealth.Current - damage);
                    ecb.SetComponent(nearestEnemy, enemyHealth);

                    attackStatsRef.LastAttackTime = currentTime;
                }
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}

