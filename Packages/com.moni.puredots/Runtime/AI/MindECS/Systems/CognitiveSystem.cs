using System.Collections.Generic;
using DefaultEcs;
using DefaultEcs.System;
using PureDOTS.AI.AggregateECS;
using PureDOTS.AI.AggregateECS.Components;
using PureDOTS.AI.MindECS.Components;
using PureDOTS.Runtime.Bridges;
using PureDOTS.Runtime.AI;
using PureDOTS.Shared;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace PureDOTS.AI.MindECS.Systems
{
    /// <summary>
    /// DefaultEcs system that evaluates goals, updates personality state, and generates intents.
    /// Runs at 1-5 Hz per entity (throttled).
    /// Consumes percepts from Body ECS and generates limb commands.
    /// </summary>
    public class CognitiveSystem : AEntitySetSystem<float>
    {
        private float _lastUpdateTime;
        private const float MinUpdateInterval = 0.2f; // 5 Hz max
        private const float MaxUpdateInterval = 1.0f; // 1 Hz min
        private AgentSyncBus _syncBus;
        private Dictionary<AgentGuid, List<Percept>> _perceptMemory;
        private Dictionary<AgentGuid, AggregateIntentMessage> _aggregateIntentCache; // Cache aggregate intents by aggregate GUID
        private Dictionary<AgentGuid, AgentGuid> _agentToAggregateMap; // Map agent GUID to aggregate GUID

        public CognitiveSystem(World world, AgentSyncBus syncBus) 
            : base(world.GetEntities().With<PersonalityProfile>().With<GoalProfile>().With<BehaviorProfile>().AsSet())
        {
            _lastUpdateTime = 0f;
            _syncBus = syncBus;
            _perceptMemory = new Dictionary<AgentGuid, List<Percept>>();
            _aggregateIntentCache = new Dictionary<AgentGuid, AggregateIntentMessage>();
            _agentToAggregateMap = new Dictionary<AgentGuid, AgentGuid>();
        }

        protected override void Update(float deltaTime, in Entity entity)
        {
            var currentTime = Time.time;
            
            // Throttle updates per entity (simplified - in production, track per-entity timers)
            if (currentTime - _lastUpdateTime < MinUpdateInterval)
            {
                return;
            }

            if (!World.Has<PersonalityProfile>(entity) ||
                !World.Has<GoalProfile>(entity) ||
                !World.Has<BehaviorProfile>(entity))
            {
                return;
            }

            // Get AgentGuid for percept lookup
            if (!World.Has<AgentGuid>(entity))
            {
                return;
            }

            var agentGuid = World.Get<AgentGuid>(entity);

            // Consume percepts from sync bus
            ConsumePercepts(agentGuid, entity);

            // Consume aggregate intents from sync bus
            ConsumeAggregateIntents();

            // Update agent aggregate mapping from BodyToMind messages
            // Note: This would ideally come from BodyToMindMessage, but for now we'll update it
            // when we process BodyToMind messages (would need to extend that system)
            // For now, we'll rely on aggregate intents containing member lists

            var personality = World.Get<PersonalityProfile>(entity);
            var goals = World.Get<GoalProfile>(entity);
            var behavior = World.Get<BehaviorProfile>(entity);

            // Get or create cognitive memory
            CognitiveMemory memory = null;
            if (World.Has<CognitiveMemory>(entity))
            {
                memory = World.Get<CognitiveMemory>(entity);
            }
            else
            {
                memory = new CognitiveMemory();
                World.Set(entity, memory);
            }

            // Evaluate goals and update priorities based on percepts and aggregate intents
            EvaluateGoals(goals, personality, behavior, memory, agentGuid);

            // Generate intent based on current primary goal
            if (!string.IsNullOrEmpty(goals.CurrentPrimaryGoalId))
            {
                GenerateIntent(entity, goals, personality, behavior, memory);
            }

            // Generate limb commands based on percepts
            GenerateLimbCommands(entity, agentGuid, memory, personality);

            _lastUpdateTime = currentTime;
        }

        private void ConsumePercepts(AgentGuid agentGuid, Entity entity)
        {
            if (_syncBus == null || _syncBus.PerceptQueueCount == 0)
            {
                return;
            }

            // Dequeue percepts (this is a managed operation, so we do it here)
            using var perceptBatch = _syncBus.DequeuePerceptBatch(Allocator.Temp);

            if (!_perceptMemory.ContainsKey(agentGuid))
            {
                _perceptMemory[agentGuid] = new List<Percept>();
            }

            var percepts = _perceptMemory[agentGuid];

            // Add percepts to memory
            for (int i = 0; i < perceptBatch.Length; i++)
            {
                var percept = perceptBatch[i];
                if (percept.AgentGuid.Equals(agentGuid))
                {
                    percepts.Add(percept);

                    // Update cognitive memory if available
                    if (World.Has<CognitiveMemory>(entity))
                    {
                        var memory = World.Get<CognitiveMemory>(entity);
                        memory.AddPercept(percept);
                    }
                }
            }

            // Prune old percepts (keep last 50)
            while (percepts.Count > 50)
            {
                percepts.RemoveAt(0);
            }
        }

        private void UpdateAgentAggregateMapping(AgentGuid agentGuid, AgentGuid aggregateGuid)
        {
            // Update agent to aggregate mapping
            if (aggregateGuid.High != 0 || aggregateGuid.Low != 0)
            {
                _agentToAggregateMap[agentGuid] = aggregateGuid;
            }
            else
            {
                _agentToAggregateMap.Remove(agentGuid);
            }
        }

        private void ConsumeAggregateIntents()
        {
            if (_syncBus == null || _syncBus.AggregateIntentQueueCount == 0)
            {
                return;
            }

            // Dequeue aggregate intents (managed operation)
            var aggregateIntents = _syncBus.DequeueAggregateIntentBatch();

            // Get aggregate world to build agent-to-aggregate mapping
            var aggregateWorld = AggregateECSWorld.Instance;
            
            // Cache aggregate intents by aggregate GUID and build agent-to-aggregate mapping
            foreach (var intent in aggregateIntents)
            {
                _aggregateIntentCache[intent.AggregateGuid] = intent;

                // Build agent-to-aggregate mapping from aggregate world
                if (aggregateWorld != null)
                {
                    foreach (var entity in aggregateWorld.World.GetEntities().With<AggregateEntity>().AsSet().GetEntities())
                    {
                        var aggregate = aggregateWorld.World.Get<AggregateEntity>(entity);
                        if (aggregate.AggregateGuid.Equals(intent.AggregateGuid))
                        {
                            // Map all members to this aggregate
                            foreach (var memberGuid in aggregate.MemberGuids)
                            {
                                _agentToAggregateMap[memberGuid] = intent.AggregateGuid;
                            }
                            break;
                        }
                    }
                }
            }
        }

        private void EvaluateGoals(GoalProfile goals, PersonalityProfile personality, BehaviorProfile behavior, CognitiveMemory memory, AgentGuid agentGuid)
        {
            // Get aggregate intent for this agent (if agent belongs to an aggregate)
            AggregateIntentMessage aggregateIntent = default;
            bool hasAggregateIntent = false;
            
            if (_agentToAggregateMap.TryGetValue(agentGuid, out var aggregateGuid))
            {
                if (_aggregateIntentCache.TryGetValue(aggregateGuid, out var intent))
                {
                    aggregateIntent = intent;
                    hasAggregateIntent = true;
                }
            }

            // Update goal priorities based on personality traits, percepts, and aggregate intents
            for (int i = 0; i < goals.ActiveGoals.Count; i++)
            {
                var goal = goals.ActiveGoals[i];
                
                // Adjust priority based on personality
                float adjustedPriority = goal.Priority;
                
                // Adjust based on percepts
                if (memory != null)
                {
                    var visionConfidence = memory.GetSensorConfidence(SensorType.Vision);
                    var smellConfidence = memory.GetSensorConfidence(SensorType.Smell);
                    var hearingConfidence = memory.GetSensorConfidence(SensorType.Hearing);

                    switch (goal.Type)
                    {
                        case "Combat":
                        case "Defend":
                            adjustedPriority *= (1f + personality.Aggressiveness);
                            // If visual confidence > 0.8 of hostile, increase combat priority
                            if (visionConfidence > 0.8f)
                            {
                                adjustedPriority *= 1.5f;
                            }
                            break;
                        case "Explore":
                            adjustedPriority *= (1f + personality.Curiosity);
                            break;
                        case "Social":
                            adjustedPriority *= (1f + personality.SocialPreference);
                            break;
                        case "Rest":
                            adjustedPriority *= (1f - personality.RiskTolerance);
                            break;
                        case "Harvest":
                            // If smell indicates fertile soil, increase harvest priority
                            if (smellConfidence > 0.6f)
                            {
                                adjustedPriority *= 1.3f;
                            }
                            break;
                    }
                }
                else
                {
                    // Fallback to personality-only adjustment
                    switch (goal.Type)
                    {
                        case "Combat":
                        case "Defend":
                            adjustedPriority *= (1f + personality.Aggressiveness);
                            break;
                        case "Explore":
                            adjustedPriority *= (1f + personality.Curiosity);
                            break;
                        case "Social":
                            adjustedPriority *= (1f + personality.SocialPreference);
                            break;
                        case "Rest":
                            adjustedPriority *= (1f - personality.RiskTolerance);
                            break;
                    }
                }

                // Apply aggregate intent bias
                if (hasAggregateIntent && aggregateIntent.DistributionRatios != null)
                {
                    // Get distribution ratio for this goal type
                    float distributionRatio = 0f;
                    if (aggregateIntent.DistributionRatios.TryGetValue(goal.Type, out var ratio))
                    {
                        distributionRatio = ratio;
                    }
                    else if (goal.Type == "Harvest" && aggregateIntent.DistributionRatios.TryGetValue("Farm", out var farmRatio))
                    {
                        distributionRatio = farmRatio; // Map "Farm" to "Harvest"
                    }

                    // Apply bias: increase priority based on aggregate distribution ratio and priority
                    if (distributionRatio > 0f)
                    {
                        float aggregateBias = 1f + (aggregateIntent.Priority * distributionRatio * 0.5f); // Max 50% boost
                        adjustedPriority *= aggregateBias;
                    }

                    // If aggregate goal matches this goal type, apply additional boost
                    if (aggregateIntent.GoalType == goal.Type)
                    {
                        adjustedPriority *= (1f + aggregateIntent.Priority * 0.3f); // Additional 30% boost
                    }
                }

                goal.Priority = adjustedPriority;
                goals.ActiveGoals[i] = goal;
            }

            // Select primary goal (highest priority)
            if (goals.ActiveGoals.Count > 0)
            {
                var primaryGoal = goals.ActiveGoals[0];
                float maxPriority = primaryGoal.Priority;
                goals.CurrentPrimaryGoalId = primaryGoal.Id;

                for (int i = 1; i < goals.ActiveGoals.Count; i++)
                {
                    if (goals.ActiveGoals[i].Priority > maxPriority)
                    {
                        maxPriority = goals.ActiveGoals[i].Priority;
                        goals.CurrentPrimaryGoalId = goals.ActiveGoals[i].Id;
                    }
                }
            }
        }

        private void GenerateIntent(in Entity entity, GoalProfile goals, PersonalityProfile personality, BehaviorProfile behavior, CognitiveMemory memory)
        {
            // Find primary goal
            GoalProfile.Goal primaryGoal = default;
            foreach (var goal in goals.ActiveGoals)
            {
                if (goal.Id == goals.CurrentPrimaryGoalId)
                {
                    primaryGoal = goal;
                    break;
                }
            }

            if (primaryGoal.Id == null)
            {
                return;
            }

            // Get AgentGuid from entity (stored as component in DefaultEcs world)
            if (!World.Has<AgentGuid>(entity))
            {
                return; // Entity not mapped to Body ECS yet
            }

            var agentGuid = World.Get<AgentGuid>(entity);

            // Generate intent based on goal type
            IntentKind intentKind = IntentKind.None;
            float3 targetPosition = float3.zero;
            Entity targetEntity = Entity.Null;
            
            switch (primaryGoal.Type)
            {
                case "Move":
                    intentKind = IntentKind.Move;
                    // Extract target position from goal parameters if available
                    if (primaryGoal.Parameters != null && primaryGoal.Parameters.TryGetValue("TargetPosition", out var posObj))
                    {
                        if (posObj is float3 pos)
                            targetPosition = pos;
                    }
                    break;
                case "Attack":
                    intentKind = IntentKind.Attack;
                    // Extract target entity from goal parameters if available
                    if (primaryGoal.Parameters != null && primaryGoal.Parameters.TryGetValue("TargetEntity", out var entObj))
                    {
                        // Note: Entity references don't cross ECS boundaries, use GUID lookup instead
                    }
                    break;
                case "Harvest":
                    intentKind = IntentKind.Harvest;
                    break;
                case "Rest":
                    intentKind = IntentKind.Rest;
                    break;
                case "Flee":
                    intentKind = IntentKind.Flee;
                    break;
            }

            // Send intent to AgentSyncBus
            if (_syncBus != null && intentKind != IntentKind.None)
            {
                var message = new MindToBodyMessage
                {
                    AgentGuid = agentGuid,
                    Kind = intentKind,
                    TargetPosition = targetPosition,
                    TargetEntity = targetEntity,
                    Priority = (byte)math.clamp((int)(primaryGoal.Priority * 255f), 0, 255),
                    TickNumber = 0 // Will be set by bridge system
                };

                _syncBus.EnqueueMindToBody(message);
            }
        }

        private void GenerateLimbCommands(in Entity entity, AgentGuid agentGuid, CognitiveMemory memory, PersonalityProfile personality)
        {
            if (_syncBus == null || memory == null)
            {
                return;
            }

            // Get recent percepts for this agent
            if (!_perceptMemory.TryGetValue(agentGuid, out var percepts) || percepts.Count == 0)
            {
                return;
            }

            // Evaluate percepts and generate limb commands
            foreach (var percept in percepts)
            {
                // If visual confidence > 0.8 of hostile → activate limb: Weapon
                if (percept.Type == SensorType.Vision && percept.Confidence > 0.8f)
                {
                    // Assume limb index 0 is weapon (in production, lookup actual limb)
                    var command = new LimbCommand
                    {
                        AgentGuid = agentGuid,
                        LimbIndex = 0, // TODO: Map to actual weapon limb index
                        Action = LimbAction.Activate,
                        Target = percept.Source,
                        Priority = (byte)math.clamp((int)(percept.Confidence * 255f), 0, 255),
                        TickNumber = percept.TickNumber
                    };
                    _syncBus.EnqueueLimbCommand(command);
                }

                // If smell indicates fertile soil → activate limb: Manipulator (Farm)
                if (percept.Type == SensorType.Smell && percept.Confidence > 0.6f)
                {
                    // Assume limb index 1 is manipulator (in production, lookup actual limb)
                    var command = new LimbCommand
                    {
                        AgentGuid = agentGuid,
                        LimbIndex = 1, // TODO: Map to actual manipulator limb index
                        Action = LimbAction.Activate,
                        Target = percept.Source,
                        Priority = (byte)math.clamp((int)(percept.Confidence * 255f), 0, 255),
                        TickNumber = percept.TickNumber
                    };
                    _syncBus.EnqueueLimbCommand(command);
                }
            }
        }
    }
}

