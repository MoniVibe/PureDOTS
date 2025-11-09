using PureDOTS.Runtime.Combat;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Systems.Combat
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct CombatLoopSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CombatLoopState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            foreach (var (config, loopState, transform) in SystemAPI
                         .Query<RefRO<CombatLoopConfig>, RefRW<CombatLoopState>, RefRO<LocalTransform>>())
            {
                ref var stateRW = ref loopState.ValueRW;
                stateRW.WeaponCooldown = math.max(0f, stateRW.WeaponCooldown - deltaTime);

                switch (stateRW.Phase)
                {
                    case CombatLoopPhase.Idle:
                        stateRW.Phase = CombatLoopPhase.Patrol;
                        stateRW.PhaseTimer = 1f;
                        break;
                    case CombatLoopPhase.Patrol:
                        stateRW.PhaseTimer -= deltaTime;
                        if (stateRW.PhaseTimer <= 0f)
                        {
                            stateRW.Phase = CombatLoopPhase.Intercept;
                            stateRW.PhaseTimer = 1f;
                        }
                        break;
                    case CombatLoopPhase.Intercept:
                        stateRW.PhaseTimer -= deltaTime;
                        if (stateRW.PhaseTimer <= 0f)
                        {
                            stateRW.Phase = CombatLoopPhase.Attack;
                        }
                        break;
                    case CombatLoopPhase.Attack:
                        if (stateRW.WeaponCooldown <= 0f)
                        {
                            stateRW.WeaponCooldown = config.ValueRO.WeaponCooldownSeconds;
                        }
                        stateRW.Phase = CombatLoopPhase.Retreat;
                        stateRW.PhaseTimer = 2f;
                        break;
                    case CombatLoopPhase.Retreat:
                        stateRW.PhaseTimer -= deltaTime;
                        if (stateRW.PhaseTimer <= 0f)
                        {
                            stateRW.Phase = CombatLoopPhase.Patrol;
                        }
                        break;
                }
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }
    }
}
