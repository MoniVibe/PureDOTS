using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Groups;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Systems.Groups
{
    /// <summary>
    /// Adapts existing group metrics/cohesion/objective signals into GroupMoraleContractState.
    /// Emits phase transition events for telemetry and narrative hooks.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(GroupDecisionSystemGroup))]
    [UpdateAfter(typeof(GroupMetricsSystem))]
    [UpdateAfter(typeof(GroupFormationSpreadSystem))]
    [UpdateBefore(typeof(GroupObjectiveSelectionSystem))]
    public partial struct GroupMoraleContractAdapterSystem : ISystem
    {
        private ComponentLookup<LocalTransform> _transformLookup;
        private ComponentLookup<GroupFormationSpread> _spreadLookup;
        private ComponentLookup<GroupAnchorContractState> _anchorLookup;
        private ComponentLookup<GroupObjective> _objectiveLookup;
        private ComponentLookup<GroupGoalCommitmentContract> _goalLookup;
        private ComponentLookup<GroupSplinterContractState> _splinterLookup;
        private ComponentLookup<GroupIdentity> _identityLookup;
        private BufferLookup<GroupMember> _memberLookup;
        private BufferLookup<GroupMoraleTransitionEvent> _transitionLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
            state.RequireForUpdate<GroupMoraleContractState>();

            _transformLookup = state.GetComponentLookup<LocalTransform>(true);
            _spreadLookup = state.GetComponentLookup<GroupFormationSpread>(true);
            _anchorLookup = state.GetComponentLookup<GroupAnchorContractState>(true);
            _objectiveLookup = state.GetComponentLookup<GroupObjective>(true);
            _goalLookup = state.GetComponentLookup<GroupGoalCommitmentContract>(false);
            _splinterLookup = state.GetComponentLookup<GroupSplinterContractState>(false);
            _identityLookup = state.GetComponentLookup<GroupIdentity>(true);
            _memberLookup = state.GetBufferLookup<GroupMember>(true);
            _transitionLookup = state.GetBufferLookup<GroupMoraleTransitionEvent>(false);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            if (timeState.IsPaused
                || !SystemAPI.TryGetSingleton<RewindState>(out var rewindState)
                || rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            _transformLookup.Update(ref state);
            _spreadLookup.Update(ref state);
            _anchorLookup.Update(ref state);
            _objectiveLookup.Update(ref state);
            _goalLookup.Update(ref state);
            _splinterLookup.Update(ref state);
            _identityLookup.Update(ref state);
            _memberLookup.Update(ref state);
            _transitionLookup.Update(ref state);

            var currentTick = timeState.Tick;
            var deltaTime = math.max(0.001f, (float)SystemAPI.Time.DeltaTime);

            foreach (var (metrics, aggregate, profile, moraleState, entity) in
                     SystemAPI.Query<RefRO<GroupMetrics>, RefRO<GroupAggregate>, RefRO<GroupMoraleContractProfile>, RefRW<GroupMoraleContractState>>()
                         .WithEntityAccess())
            {
                var previous = moraleState.ValueRO;
                var hasObjective = _objectiveLookup.HasComponent(entity) && _objectiveLookup[entity].IsActive != 0;
                var objective = hasObjective ? _objectiveLookup[entity] : default;

                var morale01 = ResolveMorale01(metrics.ValueRO, aggregate.ValueRO);
                var cohesion01 = ResolveCohesion01(entity, aggregate.ValueRO.Cohesion);
                var threat01 = math.saturate(metrics.ValueRO.ThreatLevel / 255f);
                var casualtyPressure01 = ResolveCasualtyPressure01(metrics.ValueRO.MemberCount, metrics.ValueRO.ActiveMemberCount);
                var supplyStress01 = ResolveSupplyStress01(metrics.ValueRO.ResourceCount0);
                var pressure01 = ResolvePressure01(threat01, casualtyPressure01, supplyStress01);
                var anchorSecurity01 = ResolveAnchorSecurity01(entity, hasObjective, objective);
                var commitment01 = ResolveGoalCommitment01(
                    entity,
                    hasObjective,
                    objective,
                    previous.GoalCommitment01,
                    in profile.ValueRO,
                    currentTick,
                    deltaTime);

                var phase = GroupMoraleContract.ResolvePhase(morale01, in profile.ValueRO);
                var intent = GroupMoraleContract.ResolveIntent(
                    phase,
                    cohesion01,
                    anchorSecurity01,
                    commitment01,
                    in profile.ValueRO);

                var influences = ResolveInfluences(
                    entity,
                    hasObjective,
                    objective,
                    in profile.ValueRO,
                    in previous,
                    morale01,
                    cohesion01,
                    pressure01,
                    anchorSecurity01,
                    commitment01,
                    threat01,
                    casualtyPressure01,
                    supplyStress01,
                    currentTick);

                var phaseChangedTick = previous.LastUpdatedTick == 0 ? currentTick : previous.PhaseChangedTick;
                if (previous.LastUpdatedTick != 0 && previous.Phase != phase)
                {
                    phaseChangedTick = currentTick;
                    if (_transitionLookup.HasBuffer(entity))
                    {
                        var transitions = _transitionLookup[entity];
                        transitions.Add(new GroupMoraleTransitionEvent
                        {
                            FromPhase = previous.Phase,
                            ToPhase = phase,
                            Intent = intent,
                            Influences = influences,
                            Tick = currentTick
                        });

                        // Keep a compact rolling history.
                        if (transitions.Length > 16)
                        {
                            transitions.RemoveRange(0, transitions.Length - 16);
                        }
                    }
                }

                moraleState.ValueRW = new GroupMoraleContractState
                {
                    Morale01 = morale01,
                    Cohesion01 = cohesion01,
                    Pressure01 = pressure01,
                    AnchorSecurity01 = anchorSecurity01,
                    GoalCommitment01 = commitment01,
                    Phase = phase,
                    Intent = intent,
                    Influences = influences,
                    LastUpdatedTick = currentTick,
                    PhaseChangedTick = phaseChangedTick
                };

                if (_splinterLookup.HasComponent(entity))
                {
                    var splinter = _splinterLookup[entity];
                    var shouldSplit = GroupMoraleContract.ShouldSplit(phase, cohesion01, anchorSecurity01, in profile.ValueRO);
                    var shouldRejoin = GroupMoraleContract.ShouldRejoin(phase, cohesion01, morale01, commitment01, pressure01, in profile.ValueRO);
                    if (shouldSplit && splinter.IsActive == 0)
                    {
                        splinter.IsActive = 1;
                        splinter.SinceTick = currentTick;
                    }
                    else if (shouldRejoin && splinter.IsActive != 0)
                    {
                        splinter.IsActive = 0;
                        splinter.SinceTick = currentTick;
                    }

                    if (splinter.IsActive != 0)
                    {
                        splinter.RequestedMemberShare01 = math.saturate(0.2f + pressure01 * 0.45f);
                    }

                    _splinterLookup[entity] = splinter;
                }
            }
        }

        private static float ResolveMorale01(in GroupMetrics metrics, in GroupAggregate aggregate)
        {
            var aggregateMorale01 = GroupMoraleContract.NormalizeMorale01(aggregate.AverageMorale);
            if (aggregateMorale01 > 0f)
            {
                return aggregateMorale01;
            }

            return GroupMoraleContract.NormalizeMorale01(metrics.AverageMorale, 1f);
        }

        private float ResolveCohesion01(Entity groupEntity, float aggregateCohesion01)
        {
            var cohesion01 = math.saturate(aggregateCohesion01);
            if (_spreadLookup.HasComponent(groupEntity))
            {
                var spreadCohesion01 = math.saturate(_spreadLookup[groupEntity].CohesionNormalized);
                if (cohesion01 <= 0f)
                {
                    cohesion01 = spreadCohesion01;
                }
                else
                {
                    cohesion01 = math.saturate(math.lerp(cohesion01, spreadCohesion01, 0.35f));
                }
            }

            return cohesion01;
        }

        private static float ResolveCasualtyPressure01(int memberCount, int activeMemberCount)
        {
            if (memberCount <= 0)
            {
                return 0f;
            }

            var activeRatio01 = activeMemberCount / (float)math.max(1, memberCount);
            return math.saturate(1f - activeRatio01);
        }

        private static float ResolveSupplyStress01(float supplyCount)
        {
            return math.saturate((8f - math.max(0f, supplyCount)) / 8f);
        }

        private static float ResolvePressure01(float threat01, float casualtyPressure01, float supplyStress01)
        {
            return math.saturate((threat01 * 0.55f) + (casualtyPressure01 * 0.30f) + (supplyStress01 * 0.15f));
        }

        private float ResolveAnchorSecurity01(Entity groupEntity, bool hasObjective, in GroupObjective objective)
        {
            if (!_transformLookup.HasComponent(groupEntity))
            {
                return 0.5f;
            }

            if (_anchorLookup.HasComponent(groupEntity))
            {
                var anchor = _anchorLookup[groupEntity];
                var groupPos = _transformLookup[groupEntity].Position;

                float3 anchorPos = anchor.AnchorPosition;
                if (anchor.AnchorEntity != Entity.Null && _transformLookup.HasComponent(anchor.AnchorEntity))
                {
                    anchorPos = _transformLookup[anchor.AnchorEntity].Position;
                }
                else if (math.lengthsq(anchorPos) < 0.0001f)
                {
                    anchorPos = anchor.FallbackPosition;
                }

                var tolerance = math.max(0.5f, anchor.AnchorRadius + math.max(0f, anchor.DriftTolerance));
                var distance = math.distance(groupPos, anchorPos);
                if (distance <= tolerance)
                {
                    return 1f;
                }

                var outsideDistance = distance - tolerance;
                return math.saturate(1f - (outsideDistance / (tolerance * 2f)));
            }

            if (hasObjective)
            {
                switch (objective.ObjectiveType)
                {
                    case GroupObjectiveType.Defend:
                    case GroupObjectiveType.Patrol:
                    case GroupObjectiveType.PatrolRoute:
                        return 0.75f;
                    case GroupObjectiveType.Retreat:
                        return 0.35f;
                }
            }

            return 0.55f;
        }

        private float ResolveGoalCommitment01(
            Entity groupEntity,
            bool hasObjective,
            in GroupObjective objective,
            float previousCommitment01,
            in GroupMoraleContractProfile profile,
            uint currentTick,
            float deltaTime)
        {
            var objectiveCommitment01 = hasObjective ? math.saturate(objective.Priority / 255f) : 0f;
            var gain = math.max(0f, profile.CommitmentGainPerSecond) * deltaTime;
            var decay = math.max(0f, profile.CommitmentDecayPerSecond) * deltaTime;
            var commitment01 = objectiveCommitment01 >= previousCommitment01
                ? math.min(objectiveCommitment01, previousCommitment01 + gain)
                : math.max(objectiveCommitment01, previousCommitment01 - decay);

            if (!_goalLookup.HasComponent(groupEntity))
            {
                return commitment01;
            }

            var goal = _goalLookup[groupEntity];
            if (hasObjective)
            {
                var goalChanged = goal.GoalType != objective.ObjectiveType
                                  || goal.GoalEntity != objective.TargetEntity
                                  || math.distancesq(goal.GoalPosition, objective.TargetPosition) > 0.01f;

                var canRetarget = goal.LastRetargetTick == 0
                                  || currentTick - goal.LastRetargetTick >= profile.RetargetCooldownTicks;
                if (goalChanged && canRetarget)
                {
                    goal.GoalType = objective.ObjectiveType;
                    goal.GoalEntity = objective.TargetEntity;
                    goal.GoalPosition = objective.TargetPosition;
                    goal.LastRetargetTick = currentTick;
                    goal.CommitUntilTick = currentTick + profile.MinGoalCommitTicks;
                }

                if (currentTick < goal.CommitUntilTick)
                {
                    commitment01 = math.max(commitment01, math.min(0.95f, objectiveCommitment01 * 0.85f));
                }
            }

            goal.Commitment01 = math.saturate(commitment01);
            _goalLookup[groupEntity] = goal;
            return goal.Commitment01;
        }

        private GroupMoraleInfluence ResolveInfluences(
            Entity groupEntity,
            bool hasObjective,
            in GroupObjective objective,
            in GroupMoraleContractProfile profile,
            in GroupMoraleContractState previous,
            float morale01,
            float cohesion01,
            float pressure01,
            float anchorSecurity01,
            float commitment01,
            float threat01,
            float casualtyPressure01,
            float supplyStress01,
            uint currentTick)
        {
            var influences = GroupMoraleInfluence.None;

            if (casualtyPressure01 >= 0.25f)
            {
                influences |= GroupMoraleInfluence.CasualtyPressure;
            }

            if (threat01 >= 0.30f)
            {
                influences |= GroupMoraleInfluence.ThreatPressure;
            }

            if (anchorSecurity01 <= profile.SplitThreshold01)
            {
                influences |= GroupMoraleInfluence.AnchorLoss;
            }

            if (supplyStress01 >= 0.35f)
            {
                influences |= GroupMoraleInfluence.SupplyStress;
            }

            if (cohesion01 <= profile.SplitThreshold01)
            {
                influences |= GroupMoraleInfluence.Isolation;
            }

            if (hasObjective && objective.ExpirationTick > 0 && currentTick >= objective.ExpirationTick)
            {
                influences |= GroupMoraleInfluence.GoalFailure;
            }
            else if (!hasObjective && previous.GoalCommitment01 > 0.65f)
            {
                influences |= GroupMoraleInfluence.GoalFailure;
            }

            if (IsLeaderCompromised(groupEntity))
            {
                influences |= GroupMoraleInfluence.LeaderLoss;
            }

            if (morale01 >= previous.Morale01 + 0.04f && cohesion01 >= previous.Cohesion01 + 0.02f)
            {
                influences |= GroupMoraleInfluence.CohesionRecovery;
            }

            if (pressure01 + 0.05f < previous.Pressure01 && morale01 >= previous.Morale01)
            {
                influences |= GroupMoraleInfluence.VictoryRecovery;
            }

            if (commitment01 > previous.GoalCommitment01 + 0.05f && morale01 >= previous.Morale01)
            {
                influences |= GroupMoraleInfluence.FaithRecovery;
            }

            return influences;
        }

        private bool IsLeaderCompromised(Entity groupEntity)
        {
            if (!_identityLookup.HasComponent(groupEntity))
            {
                return false;
            }

            var identity = _identityLookup[groupEntity];
            if (identity.LeaderEntity == Entity.Null)
            {
                return true;
            }

            if (!_memberLookup.HasBuffer(groupEntity))
            {
                return false;
            }

            var members = _memberLookup[groupEntity];
            for (int i = 0; i < members.Length; i++)
            {
                var member = members[i];
                if (member.MemberEntity == identity.LeaderEntity)
                {
                    return (member.Flags & GroupMemberFlags.Active) == 0;
                }
            }

            return true;
        }
    }
}
