using PureDOTS.Runtime.AI.Cognitive;
using PureDOTS.Runtime.Core;
using PureDOTS.Systems;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.AI.Cognitive.Systems.Learning
{
    /// <summary>
    /// Causal chain system - 1Hz cognitive layer.
    /// Maintains lightweight causal graphs per agent for reasoning.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(LearningSystemGroup))]
    public partial struct CausalChainSystem : ISystem
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

            // Causal links are reinforced when action outcomes are reported
            // This system mainly maintains the graph structure
            // Reinforcement happens in ProceduralMemoryReinforcementSystem
        }

        /// <summary>
        /// Reinforce a causal link based on action outcome.
        /// Called externally when action outcomes are known.
        /// </summary>
        [BurstCompile]
        public static void ReinforceCausalLink(
            ref DynamicBuffer<CausalLink> causalLinks,
            ushort cause,
            ushort effect,
            float successResult, // 0.0 = failure, 1.0 = success
            uint currentTick,
            float reinforcementRate = 0.1f)
        {
            // Find existing link or create new one
            int linkIndex = -1;
            for (int i = 0; i < causalLinks.Length; i++)
            {
                if (causalLinks[i].Cause == cause && causalLinks[i].Effect == effect)
                {
                    linkIndex = i;
                    break;
                }
            }

            if (linkIndex >= 0)
            {
                // Update existing link weight
                var link = causalLinks[linkIndex];
                float newWeight = math.lerp(link.Weight, successResult, reinforcementRate);
                link.Weight = math.clamp(newWeight, 0f, 1f);
                link.LastReinforcedTick = currentTick;
                link.ObservationCount++;
                causalLinks[linkIndex] = link;
            }
            else if (causalLinks.Length < 64)
            {
                // Add new causal link
                causalLinks.Add(new CausalLink
                {
                    Cause = cause,
                    Effect = effect,
                    Weight = successResult,
                    LastReinforcedTick = currentTick,
                    ObservationCount = 1
                });
            }
        }

        /// <summary>
        /// Query causal graph for expected outcome of an action.
        /// Returns weight of causal link if found, 0.0 otherwise.
        /// </summary>
        [BurstCompile]
        public static float QueryCausalLink(
            in DynamicBuffer<CausalLink> causalLinks,
            ushort cause,
            ushort effect)
        {
            for (int i = 0; i < causalLinks.Length; i++)
            {
                var link = causalLinks[i];
                if (link.Cause == cause && link.Effect == effect)
                {
                    return link.Weight;
                }
            }
            return 0f;
        }
    }
}

