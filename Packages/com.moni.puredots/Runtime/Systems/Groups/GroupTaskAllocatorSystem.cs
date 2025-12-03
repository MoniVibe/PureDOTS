using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Groups;
using PureDOTS.Runtime.Interrupts;
using Unity.Burst;
using Unity.Entities;

namespace PureDOTS.Systems.Groups
{
    /// <summary>
    /// Translates GroupObjective into interrupts/orders for group members.
    /// This is the bridge between group-level decisions and individual entity behavior.
    /// Runs after GroupObjectiveSelectionSystem, before InterruptHandlerSystem.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(GroupDecisionSystemGroup))]
    [UpdateAfter(typeof(GroupObjectiveSelectionSystem))]
    public partial struct GroupTaskAllocatorSystem : ISystem
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
            var timeState = SystemAPI.GetSingleton<TimeState>();
            var rewindState = SystemAPI.GetSingleton<RewindState>();

            if (timeState.IsPaused || rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            // Allocate tasks for all groups with active objectives
            foreach (var (objective, members, groupIdentity) in
                SystemAPI.Query<RefRO<GroupObjective>, DynamicBuffer<GroupMember>, RefRO<GroupIdentity>>())
            {
                // Skip if objective not active
                if (objective.ValueRO.IsActive == 0)
                {
                    continue;
                }

                // Skip if group not active
                if (groupIdentity.ValueRO.Status != GroupStatus.Active)
                {
                    continue;
                }

                // Distribute objective to members via interrupts
                DistributeObjectiveToMembers(
                    ref state,
                    objective.ValueRO,
                    members,
                    timeState.Tick);
            }
        }

        /// <summary>
        /// Distributes group objective to members via interrupts.
        /// Phase 1: Simple distribution (all members get same order).
        /// Phase 2: Role-based distribution, specialization, etc.
        /// </summary>
        [BurstCompile]
        private void DistributeObjectiveToMembers(
            ref SystemState state,
            GroupObjective objective,
            DynamicBuffer<GroupMember> members,
            uint currentTick)
        {
            // Convert objective type to interrupt type
            var interruptType = ObjectiveToInterruptType(objective.ObjectiveType);
            var priority = InterruptPriority.Normal;

            // Set priority based on objective priority
            if (objective.Priority >= 200)
            {
                priority = InterruptPriority.Critical;
            }
            else if (objective.Priority >= 150)
            {
                priority = InterruptPriority.Urgent;
            }
            else if (objective.Priority >= 100)
            {
                priority = InterruptPriority.High;
            }

            // Emit interrupt to each active member
            foreach (var member in members)
            {
                if (!SystemAPI.Exists(member.MemberEntity))
                {
                    continue;
                }

                // Skip inactive members
                if ((member.Flags & GroupMemberFlags.Active) == 0)
                {
                    continue;
                }

                // Ensure member has interrupt buffer
                if (!SystemAPI.HasBuffer<Interrupt>(member.MemberEntity))
                {
                    state.EntityManager.AddBuffer<Interrupt>(member.MemberEntity);
                }

                var interruptBuffer = SystemAPI.GetBuffer<Interrupt>(member.MemberEntity);

                // Emit interrupt
                InterruptUtils.EmitOrder(
                    ref interruptBuffer,
                    interruptType,
                    Entity.Null, // Group entity (could pass actual group entity if needed)
                    objective.TargetEntity,
                    objective.TargetPosition,
                    currentTick,
                    priority);
            }
        }

        /// <summary>
        /// Maps GroupObjectiveType to InterruptType.
        /// </summary>
        [BurstCompile]
        private static InterruptType ObjectiveToInterruptType(GroupObjectiveType objectiveType)
        {
            return objectiveType switch
            {
                GroupObjectiveType.Defend => InterruptType.NewOrder,
                GroupObjectiveType.Forage => InterruptType.NewOrder,
                GroupObjectiveType.MoveTo => InterruptType.NewOrder,
                GroupObjectiveType.Patrol => InterruptType.NewOrder,
                GroupObjectiveType.Retreat => InterruptType.NewOrder,
                GroupObjectiveType.ExpandSettlement => InterruptType.NewOrder,
                GroupObjectiveType.Build => InterruptType.NewOrder,
                GroupObjectiveType.SecureSystem => InterruptType.NewOrder,
                GroupObjectiveType.EscortConvoy => InterruptType.NewOrder,
                GroupObjectiveType.Raid => InterruptType.NewOrder,
                GroupObjectiveType.Mining => InterruptType.NewOrder,
                _ => InterruptType.NewOrder
            };
        }
    }
}

