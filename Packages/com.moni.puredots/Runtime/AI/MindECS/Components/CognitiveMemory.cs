using System.Collections.Generic;
using Unity.Mathematics;
using PureDOTS.Shared;
using PureDOTS.Runtime.Bridges;
using PureDOTS.Runtime.AI;

namespace PureDOTS.AI.MindECS.Components
{
    /// <summary>
    /// Episodic memory, semantic knowledge, and relationship tracking for cognitive agents.
    /// Managed class component for DefaultEcs (Mind ECS layer).
    /// </summary>
    public class CognitiveMemory
    {
        public struct MemoryEvent
        {
            public uint TickNumber;
            public string EventType;
            public Dictionary<string, object> Data;
        }

        // Episodic memory: recent events
        public Queue<MemoryEvent> EpisodicMemory;
        public int MaxEpisodicMemorySize;

        // Semantic knowledge: facts about the world
        public Dictionary<string, object> SemanticKnowledge;

        // Relationship tracking: opinions about other agents
        public Dictionary<AgentGuid, float> RelationshipScores; // -1 to 1 (enemy to ally)

        // Deception memory: records of deception events
        public List<MemoryEvent> DeceptionHistory;

        // Perception memory: recent sensor readings
        public List<Percept> RecentPercepts;
        public int MaxPerceptHistorySize;

        // Aggregated percept history: confidence per sensor type
        public Dictionary<SensorType, float> PerceptHistory;

        // Interaction digests: compressed event data for reputation/emotion
        public List<InteractionDigest> InteractionDigests;
        public int MaxInteractionDigestSize;

        public CognitiveMemory()
        {
            EpisodicMemory = new Queue<MemoryEvent>();
            MaxEpisodicMemorySize = 100;
            SemanticKnowledge = new Dictionary<string, object>();
            RelationshipScores = new Dictionary<AgentGuid, float>();
            DeceptionHistory = new List<MemoryEvent>();
            RecentPercepts = new List<Percept>();
            MaxPerceptHistorySize = 50;
            PerceptHistory = new Dictionary<SensorType, float>();
            InteractionDigests = new List<InteractionDigest>();
            MaxInteractionDigestSize = 200;
        }

        public void AddEpisodicMemory(uint tickNumber, string eventType, Dictionary<string, object> data)
        {
            var memory = new MemoryEvent
            {
                TickNumber = tickNumber,
                EventType = eventType,
                Data = data ?? new Dictionary<string, object>()
            };

            EpisodicMemory.Enqueue(memory);
            while (EpisodicMemory.Count > MaxEpisodicMemorySize)
            {
                EpisodicMemory.Dequeue();
            }
        }

        public void UpdateRelationship(AgentGuid agentGuid, float score)
        {
            RelationshipScores[agentGuid] = math.clamp(score, -1f, 1f);
        }

        /// <summary>
        /// Add a percept to recent memory and update aggregated history.
        /// </summary>
        public void AddPercept(Percept percept)
        {
            RecentPercepts.Add(percept);
            
            // Maintain size limit
            while (RecentPercepts.Count > MaxPerceptHistorySize)
            {
                RecentPercepts.RemoveAt(0);
            }

            // Update aggregated confidence per sensor type (weighted average)
            if (PerceptHistory.ContainsKey(percept.Type))
            {
                var currentConfidence = PerceptHistory[percept.Type];
                // Exponential moving average: new = 0.7 * old + 0.3 * new
                PerceptHistory[percept.Type] = currentConfidence * 0.7f + percept.Confidence * 0.3f;
            }
            else
            {
                PerceptHistory[percept.Type] = percept.Confidence;
            }
        }

        /// <summary>
        /// Get aggregated confidence for a sensor type.
        /// </summary>
        public float GetSensorConfidence(SensorType sensorType)
        {
            return PerceptHistory.TryGetValue(sensorType, out var confidence) ? confidence : 0f;
        }

        /// <summary>
        /// Clear old percepts beyond the retention window.
        /// </summary>
        public void PruneOldPercepts(uint currentTick, uint retentionTicks = 100)
        {
            RecentPercepts.RemoveAll(p => currentTick - p.TickNumber > retentionTicks);
        }

        /// <summary>
        /// Add interaction digest for reputation/emotion tracking.
        /// </summary>
        public void AddInteractionDigest(InteractionDigest digest)
        {
            InteractionDigests.Add(digest);
            
            // Maintain size limit
            while (InteractionDigests.Count > MaxInteractionDigestSize)
            {
                InteractionDigests.RemoveAt(0);
            }
        }
    }

    /// <summary>
    /// Interaction digest for CognitiveMemory.
    /// </summary>
    public struct InteractionDigest
    {
        public AgentGuid InteractorGuid;
        public AgentGuid TargetGuid;
        public float PositiveDelta;
        public float NegativeDelta;
        public float Weight;
        public uint InteractionTick;
        public InteractionType Type;
    }

    /// <summary>
    /// Types of interactions.
    /// </summary>
    public enum InteractionType : byte
    {
        Help = 0,
        Harm = 1,
        Trade = 2,
        Social = 3,
        Combat = 4
    }
}

