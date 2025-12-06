using System.Collections.Generic;
using DefaultEcs;
using DefaultEcs.System;
using PureDOTS.AI.MindECS.Components;
using PureDOTS.Runtime.Bridges;
using PureDOTS.Runtime.Cognitive;
using PureDOTS.Shared;
using Unity.Mathematics;
using UnityEngine;

namespace PureDOTS.AI.MindECS.Systems
{
    /// <summary>
    /// Compresses memories into histograms (per culture, per tactic).
    /// Keeps last N (64) distinct entities interacted with.
    /// Older memories merge into aggregate "racial" or "factional" impressions.
    /// Batch updates every 10-30s tick.
    /// </summary>
    public class MemoryCompressionSystem : AEntitySetSystem<float>
    {
        private float _lastUpdateTime;
        private const float UpdateInterval = 10f; // 10 seconds (batched updates)
        private const int MaxDistinctEntities = 64; // Keep last 64 distinct entities
        private AgentSyncBus _syncBus;

        public MemoryCompressionSystem(World world, AgentSyncBus syncBus)
            : base(world.GetEntities().With<CognitiveMemory>().AsSet())
        {
            _lastUpdateTime = 0f;
            _syncBus = syncBus;
        }

        protected override void Update(float deltaTime, in Entity entity)
        {
            var currentTime = Time.time;

            // Throttle updates (batched every 10 seconds)
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

            // Compress memories
            CompressMemories(memory, currentTime);

            _lastUpdateTime = currentTime;
        }

        private void CompressMemories(CognitiveMemory memory, float currentTime)
        {
            if (memory.InteractionDigests == null || memory.InteractionDigests.Count == 0)
            {
                return;
            }

            // Track distinct entities interacted with
            var distinctEntities = new Dictionary<AgentGuid, int>(); // Guid -> count
            var entityToCultureMap = new Dictionary<AgentGuid, ushort>(); // Guid -> CultureId

            // First pass: count interactions per entity and map to cultures
            foreach (var digest in memory.InteractionDigests)
            {
                if (!distinctEntities.ContainsKey(digest.InteractorGuid))
                {
                    distinctEntities[digest.InteractorGuid] = 0;
                }
                distinctEntities[digest.InteractorGuid]++;

                // Map entity to culture (would need culture ID stored in digest)
                // For now, use placeholder
                if (!entityToCultureMap.ContainsKey(digest.InteractorGuid))
                {
                    entityToCultureMap[digest.InteractorGuid] = 0; // Would get from digest metadata
                }
            }

            // Build histograms per culture
            var cultureHistograms = new Dictionary<ushort, CultureHistogram>();

            foreach (var digest in memory.InteractionDigests)
            {
                var cultureId = entityToCultureMap.GetValueOrDefault(digest.InteractorGuid, 0);

                if (!cultureHistograms.ContainsKey(cultureId))
                {
                    cultureHistograms[cultureId] = new CultureHistogram
                    {
                        CultureId = cultureId,
                        TotalInteractions = 0,
                        PositiveOutcomes = 0f,
                        NegativeOutcomes = 0f,
                        AverageOutcome = 0f
                    };
                }

                var histogram = cultureHistograms[cultureId];
                histogram.TotalInteractions++;

                var outcome = digest.PositiveDelta - digest.NegativeDelta;
                if (outcome > 0f)
                {
                    histogram.PositiveOutcomes += outcome;
                }
                else
                {
                    histogram.NegativeOutcomes += math.abs(outcome);
                }

                cultureHistograms[cultureId] = histogram;
            }

            // Calculate average outcomes
            foreach (var cultureId in cultureHistograms.Keys)
            {
                var histogram = cultureHistograms[cultureId];
                if (histogram.TotalInteractions > 0)
                {
                    histogram.AverageOutcome = (histogram.PositiveOutcomes - histogram.NegativeOutcomes) / histogram.TotalInteractions;
                }
                cultureHistograms[cultureId] = histogram;
            }

            // Keep only recent distinct entities (last N)
            var sortedEntities = new List<KeyValuePair<AgentGuid, int>>(distinctEntities);
            sortedEntities.Sort((a, b) => b.Value.CompareTo(a.Value)); // Sort by interaction count

            var entitiesToKeep = new HashSet<AgentGuid>();
            var keepCount = math.min(MaxDistinctEntities, sortedEntities.Count);
            for (int i = 0; i < keepCount; i++)
            {
                entitiesToKeep.Add(sortedEntities[i].Key);
            }

            // Remove old digests for entities not in keep list
            // Merge their data into histograms
            for (int i = memory.InteractionDigests.Count - 1; i >= 0; i--)
            {
                var digest = memory.InteractionDigests[i];

                if (!entitiesToKeep.Contains(digest.InteractorGuid))
                {
                    // Entity is old, merge into histogram and remove digest
                    // Histogram already updated above, just remove digest
                    memory.InteractionDigests.RemoveAt(i);
                }
            }

            // Store histograms in memory (would be stored in ExperienceHistogram buffer in Body ECS)
            // For now, we'll store in CognitiveMemory as metadata
        }

        private struct CultureHistogram
        {
            public ushort CultureId;
            public int TotalInteractions;
            public float PositiveOutcomes;
            public float NegativeOutcomes;
            public float AverageOutcome;
        }
    }
}

