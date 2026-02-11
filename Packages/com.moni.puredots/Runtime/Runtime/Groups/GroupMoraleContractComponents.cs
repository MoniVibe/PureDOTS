using System;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Groups
{
    /// <summary>
    /// Coarse morale phases used by downstream formation/goal/splinter logic.
    /// </summary>
    public enum GroupMoralePhase : byte
    {
        Resilient = 0,
        Steady = 1,
        Strained = 2,
        Breaking = 3,
        Routed = 4
    }

    /// <summary>
    /// Action lane selected from morale phase and supporting signals.
    /// </summary>
    public enum GroupMoraleIntent : byte
    {
        Hold = 0,
        TightenFormation = 1,
        SeekAnchor = 2,
        SplitAndStabilize = 3,
        Regroup = 4,
        Retreat = 5
    }

    /// <summary>
    /// Explainability flags for morale transitions and intent selection.
    /// </summary>
    [Flags]
    public enum GroupMoraleInfluence : ushort
    {
        None = 0,
        CasualtyPressure = 1 << 0,
        ThreatPressure = 1 << 1,
        AnchorLoss = 1 << 2,
        GoalFailure = 1 << 3,
        Isolation = 1 << 4,
        SupplyStress = 1 << 5,
        LeaderLoss = 1 << 6,
        CohesionRecovery = 1 << 7,
        VictoryRecovery = 1 << 8,
        FaithRecovery = 1 << 9
    }

    /// <summary>
    /// Shared knobs for morale-driven group behavior.
    /// Values are normalized 0..1 unless otherwise noted.
    /// </summary>
    public struct GroupMoraleContractProfile : IComponentData
    {
        public float RoutedThreshold01;
        public float BreakingThreshold01;
        public float StrainedThreshold01;
        public float SteadyThreshold01;
        public float SplitThreshold01;
        public float RejoinThreshold01;
        public float CommitmentGainPerSecond;
        public float CommitmentDecayPerSecond;
        public uint MinGoalCommitTicks;
        public uint RetargetCooldownTicks;

        public static GroupMoraleContractProfile Default => new GroupMoraleContractProfile
        {
            RoutedThreshold01 = 0.15f,
            BreakingThreshold01 = 0.30f,
            StrainedThreshold01 = 0.45f,
            SteadyThreshold01 = 0.65f,
            SplitThreshold01 = 0.40f,
            RejoinThreshold01 = 0.62f,
            CommitmentGainPerSecond = 0.12f,
            CommitmentDecayPerSecond = 0.18f,
            MinGoalCommitTicks = 240,
            RetargetCooldownTicks = 180
        };
    }

    /// <summary>
    /// Evaluated morale contract state for a group.
    /// </summary>
    public struct GroupMoraleContractState : IComponentData
    {
        public float Morale01;
        public float Cohesion01;
        public float Pressure01;
        public float AnchorSecurity01;
        public float GoalCommitment01;
        public GroupMoralePhase Phase;
        public GroupMoraleIntent Intent;
        public GroupMoraleInfluence Influences;
        public uint LastUpdatedTick;
        public uint PhaseChangedTick;
    }

    /// <summary>
    /// Anchor context consumed by morale/formation decisions.
    /// </summary>
    public struct GroupAnchorContractState : IComponentData
    {
        public Entity AnchorEntity;
        public float3 AnchorPosition;
        public float3 FallbackPosition;
        public float AnchorRadius;
        public float DriftTolerance;
    }

    /// <summary>
    /// Explicit goal-commitment contract separate from objective payload.
    /// </summary>
    public struct GroupGoalCommitmentContract : IComponentData
    {
        public GroupObjectiveType GoalType;
        public Entity GoalEntity;
        public float3 GoalPosition;
        public float Commitment01;
        public uint CommitUntilTick;
        public uint LastRetargetTick;
    }

    /// <summary>
    /// Splinter/rejoin state for deterministic group decomposition.
    /// </summary>
    public struct GroupSplinterContractState : IComponentData
    {
        public Entity ParentGroup;
        public Entity ChildGroup;
        public float RequestedMemberShare01;
        public float ActualMemberShare01;
        public byte IsActive;
        public uint SinceTick;
    }

    /// <summary>
    /// Event stream for morale phase changes and selected intent.
    /// </summary>
    [InternalBufferCapacity(4)]
    public struct GroupMoraleTransitionEvent : IBufferElementData
    {
        public GroupMoralePhase FromPhase;
        public GroupMoralePhase ToPhase;
        public GroupMoraleIntent Intent;
        public GroupMoraleInfluence Influences;
        public uint Tick;
    }

    /// <summary>
    /// Pure contract helpers. Safe to use from jobs and tests.
    /// </summary>
    public static class GroupMoraleContract
    {
        public const float DefaultEntityMoraleMax = 1000f;

        public static float NormalizeMorale01(float morale, float maxMorale = DefaultEntityMoraleMax)
        {
            if (maxMorale <= 0f)
            {
                return 0f;
            }

            if (morale <= 1f)
            {
                return math.saturate(morale);
            }

            return math.saturate(morale / maxMorale);
        }

        public static GroupMoralePhase ResolvePhase(float morale01, in GroupMoraleContractProfile profile)
        {
            morale01 = math.saturate(morale01);

            if (morale01 <= math.max(0f, profile.RoutedThreshold01))
            {
                return GroupMoralePhase.Routed;
            }

            if (morale01 <= math.max(profile.RoutedThreshold01, profile.BreakingThreshold01))
            {
                return GroupMoralePhase.Breaking;
            }

            if (morale01 <= math.max(profile.BreakingThreshold01, profile.StrainedThreshold01))
            {
                return GroupMoralePhase.Strained;
            }

            if (morale01 <= math.max(profile.StrainedThreshold01, profile.SteadyThreshold01))
            {
                return GroupMoralePhase.Steady;
            }

            return GroupMoralePhase.Resilient;
        }

        public static GroupMoraleIntent ResolveIntent(
            GroupMoralePhase phase,
            float cohesion01,
            float anchorSecurity01,
            float commitment01,
            in GroupMoraleContractProfile profile)
        {
            cohesion01 = math.saturate(cohesion01);
            anchorSecurity01 = math.saturate(anchorSecurity01);
            commitment01 = math.saturate(commitment01);

            if (phase == GroupMoralePhase.Routed)
            {
                return GroupMoraleIntent.Retreat;
            }

            if (phase == GroupMoralePhase.Breaking)
            {
                if (cohesion01 <= profile.SplitThreshold01 || anchorSecurity01 <= profile.SplitThreshold01)
                {
                    return GroupMoraleIntent.SplitAndStabilize;
                }

                return GroupMoraleIntent.SeekAnchor;
            }

            if (phase == GroupMoralePhase.Strained)
            {
                return GroupMoraleIntent.TightenFormation;
            }

            if (phase == GroupMoralePhase.Steady && cohesion01 >= profile.RejoinThreshold01 && commitment01 >= profile.RejoinThreshold01)
            {
                return GroupMoraleIntent.Regroup;
            }

            return GroupMoraleIntent.Hold;
        }

        public static bool ShouldSplit(
            GroupMoralePhase phase,
            float cohesion01,
            float anchorSecurity01,
            in GroupMoraleContractProfile profile)
        {
            if (phase < GroupMoralePhase.Breaking)
            {
                return false;
            }

            cohesion01 = math.saturate(cohesion01);
            anchorSecurity01 = math.saturate(anchorSecurity01);
            return cohesion01 <= profile.SplitThreshold01 || anchorSecurity01 <= profile.SplitThreshold01;
        }

        public static bool ShouldRejoin(
            GroupMoralePhase phase,
            float cohesion01,
            float morale01,
            float commitment01,
            float pressure01,
            in GroupMoraleContractProfile profile)
        {
            if (phase >= GroupMoralePhase.Breaking)
            {
                return false;
            }

            cohesion01 = math.saturate(cohesion01);
            morale01 = math.saturate(morale01);
            commitment01 = math.saturate(commitment01);
            pressure01 = math.saturate(pressure01);

            return cohesion01 >= profile.RejoinThreshold01
                   && morale01 >= profile.SteadyThreshold01
                   && commitment01 >= profile.RejoinThreshold01
                   && pressure01 <= 0.4f;
        }
    }
}
