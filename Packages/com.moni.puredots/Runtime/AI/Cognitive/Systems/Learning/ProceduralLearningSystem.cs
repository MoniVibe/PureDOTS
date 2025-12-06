using PureDOTS.Runtime.AI.Cognitive;
using PureDOTS.Runtime.Bridges;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Core;
using PureDOTS.Systems;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Runtime.AI.Cognitive.Systems.Learning
{
    /// <summary>
    /// Procedural learning system - 1Hz cognitive layer.
    /// Implements perceive/query/select/act/reinforce/store cycle for procedural memory.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(LearningSystemGroup))]
    public partial struct ProceduralLearningSystem : ISystem
    {
        private const float UpdateInterval = 1.0f; // 1Hz
        private float _lastUpdateTime;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RewindState>();
            state.RequireForUpdate<TickTimeState>();
            _lastUpdateTime = 0f;
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var rewind = SystemAPI.GetSingleton<RewindState>();
            if (rewind.Mode != RewindMode.Record && rewind.Mode != RewindMode.CatchUp)
            {
                return;
            }

            var tickTime = SystemAPI.GetSingleton<TickTimeState>();
            if (tickTime.IsPaused)
            {
                return;
            }

            var currentTime = (float)SystemAPI.Time.ElapsedTime;
            if (currentTime - _lastUpdateTime < UpdateInterval)
            {
                return;
            }

            _lastUpdateTime = currentTime;

            var cognitiveStatsLookup = state.GetComponentLookup<CognitiveStats>(true);
            cognitiveStatsLookup.Update(ref state);

            var job = new ProceduralLearningJob
            {
                CurrentTick = tickTime.Tick,
                CognitiveStatsLookup = cognitiveStatsLookup
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        private partial struct ProceduralLearningJob : IJobEntity
        {
            public uint CurrentTick;
            [ReadOnly] public ComponentLookup<CognitiveStats> CognitiveStatsLookup;

            public void Execute(
                Entity entity,
                [ChunkIndexInQuery] int chunkIndex,
                in ContextHash contextHash,
                ref ProceduralMemory memory,
                ref DynamicBuffer<AgentIntentBuffer> intentBuffer)
            {
                // PERCEIVE: Encode current context → hash
                byte currentContextHash = contextHash.Hash;
                if (currentContextHash == 0)
                {
                    // Context not computed yet, skip this cycle
                    return;
                }

                // QUERY: Look up prior actions for this context
                ActionId selectedAction = ActionId.None;
                bool hasPriorKnowledge = false;
                float bestScore = 0f;
                int bestIndex = -1;

                for (int i = 0; i < memory.TriedActions.Length && i < memory.SuccessScores.Length; i++)
                {
                    if (memory.ContextHash == currentContextHash)
                    {
                        float score = memory.SuccessScores[i];
                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestIndex = i;
                            selectedAction = memory.TriedActions[i];
                            hasPriorKnowledge = true;
                        }
                    }
                }

                // SELECT: Choose highest-score known action, or explore based on Curiosity/Focus
                if (!hasPriorKnowledge || bestScore < 0.3f)
                {
                    // Get CognitiveStats if available, otherwise use defaults
                    CognitiveStats cognitiveStats = CognitiveStatsLookup.HasComponent(entity)
                        ? CognitiveStatsLookup[entity]
                        : CognitiveStats.CreateDefaults();

                    // Calculate exploration probability: ExplorationChance = 0.1f + Curiosity * (1 - Focus / MaxFocus)
                    float curiosityNorm = CognitiveStats.Normalize(cognitiveStats.Curiosity);
                    float focusRatio = cognitiveStats.MaxFocus > 0f ? cognitiveStats.Focus / cognitiveStats.MaxFocus : 1f;
                    float explorationChance = 0.1f + curiosityNorm * (1f - focusRatio);
                    
                    // Use deterministic tick-based seed for probability check (Burst-safe)
                    float randomValue = (CurrentTick % 1000) / 1000f;
                    
                    if (randomValue < explorationChance)
                    {
                        // Explore: use deterministic tick-based variation
                        int explorationIndex = (int)(CurrentTick % 8); // Cycle through 8 basic actions
                        selectedAction = (ActionId)((explorationIndex % 8) + 1); // Map to ActionId 1-8
                    }
                    else if (hasPriorKnowledge)
                    {
                        // Exploit: use best known action even if score is low
                        selectedAction = memory.TriedActions[bestIndex];
                    }
                }

                // ACT: Perform action (write intent)
                if (selectedAction != ActionId.None)
                {
                    intentBuffer.Add(new AgentIntentBuffer
                    {
                        Kind = ActionIdToIntentKind(selectedAction),
                        TargetPosition = float3.zero, // Will be filled by other systems
                        TargetEntity = Entity.Null,
                        Priority = 128, // Medium priority for learned actions
                        TickNumber = CurrentTick
                    });
                }

                // REINFORCE: Update success scores (called by outcome tracking system)
                // This happens when action outcomes are reported back
                // For now, we just maintain the memory structure

                // STORE: Maintain top-N successful chains per context
                // This is handled by periodic cleanup/pruning systems
                memory.LastUpdateTick = CurrentTick;
            }

            private IntentKind ActionIdToIntentKind(ActionId actionId)
            {
                return actionId switch
                {
                    ActionId.Move => IntentKind.Move,
                    ActionId.Climb => IntentKind.Interact,
                    ActionId.Push => IntentKind.Interact,
                    ActionId.Pull => IntentKind.Interact,
                    ActionId.Jump => IntentKind.Move,
                    ActionId.Throw => IntentKind.Interact,
                    ActionId.Use => IntentKind.Interact,
                    ActionId.Grab => IntentKind.Interact,
                    ActionId.Drop => IntentKind.Interact,
                    ActionId.EscapePit => IntentKind.Move,
                    _ => IntentKind.Move
                };
            }
        }
    }

    /// <summary>
    /// System that reinforces procedural memory based on action outcomes.
    /// Called when actions complete to update success scores.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(LearningSystemGroup))]
    [UpdateAfter(typeof(ProceduralLearningSystem))]
    public partial struct ProceduralMemoryReinforcementSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RewindState>();
            state.RequireForUpdate<TickTimeState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var rewind = SystemAPI.GetSingleton<RewindState>();
            if (rewind.Mode != RewindMode.Record && rewind.Mode != RewindMode.CatchUp)
            {
                return;
            }

            var tickTime = SystemAPI.GetSingleton<TickTimeState>();
            if (tickTime.IsPaused)
            {
                return;
            }

            // This system would be called with outcome data from action execution
            // For now, it's a placeholder that will be extended when action outcome tracking is implemented
        }

        /// <summary>
        /// Reinforce a specific action in procedural memory.
        /// Called externally when action outcomes are known.
        /// Applies Intelligence/Wisdom multipliers to learning rate.
        /// Formula: ΔScore = (Success - Failure) * BaseRate * (0.6f * Intelligence + 0.4f * Wisdom) * (1 + Curiosity * 0.5f)
        /// </summary>
        [BurstCompile]
        public static void ReinforceAction(
            ref ProceduralMemory memory,
            byte contextHash,
            ActionId actionId,
            float successResult, // 0.0 = failure, 1.0 = success
            float baseLearningRate,
            in CognitiveStats cognitiveStats)
        {
            if (memory.ContextHash != contextHash)
            {
                // New context, initialize
                memory.ContextHash = contextHash;
                memory.TriedActions.Clear();
                memory.SuccessScores.Clear();
            }

            // Calculate effective learning rate using Intelligence/Wisdom/Curiosity multipliers
            float intNorm = CognitiveStats.Normalize(cognitiveStats.Intelligence);
            float wisNorm = CognitiveStats.Normalize(cognitiveStats.Wisdom);
            float curNorm = CognitiveStats.Normalize(cognitiveStats.Curiosity);
            
            // EffectiveLearningRate = BaseRate * (0.6 * Intelligence + 0.4 * Wisdom) * (1 + Curiosity * 0.5)
            float cognitiveMultiplier = 0.6f * intNorm + 0.4f * wisNorm;
            float curiosityMultiplier = 1f + curNorm * 0.5f;
            float effectiveLearningRate = baseLearningRate * cognitiveMultiplier * curiosityMultiplier;

            // Find existing action or add new one
            int actionIndex = -1;
            for (int i = 0; i < memory.TriedActions.Length; i++)
            {
                if (memory.TriedActions[i] == actionId)
                {
                    actionIndex = i;
                    break;
                }
            }

            if (actionIndex >= 0)
            {
                // Update existing score: lerp(old, new, effectiveLearningRate)
                float oldScore = memory.SuccessScores[actionIndex];
                float newScore = math.lerp(oldScore, successResult, effectiveLearningRate);
                memory.SuccessScores[actionIndex] = newScore;
            }
            else if (memory.TriedActions.Length < 64)
            {
                // Add new action
                memory.TriedActions.Add(actionId);
                memory.SuccessScores.Add(successResult);
            }
        }

        /// <summary>
        /// Legacy overload for backward compatibility (uses default learning rate without stats).
        /// </summary>
        [BurstCompile]
        public static void ReinforceAction(
            ref ProceduralMemory memory,
            byte contextHash,
            ActionId actionId,
            float successResult,
            float learningRate)
        {
            // Use default stats for backward compatibility
            var defaultStats = CognitiveStats.CreateDefaults();
            ReinforceAction(ref memory, contextHash, actionId, successResult, learningRate, in defaultStats);
        }
    }
}

