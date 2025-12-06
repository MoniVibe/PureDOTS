using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Shared;
using PureDOTS.Runtime.AI.Social;
using PureDOTS.Runtime.Bridges;
using PureDOTS.Runtime.Components;
using PureDOTS.Systems;

namespace PureDOTS.Runtime.AI.Social.Systems
{
    /// <summary>
    /// Cooperation system that dequeues SocialMessage and resolves interactions deterministically.
    /// Implements Discovery, Negotiation, Execution, and Reflection phases.
    /// Based on Hoey et al. (2018) cooperation protocols.
    /// </summary>
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    [UpdateAfter(typeof(VillagerSystemGroup))]
    [BurstCompile]
    public partial struct CooperationSystem : ISystem
    {
        private const float CooperationThreshold = 0.3f; // Minimum utility gain for cooperation
        private const float TrustUpdateLearningRate = 0.1f; // Default learning rate for trust updates
        private const float MaxInteractionDistance = 50f; // Maximum distance for interactions

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<AgentSyncState>();
            state.RequireForUpdate<RewindState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var rewindState = SystemAPI.GetSingleton<RewindState>();
            if (rewindState.Mode != RewindMode.Record)
            {
                return; // Skip during playback
            }

            // This system needs managed access to AgentSyncBus, so we use a managed wrapper
            // The actual processing happens in a Burst job
        }
    }

    /// <summary>
    /// Managed wrapper for CooperationSystem that accesses AgentSyncBus.
    /// Delegates to Burst job for deterministic processing.
    /// </summary>
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    [UpdateAfter(typeof(VillagerSystemGroup))]
    public sealed partial class CooperationSystemManaged : SystemBase
    {
        private float _lastUpdateTime;
        private const float UpdateInterval = 0.2f; // 5 Hz updates (temporal batching)

        protected override void OnCreate()
        {
            _lastUpdateTime = 0f;
            RequireForUpdate<AgentSyncState>();
            RequireForUpdate<RewindState>();
        }

        protected override void OnUpdate()
        {
            var rewindState = SystemAPI.GetSingleton<RewindState>();
            if (rewindState.Mode != RewindMode.Record)
            {
                return; // Skip during playback
            }

            var currentTime = (float)SystemAPI.Time.ElapsedTime;
            if (currentTime - _lastUpdateTime < UpdateInterval)
            {
                return; // Temporal batching
            }

            var coordinator = World.GetExistingSystemManaged<AgentSyncBridgeCoordinator>();
            if (coordinator == null)
            {
                return;
            }

            var bus = coordinator.GetBus();
            if (bus == null || bus.SocialMessageQueueCount == 0)
            {
                return;
            }

            var tickState = SystemAPI.GetSingleton<TickTimeState>();
            var tickNumber = tickState.Tick;

            // Dequeue social messages
            using var messageBatch = bus.DequeueSocialMessageBatch(Allocator.TempJob);

            if (messageBatch.Length == 0)
            {
                return;
            }

            // Get component lookups
            var syncIdLookup = GetComponentLookup<AgentSyncId>(true);
            var positionLookup = GetComponentLookup<Unity.Transforms.LocalTransform>(true);
            var socialKnowledgeLookup = GetComponentLookup<SocialKnowledge>(false);
            var relationshipBufferLookup = GetBufferLookup<SocialRelationship>(false);
            var socialMessageBufferLookup = GetBufferLookup<SocialMessage>(false);

            // Process messages in Burst job
            var job = new ProcessCooperationMessagesJob
            {
                Messages = messageBatch,
                SyncIdLookup = syncIdLookup,
                PositionLookup = positionLookup,
                SocialKnowledgeLookup = socialKnowledgeLookup,
                RelationshipBufferLookup = relationshipBufferLookup,
                SocialMessageBufferLookup = socialMessageBufferLookup,
                TickNumber = tickNumber,
                CooperationThreshold = CooperationSystem.CooperationThreshold,
                LearningRate = CooperationSystem.TrustUpdateLearningRate,
                MaxDistance = CooperationSystem.MaxInteractionDistance
            };

            var entityQuery = GetEntityQuery(typeof(AgentSyncId), typeof(SocialKnowledge));
            job.ScheduleParallel(entityQuery, Dependency).Complete();

            _lastUpdateTime = currentTime;
        }
    }

    [BurstCompile]
    private partial struct ProcessCooperationMessagesJob : IJobEntity
    {
        [ReadOnly] public NativeList<SocialMessage> Messages;
        [ReadOnly] public ComponentLookup<AgentSyncId> SyncIdLookup;
        [ReadOnly] public ComponentLookup<Unity.Transforms.LocalTransform> PositionLookup;
        public ComponentLookup<SocialKnowledge> SocialKnowledgeLookup;
        public BufferLookup<SocialRelationship> RelationshipBufferLookup;
        public BufferLookup<SocialMessage> SocialMessageBufferLookup;
        public uint TickNumber;
        public float CooperationThreshold;
        public float LearningRate;
        public float MaxDistance;

        public void Execute(
            Entity entity,
            ref SocialKnowledge socialKnowledge,
            DynamicBuffer<SocialRelationship> relationships)
        {
            if (!SyncIdLookup.HasComponent(entity))
            {
                return;
            }

            var agentGuid = SyncIdLookup[entity].Guid;
            var agentPosition = PositionLookup.HasComponent(entity)
                ? PositionLookup[entity].Position
                : float3.zero;

            // Process messages for this agent
            for (int i = 0; i < Messages.Length; i++)
            {
                var message = Messages[i];

                // Check if message is for this agent
                if (!message.ReceiverGuid.Equals(agentGuid))
                {
                    continue;
                }

                // Discovery: Check proximity (simplified - full spatial check would use spatial index)
                // For now, we process all messages; spatial filtering can be added via spatial index

                // Negotiation: Process message based on type
                ProcessMessage(
                    entity,
                    message,
                    ref socialKnowledge,
                    relationships);
            }
        }


        private void ProcessMessage(
            Entity entity,
            SocialMessage message,
            ref SocialKnowledge socialKnowledge,
            DynamicBuffer<SocialRelationship> relationships)
        {
            // Find or create relationship
            var relationshipIndex = FindRelationshipIndex(relationships, message.SenderGuid);
            SocialRelationship relationship;

            if (relationshipIndex >= 0)
            {
                relationship = relationships[relationshipIndex];
            }
            else
            {
                // Create new relationship
                relationship = new SocialRelationship
                {
                    OtherAgentGuid = message.SenderGuid,
                    Trust = socialKnowledge.BaseTrust,
                    Reputation = socialKnowledge.BaseReputation,
                    CooperationBias = socialKnowledge.CooperationBias,
                    LastInteractionTick = TickNumber,
                    InteractionCount = 0f
                };
                relationships.Add(relationship);
                relationshipIndex = relationships.Length - 1;
            }

            // Execution: Resolve interaction based on message type
            float interactionOutcome = 0f;
            bool shouldCooperate = false;

            switch (message.Type)
            {
                case SocialMessageType.Offer:
                case SocialMessageType.Request:
                    // Calculate utility
                    var mutualGain = message.Payload;
                    shouldCooperate = CooperationResolutionSystem.CalculateMutualUtility(
                        message.Payload,
                        message.Payload * 0.8f, // Receiver's utility estimate
                        CooperationThreshold,
                        out var gain);
                    interactionOutcome = shouldCooperate ? 1f : 0f;
                    break;

                case SocialMessageType.Praise:
                    // Positive interaction
                    interactionOutcome = 0.8f;
                    break;

                case SocialMessageType.Threat:
                    // Negative interaction
                    interactionOutcome = -0.3f;
                    break;

                default:
                    interactionOutcome = 0f;
                    break;
            }

            // Reflection: Update trust and reputation
            var expectedOutcome = CooperationResolutionSystem.CalculateExpectedOutcome(
                relationship.Trust,
                0.5f, // Base success rate
                math.min(relationship.InteractionCount / 10f, 1f)); // Interaction history

            relationship.Trust = CooperationResolutionSystem.UpdateTrust(
                relationship.Trust,
                interactionOutcome,
                expectedOutcome,
                LearningRate);

            relationship.Reputation = math.lerp(relationship.Reputation, relationship.Trust, 0.1f);
            relationship.LastInteractionTick = TickNumber;
            relationship.InteractionCount += 1f;

            relationships[relationshipIndex] = relationship;

            // Update social knowledge last update tick
            socialKnowledge.LastUpdateTick = TickNumber;
        }

        private int FindRelationshipIndex(DynamicBuffer<SocialRelationship> relationships, AgentGuid guid)
        {
            for (int i = 0; i < relationships.Length; i++)
            {
                if (relationships[i].OtherAgentGuid.Equals(guid))
                {
                    return i;
                }
            }
            return -1;
        }
    }
}

