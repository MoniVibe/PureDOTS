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

namespace PureDOTS.Systems.Groups
{
    /// <summary>
    /// System that determines individual combat intent based on personality and group stance.
    /// Bold/chaotic individuals may flank; craven/peaceful may flee.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(GroupFormationSystem))]
    public partial struct GroupCombatIntentSystem : ISystem
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
            if (timeState.IsPaused)
            {
                return;
            }

            if (!SystemAPI.TryGetSingleton<RewindState>(out var rewindState) || rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            if (!SystemAPI.TryGetSingleton<DemoScenarioState>(out var demoState))
            {
                return;
            }

            // Only process for Godgame (combat intent is primarily for ground units)
            if (!demoState.EnableGodgame)
            {
                return;
            }

            var random = Unity.Mathematics.Random.CreateFromIndex(timeState.Tick);
            var ecb = new EntityCommandBuffer(Allocator.TempJob);

            // Query group members with CombatIntent
            foreach (var (groupMembership, health, combatIntent, entity) in SystemAPI.Query<
                RefRO<GroupMembership>,
                RefRO<Health>,
                RefRW<CombatIntent>>()
                .WithEntityAccess())
            {
                if (!state.EntityManager.Exists(groupMembership.ValueRO.Group))
                {
                    continue;
                }

                var groupStance = state.EntityManager.GetComponentData<GroupStanceState>(groupMembership.ValueRO.Group);

                // Only process when group is in Attack stance
                if (groupStance.Stance != GroupStance.Attack)
                {
                    // Reset to FollowGroup when not attacking
                    combatIntent.ValueRW.State = (byte)CombatIntentState.FollowGroup;
                    combatIntent.ValueRW.Target = Entity.Null;
                    continue;
                }

                // Check personality traits (if available) to determine behavior
                // For demo, use simple random chance based on health
                float healthPercent = health.ValueRO.Current / math.max(1f, health.ValueRO.Max);
                float randomValue = random.NextFloat();

                // Low health + random chance → flee
                if (healthPercent < 0.3f && randomValue < 0.3f)
                {
                    combatIntent.ValueRW.State = (byte)CombatIntentState.Flee;
                }
                // High health + random chance → flank
                else if (healthPercent > 0.7f && randomValue < 0.2f)
                {
                    combatIntent.ValueRW.State = (byte)CombatIntentState.Flank;
                }
                // Otherwise follow group
                else
                {
                    combatIntent.ValueRW.State = (byte)CombatIntentState.FollowGroup;
                }

                // TODO: Integrate with PersonalityAxes and AlignmentTriplet when available
                // Bold + Chaotic → higher flank chance
                // Craven + Peaceful → higher flee chance
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}

