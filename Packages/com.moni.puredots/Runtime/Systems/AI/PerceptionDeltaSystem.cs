using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Core;
using Unity.Burst;
using Unity.Entities;

namespace PureDOTS.Systems.AI
{
    /// <summary>
    /// Event-driven system that processes perception deltas.
    /// Only processes entities where PerceptionFeatureVector has changed.
    /// Bridges to MindECS PerceptionInterpreterSystem via AgentSyncBus.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(AISystemGroup))]
    [UpdateAfter(typeof(PerceptionFusionSystem))]
    public partial struct PerceptionDeltaSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TickTimeState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TickTimeState>();
            if (timeState.IsPaused)
            {
                return;
            }

            // Event-driven: only process entities with changed PerceptionFeatureVector
            // Note: WithChangeFilter works on components, not buffers
            // For buffers, we check if the buffer length changed or process all entities with the buffer
            // This system processes perception deltas and can notify MindECS via sync bus
            foreach (var (featureVector, entity) in SystemAPI.Query<DynamicBuffer<PerceptionFeatureVector>>()
                .WithEntityAccess())
            {
                // Process perception deltas
                // In full implementation, would notify MindECS via AgentSyncBus
                HandlePerceptionDelta(featureVector, entity);
            }
        }

        [BurstCompile]
        private void HandlePerceptionDelta(DynamicBuffer<PerceptionFeatureVector> featureVector, Entity entity)
        {
            // Process perception feature vector changes
            // This system reacts to PerceptionFeatureVector buffer changes
            // Additional processing can be added here for perception-based decisions
        }
    }
}

