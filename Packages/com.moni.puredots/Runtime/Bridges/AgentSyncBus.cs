using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Shared;
using PureDOTS.Runtime.AI;
using PureDOTS.Runtime.AI.Social;
using PureDOTS.Runtime.Components;

namespace PureDOTS.Runtime.Bridges
{
    /// <summary>
    /// Message sent from Mind ECS to Body ECS containing intent commands.
    /// </summary>
    public struct MindToBodyMessage
    {
        public AgentGuid AgentGuid;
        public IntentKind Kind;
        public float3 TargetPosition;
        public Entity TargetEntity;
        public byte Priority;
        public uint TickNumber; // Timestamp from simulation tick
    }

    /// <summary>
    /// Message sent from Body ECS to Mind ECS containing state updates.
    /// </summary>
    public struct BodyToMindMessage
    {
        public AgentGuid AgentGuid;
        public float3 Position;
        public quaternion Rotation;
        public float Health;
        public float MaxHealth;
        public byte Flags; // Bit flags for changed fields
        public uint TickNumber; // Timestamp from simulation tick
        public AgentGuid AggregateGuid; // Which aggregate this agent belongs to (empty if none)
    }

    /// <summary>
    /// Message sent from Body ECS to Mind ECS containing context perception data.
    /// Used for procedural learning synchronization.
    /// </summary>
    public struct ContextPerceptionMessage
    {
        public AgentGuid AgentGuid;
        public byte ContextHash; // Situation fingerprint
        public byte TerrainType;
        public byte ObstacleTag;
        public byte GoalType;
        public uint TickNumber;
    }

    /// <summary>
    /// Message sent from Body ECS to Mind ECS containing action outcome data.
    /// Used for procedural memory reinforcement.
    /// </summary>
    public struct ActionOutcomeMessage
    {
        public AgentGuid AgentGuid;
        public byte ActionId;
        public byte ContextHash;
        public float SuccessResult; // 0.0 = failure, 1.0 = success
        public uint TickNumber;
    }

    /// <summary>
    /// Message sent from Mind ECS to Body ECS containing procedural memory updates.
    /// Used to sync learned action preferences from cognitive layer.
    /// </summary>
    public struct ProceduralMemoryUpdateMessage
    {
        public AgentGuid AgentGuid;
        public byte ContextHash;
        public byte ActionId;
        public float SuccessScore;
        public uint TickNumber;
    }

    /// <summary>
    /// Limb command sent from Mind ECS to Body ECS for limb activation.
    /// </summary>
    public struct LimbCommand
    {
        public AgentGuid AgentGuid;
        public int LimbIndex;
        public LimbAction Action;
        public float3 Target;
        public byte Priority;
        public uint TickNumber;
    }

    /// <summary>
    /// Aggregate intent message sent from Aggregate ECS to Mind ECS.
    /// Contains group-level goals and distribution ratios for biasing individual agent goals.
    /// Extended with cooperation/competition weights for social dynamics.
    /// </summary>
    public struct AggregateIntentMessage
    {
        public AgentGuid AggregateGuid;
        public string GoalType; // "Harvest", "Defend", "Patrol", "Rest", etc.
        public float Priority; // 0-1
        public float3 TargetPosition;
        public Dictionary<string, float> DistributionRatios; // e.g., "Farm"=0.6, "Defend"=0.3, "Rest"=0.1
        public float CooperationWeight; // Weight for cooperative actions (0-1)
        public float CompetitionWeight; // Weight for competitive actions (0-1)
        public float ResourcePriority; // Priority for resource acquisition (0-1)
        public float ThreatLevel; // Perceived threat level (0-1)
    }

    /// <summary>
    /// Consensus vote message for hierarchical arbitration.
    /// Used for local/regional/global consensus voting.
    /// </summary>
    public struct ConsensusVoteMessage
    {
        public AgentGuid VoterGuid;
        public AgentGuid ClusterGuid;
        public byte VoteValue; // 0-255
        public ConsensusTier Tier;
        public uint TickNumber;
    }

    /// <summary>
    /// Consensus outcome message resolved at a tier.
    /// Broadcasts resolved consensus to affected agents.
    /// </summary>
    public struct ConsensusOutcomeMessage
    {
        public AgentGuid ClusterGuid;
        public ConsensusTier Tier;
        public byte ResolvedValue; // 0-255
        public int VoteCount;
        public uint ResolutionTick;
    }

    /// <summary>
    /// Routing metadata for self-organizing message routing.
    /// Extends messages with routing information.
    /// </summary>
    public struct RoutingMetadata
    {
        public AgentGuid SourceGuid;       // Original sender
        public AgentGuid CurrentNodeGuid;   // Current node handling message
        public AgentGuid NextRecipientGuid; // Next node to route to (if self-organizing)
        public float ImportanceScore;       // Importance score for routing
        public float ContinuityScore;      // Continuity score for routing
        public int HopCount;                // Number of hops so far
        public uint RoutingTick;            // When routing decision was made
    }

    /// <summary>
    /// Intent kinds that can be issued by the cognitive layer.
    /// </summary>
    public enum IntentKind : byte
    {
        None = 0,
        Move = 1,
        Attack = 2,
        Harvest = 3,
        Defend = 4,
        Patrol = 5,
        UseAbility = 6,
        Interact = 7,
        Rest = 8,
        Flee = 9
    }

    /// <summary>
    /// Flags for BodyToMindMessage indicating which fields changed.
    /// </summary>
    public static class BodyToMindFlags
    {
        public const byte PositionChanged = 1 << 0;
        public const byte RotationChanged = 1 << 1;
        public const byte HealthChanged = 1 << 2;
        public const byte MaxHealthChanged = 1 << 3;
    }

    /// <summary>
    /// Message broker for batched cross-ECS communication.
    /// Implements delta compression: only changed fields since last sync.
    /// </summary>
    public class AgentSyncBus
    {
        private readonly Queue<MindToBodyMessage> _mindToBodyQueue;
        private readonly Queue<BodyToMindMessage> _bodyToMindQueue;
        private readonly Queue<Percept> _perceptQueue;
        private readonly Queue<LimbCommand> _limbCommandQueue;
        private readonly Queue<AggregateIntentMessage> _aggregateIntentQueue;
        private readonly Queue<ConsensusVoteMessage> _consensusVoteQueue;
        private readonly Queue<ConsensusOutcomeMessage> _consensusOutcomeQueue;
        private readonly Queue<SocialMessage> _socialMessageQueue;
        private readonly Queue<CulturalSignal> _culturalSignalQueue;
        private readonly Dictionary<AgentGuid, MindToBodyMessage> _lastMindToBodyState;
        private readonly Dictionary<AgentGuid, BodyToMindMessage> _lastBodyToMindState;
        private readonly Dictionary<AgentGuid, Dictionary<ConsensusTier, byte>> _clusterConsensusCache; // Cluster -> Tier -> Last resolved value
        private readonly Dictionary<AgentGuid, Dictionary<AgentGuid, SocialMessage>> _lastSocialMessageState; // Sender -> Receiver -> Last message

        public AgentSyncBus()
        {
            _mindToBodyQueue = new Queue<MindToBodyMessage>();
            _bodyToMindQueue = new Queue<BodyToMindMessage>();
            _perceptQueue = new Queue<Percept>();
            _limbCommandQueue = new Queue<LimbCommand>();
            _aggregateIntentQueue = new Queue<AggregateIntentMessage>();
            _consensusVoteQueue = new Queue<ConsensusVoteMessage>();
            _consensusOutcomeQueue = new Queue<ConsensusOutcomeMessage>();
            _socialMessageQueue = new Queue<SocialMessage>();
            _culturalSignalQueue = new Queue<CulturalSignal>();
            _lastMindToBodyState = new Dictionary<AgentGuid, MindToBodyMessage>();
            _lastBodyToMindState = new Dictionary<AgentGuid, BodyToMindMessage>();
            _clusterConsensusCache = new Dictionary<AgentGuid, Dictionary<ConsensusTier, byte>>();
            _lastSocialMessageState = new Dictionary<AgentGuid, Dictionary<AgentGuid, SocialMessage>>();
        }

        /// <summary>
        /// Enqueue a message from Mind ECS to Body ECS with delta compression.
        /// </summary>
        public void EnqueueMindToBody(MindToBodyMessage message)
        {
            // Delta compression: only enqueue if changed
            if (_lastMindToBodyState.TryGetValue(message.AgentGuid, out var last))
            {
                if (last.Kind == message.Kind &&
                    math.distance(last.TargetPosition, message.TargetPosition) < 0.01f &&
                    last.TargetEntity == message.TargetEntity &&
                    last.Priority == message.Priority)
                {
                    return; // No change, skip
                }
            }

            _lastMindToBodyState[message.AgentGuid] = message;
            _mindToBodyQueue.Enqueue(message);
        }

        /// <summary>
        /// Enqueue a message from Body ECS to Mind ECS with delta compression.
        /// </summary>
        public void EnqueueBodyToMind(BodyToMindMessage message)
        {
            // Delta compression: only enqueue if changed
            if (_lastBodyToMindState.TryGetValue(message.AgentGuid, out var last))
            {
                byte changedFlags = 0;
                if (math.distance(last.Position, message.Position) > 0.01f)
                    changedFlags |= BodyToMindFlags.PositionChanged;
                if (math.distance(last.Rotation.value, message.Rotation.value) > 0.01f)
                    changedFlags |= BodyToMindFlags.RotationChanged;
                if (math.abs(last.Health - message.Health) > 0.01f)
                    changedFlags |= BodyToMindFlags.HealthChanged;
                if (math.abs(last.MaxHealth - message.MaxHealth) > 0.01f)
                    changedFlags |= BodyToMindFlags.MaxHealthChanged;

                if (changedFlags == 0)
                    return; // No change, skip

                message.Flags = changedFlags;
            }
            else
            {
                // First message, mark all as changed
                message.Flags = BodyToMindFlags.PositionChanged | BodyToMindFlags.RotationChanged |
                                BodyToMindFlags.HealthChanged | BodyToMindFlags.MaxHealthChanged;
            }

            _lastBodyToMindState[message.AgentGuid] = message;
            _bodyToMindQueue.Enqueue(message);
        }

        /// <summary>
        /// Dequeue all pending MindToBody messages in batch.
        /// </summary>
        public NativeList<MindToBodyMessage> DequeueMindToBodyBatch(Allocator allocator)
        {
            var batch = new NativeList<MindToBodyMessage>(_mindToBodyQueue.Count, allocator);
            while (_mindToBodyQueue.Count > 0)
            {
                batch.Add(_mindToBodyQueue.Dequeue());
            }
            return batch;
        }

        /// <summary>
        /// Dequeue all pending BodyToMind messages in batch.
        /// </summary>
        public NativeList<BodyToMindMessage> DequeueBodyToMindBatch(Allocator allocator)
        {
            var batch = new NativeList<BodyToMindMessage>(_bodyToMindQueue.Count, allocator);
            while (_bodyToMindQueue.Count > 0)
            {
                batch.Add(_bodyToMindQueue.Dequeue());
            }
            return batch;
        }

        /// <summary>
        /// Enqueue a percept from Body ECS to Mind ECS.
        /// </summary>
        public void EnqueuePercept(Percept percept)
        {
            _perceptQueue.Enqueue(percept);
        }

        /// <summary>
        /// Dequeue all pending percepts in batch.
        /// </summary>
        public NativeList<Percept> DequeuePerceptBatch(Allocator allocator)
        {
            var batch = new NativeList<Percept>(_perceptQueue.Count, allocator);
            while (_perceptQueue.Count > 0)
            {
                batch.Add(_perceptQueue.Dequeue());
            }
            return batch;
        }

        /// <summary>
        /// Enqueue a limb command from Mind ECS to Body ECS.
        /// </summary>
        public void EnqueueLimbCommand(LimbCommand command)
        {
            _limbCommandQueue.Enqueue(command);
        }

        /// <summary>
        /// Dequeue all pending limb commands in batch.
        /// </summary>
        public NativeList<LimbCommand> DequeueLimbCommandBatch(Allocator allocator)
        {
            var batch = new NativeList<LimbCommand>(_limbCommandQueue.Count, allocator);
            while (_limbCommandQueue.Count > 0)
            {
                batch.Add(_limbCommandQueue.Dequeue());
            }
            return batch;
        }

        /// <summary>
        /// Enqueue an aggregate intent message from Aggregate ECS to Mind ECS.
        /// </summary>
        public void EnqueueAggregateIntent(AggregateIntentMessage message)
        {
            _aggregateIntentQueue.Enqueue(message);
        }

        /// <summary>
        /// Dequeue all pending aggregate intent messages in batch.
        /// </summary>
        public List<AggregateIntentMessage> DequeueAggregateIntentBatch()
        {
            var batch = new List<AggregateIntentMessage>(_aggregateIntentQueue.Count);
            while (_aggregateIntentQueue.Count > 0)
            {
                batch.Add(_aggregateIntentQueue.Dequeue());
            }
            return batch;
        }

        /// <summary>
        /// Enqueue a consensus vote message with cluster grouping for batched processing.
        /// </summary>
        public void EnqueueConsensusVote(ConsensusVoteMessage vote)
        {
            _consensusVoteQueue.Enqueue(vote);
        }

        /// <summary>
        /// Dequeue all pending consensus votes grouped by cluster and tier.
        /// Returns dictionary: ClusterGuid -> Tier -> List of votes
        /// </summary>
        public Dictionary<AgentGuid, Dictionary<ConsensusTier, List<ConsensusVoteMessage>>> DequeueConsensusVoteBatch()
        {
            var grouped = new Dictionary<AgentGuid, Dictionary<ConsensusTier, List<ConsensusVoteMessage>>>();
            
            while (_consensusVoteQueue.Count > 0)
            {
                var vote = _consensusVoteQueue.Dequeue();
                
                if (!grouped.ContainsKey(vote.ClusterGuid))
                {
                    grouped[vote.ClusterGuid] = new Dictionary<ConsensusTier, List<ConsensusVoteMessage>>();
                }
                
                if (!grouped[vote.ClusterGuid].ContainsKey(vote.Tier))
                {
                    grouped[vote.ClusterGuid][vote.Tier] = new List<ConsensusVoteMessage>();
                }
                
                grouped[vote.ClusterGuid][vote.Tier].Add(vote);
            }
            
            return grouped;
        }

        /// <summary>
        /// Enqueue a consensus outcome message for broadcasting.
        /// </summary>
        public void EnqueueConsensusOutcome(ConsensusOutcomeMessage outcome)
        {
            _consensusOutcomeQueue.Enqueue(outcome);
            
            // Cache the outcome for delta compression
            if (!_clusterConsensusCache.ContainsKey(outcome.ClusterGuid))
            {
                _clusterConsensusCache[outcome.ClusterGuid] = new Dictionary<ConsensusTier, byte>();
            }
            _clusterConsensusCache[outcome.ClusterGuid][outcome.Tier] = outcome.ResolvedValue;
        }

        /// <summary>
        /// Dequeue all pending consensus outcomes in batch.
        /// </summary>
        public List<ConsensusOutcomeMessage> DequeueConsensusOutcomeBatch()
        {
            var batch = new List<ConsensusOutcomeMessage>(_consensusOutcomeQueue.Count);
            while (_consensusOutcomeQueue.Count > 0)
            {
                batch.Add(_consensusOutcomeQueue.Dequeue());
            }
            return batch;
        }

        /// <summary>
        /// Enqueue a social message with delta compression.
        /// Only enqueues if message differs from last message between sender and receiver.
        /// </summary>
        public void EnqueueSocialMessage(SocialMessage message)
        {
            // Delta compression: only enqueue if changed relationship
            if (_lastSocialMessageState.TryGetValue(message.SenderGuid, out var receiverDict))
            {
                if (receiverDict.TryGetValue(message.ReceiverGuid, out var last))
                {
                    if (last.Type == message.Type &&
                        math.abs(last.Urgency - message.Urgency) < 0.01f &&
                        math.abs(last.Payload - message.Payload) < 0.01f &&
                        last.Flags == message.Flags)
                    {
                        return; // No change, skip
                    }
                }
            }
            else
            {
                _lastSocialMessageState[message.SenderGuid] = new Dictionary<AgentGuid, SocialMessage>();
            }

            _lastSocialMessageState[message.SenderGuid][message.ReceiverGuid] = message;
            _socialMessageQueue.Enqueue(message);
        }

        /// <summary>
        /// Dequeue all pending social messages in batch.
        /// </summary>
        public NativeList<SocialMessage> DequeueSocialMessageBatch(Allocator allocator)
        {
            var batch = new NativeList<SocialMessage>(_socialMessageQueue.Count, allocator);
            while (_socialMessageQueue.Count > 0)
            {
                batch.Add(_socialMessageQueue.Dequeue());
            }
            return batch;
        }

        /// <summary>
        /// Enqueue a cultural signal for propagation.
        /// </summary>
        public void EnqueueCulturalSignal(CulturalSignal signal)
        {
            _culturalSignalQueue.Enqueue(signal);
        }

        /// <summary>
        /// Dequeue all pending cultural signals in batch.
        /// </summary>
        public NativeList<CulturalSignal> DequeueCulturalSignalBatch(Allocator allocator)
        {
            var batch = new NativeList<CulturalSignal>(_culturalSignalQueue.Count, allocator);
            while (_culturalSignalQueue.Count > 0)
            {
                batch.Add(_culturalSignalQueue.Dequeue());
            }
            return batch;
        }

        /// <summary>
        /// Clear all pending messages and state.
        /// </summary>
        public void Clear()
        {
            _mindToBodyQueue.Clear();
            _bodyToMindQueue.Clear();
            _perceptQueue.Clear();
            _limbCommandQueue.Clear();
            _aggregateIntentQueue.Clear();
            _consensusVoteQueue.Clear();
            _consensusOutcomeQueue.Clear();
            _socialMessageQueue.Clear();
            _culturalSignalQueue.Clear();
            _lastMindToBodyState.Clear();
            _lastBodyToMindState.Clear();
            _clusterConsensusCache.Clear();
            _lastSocialMessageState.Clear();
        }

        public int MindToBodyQueueCount => _mindToBodyQueue.Count;
        public int BodyToMindQueueCount => _bodyToMindQueue.Count;
        public int PerceptQueueCount => _perceptQueue.Count;
        public int LimbCommandQueueCount => _limbCommandQueue.Count;
        public int AggregateIntentQueueCount => _aggregateIntentQueue.Count;
        public int ConsensusVoteQueueCount => _consensusVoteQueue.Count;
        public int ConsensusOutcomeQueueCount => _consensusOutcomeQueue.Count;
        public int SocialMessageQueueCount => _socialMessageQueue.Count;
        public int CulturalSignalQueueCount => _culturalSignalQueue.Count;
    }
}

