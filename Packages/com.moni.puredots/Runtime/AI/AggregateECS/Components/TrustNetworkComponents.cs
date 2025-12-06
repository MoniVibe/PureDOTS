using System.Collections.Generic;
using PureDOTS.Shared;

namespace PureDOTS.AI.AggregateECS.Components
{
    /// <summary>
    /// Trust network component for aggregates.
    /// Maintains sparse relationship matrices for group-level trust tracking.
    /// Based on Kozlowski et al. (2016) trust network patterns.
    /// </summary>
    public class TrustNetwork
    {
        /// <summary>
        /// Dictionary mapping AgentGuid -> Trust value (0-1).
        /// Sparse matrix: only stores relationships with known agents.
        /// </summary>
        public Dictionary<AgentGuid, float> TrustMap;

        /// <summary>
        /// Dictionary mapping AgentGuid -> Reputation value (0-1).
        /// Aggregate perception of other agents.
        /// </summary>
        public Dictionary<AgentGuid, float> ReputationMap;

        /// <summary>
        /// Dictionary mapping AgentGuid -> Cooperation bias (-1 to 1).
        /// Positive = cooperative, negative = competitive.
        /// </summary>
        public Dictionary<AgentGuid, float> CooperationBiasMap;

        /// <summary>
        /// Last update tick for trust network.
        /// Used for temporal batching.
        /// </summary>
        public uint LastUpdateTick;

        public TrustNetwork()
        {
            TrustMap = new Dictionary<AgentGuid, float>();
            ReputationMap = new Dictionary<AgentGuid, float>();
            CooperationBiasMap = new Dictionary<AgentGuid, float>();
            LastUpdateTick = 0;
        }

        /// <summary>
        /// Gets trust value for an agent, or default if not found.
        /// </summary>
        public float GetTrust(AgentGuid agentGuid, float defaultValue = 0.5f)
        {
            return TrustMap.TryGetValue(agentGuid, out var trust) ? trust : defaultValue;
        }

        /// <summary>
        /// Gets reputation value for an agent, or default if not found.
        /// </summary>
        public float GetReputation(AgentGuid agentGuid, float defaultValue = 0.5f)
        {
            return ReputationMap.TryGetValue(agentGuid, out var reputation) ? reputation : defaultValue;
        }

        /// <summary>
        /// Gets cooperation bias for an agent, or default if not found.
        /// </summary>
        public float GetCooperationBias(AgentGuid agentGuid, float defaultValue = 0f)
        {
            return CooperationBiasMap.TryGetValue(agentGuid, out var bias) ? bias : defaultValue;
        }

        /// <summary>
        /// Sets trust value for an agent.
        /// </summary>
        public void SetTrust(AgentGuid agentGuid, float trust)
        {
            TrustMap[agentGuid] = UnityEngine.Mathf.Clamp01(trust);
        }

        /// <summary>
        /// Sets reputation value for an agent.
        /// </summary>
        public void SetReputation(AgentGuid agentGuid, float reputation)
        {
            ReputationMap[agentGuid] = UnityEngine.Mathf.Clamp01(reputation);
        }

        /// <summary>
        /// Sets cooperation bias for an agent.
        /// </summary>
        public void SetCooperationBias(AgentGuid agentGuid, float bias)
        {
            CooperationBiasMap[agentGuid] = UnityEngine.Mathf.Clamp(bias, -1f, 1f);
        }

        /// <summary>
        /// Prunes trust network to keep only N nearest neighbors.
        /// Performance optimization: limits relationship tracking.
        /// </summary>
        public void PruneToNearestNeighbors(int maxNeighbors)
        {
            if (TrustMap.Count <= maxNeighbors)
            {
                return; // No pruning needed
            }

            // Sort by trust value and keep top N
            var sorted = new List<System.Collections.Generic.KeyValuePair<AgentGuid, float>>(TrustMap);
            sorted.Sort((a, b) => b.Value.CompareTo(a.Value)); // Sort descending

            // Keep only top N
            var toKeep = new HashSet<AgentGuid>();
            for (int i = 0; i < maxNeighbors && i < sorted.Count; i++)
            {
                toKeep.Add(sorted[i].Key);
            }

            // Remove entries not in top N
            var keysToRemove = new List<AgentGuid>();
            foreach (var key in TrustMap.Keys)
            {
                if (!toKeep.Contains(key))
                {
                    keysToRemove.Add(key);
                }
            }

            foreach (var key in keysToRemove)
            {
                TrustMap.Remove(key);
                ReputationMap.Remove(key);
                CooperationBiasMap.Remove(key);
            }
        }
    }
}

