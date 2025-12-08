using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Core;
using Unity.Burst;
using Unity.Entities;

namespace PureDOTS.Runtime.Systems.Streaming
{
    /// <summary>
    /// Maintains CellAgentBuffer membership by pruning invalid agent references.
    /// Keeps streaming counts accurate when agents are destroyed.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(CellSerializationSystem))]
    public partial struct CellAgentBufferMaintenanceSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TickTimeState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var tickState = SystemAPI.GetSingleton<TickTimeState>();
            if (tickState.IsPaused)
            {
                return;
            }

            var entityManager = state.EntityManager;

            foreach (var agents in SystemAPI.Query<DynamicBuffer<CellAgentBuffer>>())
            {
                // Remove invalid agent references (destroyed entities).
                for (int i = agents.Length - 1; i >= 0; i--)
                {
                    var agent = agents[i].AgentEntity;
                    if (!entityManager.Exists(agent))
                    {
                        agents.RemoveAt(i);
                    }
                }
            }
        }
    }
}
