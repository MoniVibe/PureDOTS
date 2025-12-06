using PureDOTS.Runtime.AI;
using PureDOTS.Runtime.Bands;
using PureDOTS.Runtime.Combat;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Morale;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Systems.AI
{
    /// <summary>
    /// Tactical AI state machine for formations.
    /// Evaluates state transitions based on morale, cohesion, commander traits, and battlefield context.
    /// Issues micro-goals via command packets.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(TacticalSystemGroup))]
    public partial struct TacticalAIStateMachineSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
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

            var ecb = new EntityCommandBuffer(Allocator.TempJob);

            // Process formations with tactical AI state
            foreach (var (formationEntity, aiState, bandStats, groupMorale, formationCommand) in SystemAPI
                         .Query<Entity, RefRW<TacticalAIState>, RefRO<BandStats>, RefRO<GroupMorale>, RefRW<FormationCommand>>()
                         .WithEntityAccess())
            {
                var stateValue = aiState.ValueRO;
                var stats = bandStats.ValueRO;
                var morale = groupMorale.ValueRO;

                // Update context
                var context = new float4(
                    morale.CurrentMorale,           // RelativeMorale
                    stats.Cohesion / 100f,          // Cohesion (normalized)
                    0.5f,                          // CommanderTraits (placeholder)
                    0.5f                           // BattlefieldContext (placeholder)
                );

                // Evaluate state transition
                var newState = EvaluateStateTransition(stateValue.State, context, stats, morale, currentTick - stateValue.DecisionTick);

                // Update state if changed
                if (newState != stateValue.State)
                {
                    aiState.ValueRW = new TacticalAIState
                    {
                        State = newState,
                        DecisionTick = currentTick,
                        Context = context
                    };

                    // Issue micro-goals based on new state
                    IssueMicroGoal(formationEntity, newState, ref formationCommand.ValueRW, ecb);
                }
                else
                {
                    // Update context even if state didn't change
                    aiState.ValueRW = new TacticalAIState
                    {
                        State = stateValue.State,
                        DecisionTick = stateValue.DecisionTick,
                        Context = context
                    };
                }
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        private static TacticalAIStateType EvaluateStateTransition(
            TacticalAIStateType currentState,
            float4 context,
            BandStats stats,
            GroupMorale morale,
            uint ticksSinceDecision)
        {
            // Simple state machine logic
            // In full implementation, this would use SIMD lookup tables

            var relativeMorale = context.x;
            var cohesion = context.y;

            // Minimum time in state before transition (prevent rapid switching)
            if (ticksSinceDecision < 10)
            {
                return currentState;
            }

            // Check for rout state
            if (relativeMorale < 0.3f)
            {
                return TacticalAIStateType.Regroup;
            }

            // State-specific transitions
            switch (currentState)
            {
                case TacticalAIStateType.Idle:
                    if (cohesion > 0.7f && relativeMorale > 0.6f)
                    {
                        return TacticalAIStateType.Advance;
                    }
                    break;

                case TacticalAIStateType.Advance:
                    if ((stats.Flags & BandStatusFlags.Engaged) != 0)
                    {
                        return TacticalAIStateType.Engage;
                    }
                    if (cohesion < 0.5f)
                    {
                        return TacticalAIStateType.Regroup;
                    }
                    break;

                case TacticalAIStateType.Engage:
                    if (relativeMorale < 0.4f)
                    {
                        return TacticalAIStateType.Evaluate;
                    }
                    if ((stats.Flags & BandStatusFlags.Engaged) == 0)
                    {
                        return TacticalAIStateType.Pursue;
                    }
                    break;

                case TacticalAIStateType.Evaluate:
                    if (relativeMorale > 0.5f && cohesion > 0.6f)
                    {
                        return TacticalAIStateType.Engage;
                    }
                    if (relativeMorale < 0.3f)
                    {
                        return TacticalAIStateType.Regroup;
                    }
                    break;

                case TacticalAIStateType.Regroup:
                    if (cohesion > 0.7f && relativeMorale > 0.5f)
                    {
                        return TacticalAIStateType.Idle;
                    }
                    break;

                case TacticalAIStateType.Pursue:
                    if ((stats.Flags & BandStatusFlags.Engaged) != 0)
                    {
                        return TacticalAIStateType.Engage;
                    }
                    if (cohesion < 0.5f)
                    {
                        return TacticalAIStateType.Regroup;
                    }
                    break;
            }

            return currentState;
        }

        [BurstCompile]
        private static void IssueMicroGoal(
            Entity formationEntity,
            TacticalAIStateType state,
            ref FormationCommand command,
            EntityCommandBuffer ecb)
        {
            // Issue micro-goals based on state
            // This would modify FormationCommand to issue specific commands
            // For now, we update the command based on state

            switch (state)
            {
                case TacticalAIStateType.Advance:
                    // "push center" - advance forward
                    // Command would be set to advance
                    break;

                case TacticalAIStateType.Engage:
                    // "engage" - attack target
                    // Command would be set to attack
                    break;

                case TacticalAIStateType.Regroup:
                    // "regroup" - tighten formation
                    // Command would be set to hold/regroup
                    break;

                case TacticalAIStateType.Pursue:
                    // "pursue" - chase enemy
                    // Command would be set to pursue
                    break;
            }
        }
    }
}

