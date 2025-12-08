using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Core;
using PureDOTS.Shared;
using PureDOTS.Systems;
using System.Collections.Generic;

namespace PureDOTS.Runtime.Bridges
{
    /// <summary>
    /// Three-tier consensus arbitration system: Local → Regional → Global.
    /// Reduces message chatter from O(n²) → O(n) via hierarchical voting.
    /// Managed wrapper delegates to Burst jobs for GUID→Entity resolution.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(MindToBodySyncSystem))]
    public sealed partial class ConsensusArbitrationSystem : SystemBase
    {
        private float _lastArbitrationTime;
        private const float ArbitrationInterval = 0.5f; // 2 Hz arbitration

        protected override void OnCreate()
        {
            _lastArbitrationTime = 0f;
            RequireForUpdate<AgentSyncState>();
        }

        protected override void OnUpdate()
        {
            var currentTime = (float)SystemAPI.Time.ElapsedTime;
            if (currentTime - _lastArbitrationTime < ArbitrationInterval)
            {
                return;
            }

            var coordinator = World.GetExistingSystemManaged<AgentSyncBridgeCoordinator>();
            if (coordinator == null)
            {
                return;
            }

            var bus = coordinator.GetBus();
            if (bus == null)
            {
                return;
            }

            // Process consensus votes grouped by cluster and tier
            if (bus.ConsensusVoteQueueCount > 0)
            {
                var voteGroups = bus.DequeueConsensusVoteBatch();
                ProcessConsensusVotes(voteGroups, bus);
            }

            // Broadcast resolved outcomes
            if (bus.ConsensusOutcomeQueueCount > 0)
            {
                var outcomes = bus.DequeueConsensusOutcomeBatch();
                BroadcastConsensusOutcomes(outcomes);
            }

            _lastArbitrationTime = currentTime;
        }

        private void ProcessConsensusVotes(
            Dictionary<AgentGuid, Dictionary<ConsensusTier, List<ConsensusVoteMessage>>> voteGroups,
            AgentSyncBus bus)
        {
            var tickState = SystemAPI.GetSingleton<TickTimeState>();
            
            // Process each cluster's votes by tier
            foreach (var clusterPair in voteGroups)
            {
                var clusterGuid = clusterPair.Key;
                var tierVotes = clusterPair.Value;

                // Process Local tier first
                if (tierVotes.ContainsKey(ConsensusTier.Local))
                {
                    var localVotes = tierVotes[ConsensusTier.Local];
                    var resolvedValue = ResolveConsensus(localVotes);
                    
                    if (resolvedValue.HasValue)
                    {
                        bus.EnqueueConsensusOutcome(new ConsensusOutcomeMessage
                        {
                            ClusterGuid = clusterGuid,
                            Tier = ConsensusTier.Local,
                            ResolvedValue = resolvedValue.Value,
                            VoteCount = localVotes.Count,
                            ResolutionTick = tickState.Tick
                        });
                    }
                }

                // Process Regional tier (requires local consensus first)
                if (tierVotes.ContainsKey(ConsensusTier.Regional))
                {
                    var regionalVotes = tierVotes[ConsensusTier.Regional];
                    var resolvedValue = ResolveConsensus(regionalVotes);
                    
                    if (resolvedValue.HasValue)
                    {
                        bus.EnqueueConsensusOutcome(new ConsensusOutcomeMessage
                        {
                            ClusterGuid = clusterGuid,
                            Tier = ConsensusTier.Regional,
                            ResolvedValue = resolvedValue.Value,
                            VoteCount = regionalVotes.Count,
                            ResolutionTick = tickState.Tick
                        });
                    }
                }

                // Process Global tier (requires regional consensus first)
                if (tierVotes.ContainsKey(ConsensusTier.Global))
                {
                    var globalVotes = tierVotes[ConsensusTier.Global];
                    var resolvedValue = ResolveConsensus(globalVotes);
                    
                    if (resolvedValue.HasValue)
                    {
                        bus.EnqueueConsensusOutcome(new ConsensusOutcomeMessage
                        {
                            ClusterGuid = clusterGuid,
                            Tier = ConsensusTier.Global,
                            ResolvedValue = resolvedValue.Value,
                            VoteCount = globalVotes.Count,
                            ResolutionTick = tickState.Tick
                        });
                    }
                }
            }
        }

        private byte? ResolveConsensus(List<ConsensusVoteMessage> votes)
        {
            if (votes == null || votes.Count == 0)
                return null;

            // Weighted average of votes (simple majority)
            long totalWeight = 0;
            long weightedSum = 0;

            foreach (var vote in votes)
            {
                // Use vote value as weight (0-255)
                var weight = (long)vote.VoteValue;
                totalWeight += weight;
                weightedSum += weight * vote.VoteValue;
            }

            if (totalWeight == 0)
                return null;

            var resolved = (byte)math.clamp(weightedSum / totalWeight, 0, 255);
            return resolved;
        }

        private void BroadcastConsensusOutcomes(List<ConsensusOutcomeMessage> outcomes)
        {
            // Write outcomes to entities with matching cluster GUIDs
            var syncIdLookup = GetComponentLookup<AgentSyncId>(false);
            var outcomeBufferLookup = GetBufferLookup<ConsensusOutcome>(false);

            var query = GetEntityQuery(typeof(AgentSyncId), typeof(LocalConsensusState));
            
            foreach (var outcome in outcomes)
            {
                var job = new BroadcastOutcomeJob
                {
                    Outcome = outcome,
                    SyncIdLookup = syncIdLookup,
                    OutcomeBufferLookup = outcomeBufferLookup
                };

                job.ScheduleParallel(query, Dependency).Complete();
            }
        }

        [BurstCompile]
        private partial struct BroadcastOutcomeJob : IJobEntity
        {
            public ConsensusOutcomeMessage Outcome;
            [ReadOnly] public ComponentLookup<AgentSyncId> SyncIdLookup;
            public BufferLookup<ConsensusOutcome> OutcomeBufferLookup;

            public void Execute(Entity entity, in AgentSyncId syncId)
            {
                // Check if this entity belongs to the cluster
                if (!syncId.ClusterGuid.Equals(Outcome.ClusterGuid))
                {
                    return;
                }

                // Add outcome to buffer
                if (OutcomeBufferLookup.HasBuffer(entity))
                {
                    var buffer = OutcomeBufferLookup[entity];
                    buffer.Add(new ConsensusOutcome
                    {
                        ClusterGuid = Outcome.ClusterGuid,
                        Tier = Outcome.Tier,
                        ResolvedValue = Outcome.ResolvedValue,
                        VoteCount = Outcome.VoteCount,
                        ResolutionTick = Outcome.ResolutionTick
                    });
                }
            }
        }
    }
}

