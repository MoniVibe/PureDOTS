using System.Collections.Generic;
using PureDOTS.Shared;
using Unity.Mathematics;

namespace PureDOTS.AI.AggregateECS.Components
{
    /// <summary>
    /// Type of aggregate entity.
    /// </summary>
    public enum AggregateType : byte
    {
        Village = 0,
        Fleet = 1,
        Band = 2
    }

    /// <summary>
    /// Aggregate statistics collected from member agents.
    /// </summary>
    public struct AggregateStats
    {
        public float Food; // Total or average food level
        public float Morale; // Average morale
        public float Defense; // Defense capability score
        public int Population; // Number of members
        public float Health; // Average health
        public float Energy; // Average energy
    }

    /// <summary>
    /// Aggregate entity component for DefaultEcs world.
    /// Represents a group of agents (village, fleet, band) with collective goals.
    /// </summary>
    public class AggregateEntity
    {
        public AgentGuid AggregateGuid;
        public AggregateType Type;
        public List<AgentGuid> MemberGuids;
        public AggregateStats Stats;
        public float3 CenterPosition; // Approximate center of aggregate

        public AggregateEntity()
        {
            MemberGuids = new List<AgentGuid>();
            Stats = new AggregateStats();
            CenterPosition = float3.zero;
        }

        public void AddMember(AgentGuid agentGuid)
        {
            if (!MemberGuids.Contains(agentGuid))
            {
                MemberGuids.Add(agentGuid);
                Stats.Population = MemberGuids.Count;
            }
        }

        public void RemoveMember(AgentGuid agentGuid)
        {
            MemberGuids.Remove(agentGuid);
            Stats.Population = MemberGuids.Count;
        }

        public bool HasMember(AgentGuid agentGuid)
        {
            return MemberGuids.Contains(agentGuid);
        }
    }

    /// <summary>
    /// Aggregate intent component - group-level goals and distribution ratios.
    /// </summary>
    public class AggregateIntent
    {
        public string CurrentGoal; // "Harvest", "Defend", "Patrol", "Rest", etc.
        public float Priority; // 0-1, how important this goal is
        public float3 TargetPosition; // Optional target position for goal
        public Dictionary<string, float> DistributionRatios; // e.g., "Farm"=0.6, "Defend"=0.3, "Rest"=0.1

        public AggregateIntent()
        {
            CurrentGoal = "Idle";
            Priority = 0f;
            TargetPosition = float3.zero;
            DistributionRatios = new Dictionary<string, float>();
        }

        public void SetDistribution(string goalType, float ratio)
        {
            DistributionRatios[goalType] = math.clamp(ratio, 0f, 1f);
        }

        public float GetDistribution(string goalType)
        {
            return DistributionRatios.TryGetValue(goalType, out var ratio) ? ratio : 0f;
        }
    }
}

