using System.Collections.Generic;
using DefaultEcs;
using DefaultEcs.System;
using PureDOTS.AI.MindECS.Components;
using PureDOTS.Runtime.Bridges;
using PureDOTS.Runtime.Cognitive;
using PureDOTS.Runtime.IntergroupRelations;
using PureDOTS.Shared;
using Unity.Mathematics;
using UnityEngine;

namespace PureDOTS.AI.MindECS.Systems
{
    /// <summary>
    /// Meta-learning system for aggregate entities (fleets, bands, empires).
    /// Maintains statistical models of subordinate performance.
    /// Average success rate vs opponent factions (weighted by recency).
    /// Shared via "Doctrine Buffs" that trickle down to subordinates.
    /// Doctrine modifiers applied when encountering familiar cultures.
    /// </summary>
    public class AggregateLearningSystem : AEntitySetSystem<float>
    {
        private float _lastUpdateTime;
        private const float UpdateInterval = 5f; // 5 second updates (meta-learning is slower)
        private AgentSyncBus _syncBus;
        private Dictionary<AgentGuid, AggregateDoctrineData> _doctrineCache;

        public AggregateLearningSystem(World world, AgentSyncBus syncBus)
            : base(world.GetEntities().With<CognitiveMemory>().AsSet())
        {
            _lastUpdateTime = 0f;
            _syncBus = syncBus;
            _doctrineCache = new Dictionary<AgentGuid, AggregateDoctrineData>();
        }

        protected override void Update(float deltaTime, in Entity entity)
        {
            var currentTime = Time.time;

            // Throttle updates (5 second intervals)
            if (currentTime - _lastUpdateTime < UpdateInterval)
            {
                return;
            }

            if (!World.Has<CognitiveMemory>(entity) || !World.Has<AgentGuid>(entity))
            {
                return;
            }

            var memory = World.Get<CognitiveMemory>(entity);
            var agentGuid = World.Get<AgentGuid>(entity);

            // Check if this entity is an aggregate (fleet/band/empire)
            // In production, this would check for AggregateEntity component or similar
            var isAggregate = CheckIfAggregate(entity, agentGuid);

            if (isAggregate)
            {
                // Update aggregate doctrine from member experiences
                UpdateAggregateDoctrine(entity, agentGuid, memory);
            }

            _lastUpdateTime = currentTime;
        }

        private bool CheckIfAggregate(Entity entity, AgentGuid agentGuid)
        {
            // In production, this would check for AggregateEntity component
            // or check if entity has subordinates/members
            // For now, we'll check the doctrine cache
            return _doctrineCache.ContainsKey(agentGuid);
        }

        private void UpdateAggregateDoctrine(Entity entity, AgentGuid aggregateGuid, CognitiveMemory memory)
        {
            if (!_doctrineCache.TryGetValue(aggregateGuid, out var doctrine))
            {
                doctrine = new AggregateDoctrineData();
                _doctrineCache[aggregateGuid] = doctrine;
            }

            // Process member experiences to update doctrine
            if (memory.InteractionDigests == null || memory.InteractionDigests.Count == 0)
            {
                return;
            }

            // Aggregate success rates per opponent culture/faction
            var opponentStats = new Dictionary<ushort, OpponentStats>();

            foreach (var digest in memory.InteractionDigests)
            {
                // Extract culture ID from interaction (would need to be stored in digest)
                // For now, we'll use a placeholder
                ushort opponentCultureId = 0; // Would come from digest metadata

                if (!opponentStats.ContainsKey(opponentCultureId))
                {
                    opponentStats[opponentCultureId] = new OpponentStats();
                }

                var stats = opponentStats[opponentCultureId];

                // Calculate success rate from positive/negative deltas
                var outcome = digest.PositiveDelta - digest.NegativeDelta;
                var success = outcome > 0f ? 1f : 0f;

                // Weight by recency (more recent = higher weight)
                var recencyWeight = CalculateRecencyWeight(digest.InteractionTick);
                stats.TotalSuccess += success * recencyWeight;
                stats.TotalEngagements += recencyWeight;
            }

            // Update doctrine with average success rates
            foreach (var kvp in opponentStats)
            {
                var cultureId = kvp.Key;
                var stats = kvp.Value;

                if (stats.TotalEngagements > 0f)
                {
                    var successRate = stats.TotalSuccess / stats.TotalEngagements;

                    // Update doctrine: lerp(old, new, learningRate)
                    const float learningRate = 0.1f;
                    if (!doctrine.OpponentSuccessRates.ContainsKey(cultureId))
                    {
                        doctrine.OpponentSuccessRates[cultureId] = successRate;
                    }
                    else
                    {
                        var oldRate = doctrine.OpponentSuccessRates[cultureId];
                        doctrine.OpponentSuccessRates[cultureId] = math.lerp(oldRate, successRate, learningRate);
                    }
                }
            }

            _doctrineCache[aggregateGuid] = doctrine;

            // Apply doctrine buffs to subordinates
            ApplyDoctrineBuffs(aggregateGuid, doctrine);
        }

        private float CalculateRecencyWeight(uint interactionTick)
        {
            // Weight decreases with age
            // More recent interactions have higher weight
            var currentTick = (uint)Time.time; // Would use actual tick counter
            var age = currentTick - interactionTick;
            var maxAge = 1000u; // Max age for weighting

            if (age > maxAge)
            {
                return 0f;
            }

            // Linear decay: weight = 1 - (age / maxAge)
            return 1f - (age / (float)maxAge);
        }

        private void ApplyDoctrineBuffs(AgentGuid aggregateGuid, AggregateDoctrineData doctrine)
        {
            // Doctrine buffs trickle down to subordinates
            // These modify damage, defense, behavior when encountering familiar cultures

            // In production, this would:
            // 1. Find all subordinate entities (members of this aggregate)
            // 2. Apply doctrine modifiers as components or buffs
            // 3. Modifiers affect combat effectiveness vs specific cultures

            // For now, we'll store doctrine in cache and it would be applied by other systems
            // when subordinates encounter opponents

            // EXTENDED: Aggregate learning for procedural knowledge
            // Compute frequency map of successful strategies from procedural memories
            // Convert to Cultural Doctrine Buffs that trickle to future generations
            
            // When many entities repeat success, aggregate system computes frequency map
            // of successful strategies and converts into Cultural Doctrine Buffs
            // Example: "Our miners use supports to reach ore veins"
            // Doctrine trickles down to future generations (inherit baseline procedural knowledge)
            
            // This extends the existing aggregate learning to include procedural memory patterns
            // Successful action chains from procedural memories can be aggregated and shared
        }

        private struct AggregateDoctrineData
        {
            public Dictionary<ushort, float> OpponentSuccessRates; // CultureId -> SuccessRate
            public Dictionary<ushort, float> DoctrineModifiers; // CultureId -> ModifierValue

            public AggregateDoctrineData()
            {
                OpponentSuccessRates = new Dictionary<ushort, float>();
                DoctrineModifiers = new Dictionary<ushort, float>();
            }
        }

        private struct OpponentStats
        {
            public float TotalSuccess;
            public float TotalEngagements;
        }
    }
}

