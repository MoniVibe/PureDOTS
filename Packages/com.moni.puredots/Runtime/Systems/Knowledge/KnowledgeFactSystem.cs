using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Knowledge;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Systems.Knowledge
{
    /// <summary>
    /// Applies knowledge fact requests and decays fact confidence over time.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct KnowledgeFactSystem : ISystem
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

            var config = SystemAPI.TryGetSingleton<KnowledgeFactConfig>(out var configSingleton)
                ? configSingleton
                : KnowledgeFactConfig.Default;

            foreach (var (requests, entity) in SystemAPI.Query<DynamicBuffer<KnowledgeFactRequest>>().WithEntityAccess())
            {
                if (requests.Length == 0)
                {
                    continue;
                }

                DynamicBuffer<KnowledgeFact> facts;
                if (!state.EntityManager.HasBuffer<KnowledgeFact>(entity))
                {
                    facts = state.EntityManager.AddBuffer<KnowledgeFact>(entity);
                }
                else
                {
                    facts = state.EntityManager.GetBuffer<KnowledgeFact>(entity);
                }

                for (int i = 0; i < requests.Length; i++)
                {
                    ApplyFactRequest(ref facts, requests[i], currentTick);
                }

                PruneFacts(ref facts, config.MaxFacts);
                requests.Clear();
            }

            foreach (var facts in SystemAPI.Query<DynamicBuffer<KnowledgeFact>>())
            {
                var buffer = facts;
                DecayFacts(ref buffer, currentTick, config.MinConfidence);
                PruneFacts(ref buffer, config.MaxFacts);
            }
        }

        private static void ApplyFactRequest(ref DynamicBuffer<KnowledgeFact> facts, in KnowledgeFactRequest request, uint currentTick)
        {
            if (request.FactId.Length == 0)
            {
                return;
            }

            for (int i = 0; i < facts.Length; i++)
            {
                var entry = facts[i];
                if (!entry.FactId.Equals(request.FactId))
                {
                    continue;
                }

                entry.Confidence = ApplyMode(entry.Confidence, request.Confidence, request.ApplyMode);
                entry.LastUpdatedTick = currentTick;
                entry.DecayHalfLife = request.DecayHalfLife > 0 ? request.DecayHalfLife : entry.DecayHalfLife;
                entry.SourceKind = request.SourceKind;
                entry.SourceEntity = request.SourceEntity;
                facts[i] = entry;
                return;
            }

            facts.Add(new KnowledgeFact
            {
                FactId = request.FactId,
                Confidence = math.saturate(request.Confidence),
                LearnedTick = currentTick,
                LastUpdatedTick = currentTick,
                DecayHalfLife = request.DecayHalfLife,
                SourceKind = request.SourceKind,
                SourceEntity = request.SourceEntity
            });
        }

        private static float ApplyMode(float current, float incoming, KnowledgeApplyMode mode)
        {
            switch (mode)
            {
                case KnowledgeApplyMode.Max:
                    return math.max(current, incoming);
                case KnowledgeApplyMode.Override:
                    return incoming;
                default:
                    return math.saturate(current + incoming);
            }
        }

        private static void DecayFacts(ref DynamicBuffer<KnowledgeFact> facts, uint currentTick, float minConfidence)
        {
            for (int i = facts.Length - 1; i >= 0; i--)
            {
                var entry = facts[i];
                if (entry.DecayHalfLife > 0 && entry.LastUpdatedTick < currentTick)
                {
                    var ticksSince = currentTick - entry.LastUpdatedTick;
                    var decayFactor = math.pow(0.5f, (float)ticksSince / entry.DecayHalfLife);
                    entry.Confidence = math.saturate(entry.Confidence * decayFactor);
                    entry.LastUpdatedTick = currentTick;
                }

                if (entry.Confidence < minConfidence)
                {
                    facts.RemoveAt(i);
                    continue;
                }

                facts[i] = entry;
            }
        }

        private static void PruneFacts(ref DynamicBuffer<KnowledgeFact> facts, int maxFacts)
        {
            if (maxFacts <= 0 || facts.Length <= maxFacts)
            {
                return;
            }

            while (facts.Length > maxFacts)
            {
                int weakestIndex = 0;
                float weakestConfidence = facts[0].Confidence;
                for (int i = 1; i < facts.Length; i++)
                {
                    if (facts[i].Confidence < weakestConfidence)
                    {
                        weakestConfidence = facts[i].Confidence;
                        weakestIndex = i;
                    }
                }

                facts.RemoveAt(weakestIndex);
            }
        }
    }
}
