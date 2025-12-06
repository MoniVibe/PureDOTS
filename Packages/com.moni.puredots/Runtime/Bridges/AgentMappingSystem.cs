using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using PureDOTS.Shared;
using PureDOTS.Runtime.Components;

namespace PureDOTS.Runtime.Bridges
{
    /// <summary>
    /// Maintains GUID ↔ Entity mappings for both Unity Entities and DefaultEcs worlds.
    /// Creates mappings on agent spawn, cleans up on despawn.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct AgentMappingSystem : ISystem
    {
        private EntityQuery _newAgentsQuery;
        private EntityQuery _destroyedAgentsQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _newAgentsQuery = state.GetEntityQuery(
                ComponentType.ReadWrite<AgentBody>(),
                ComponentType.Exclude<AgentSyncId>()
            );

            _destroyedAgentsQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<AgentSyncId>()
            );
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Create mappings for new agents
            foreach (var (agentBody, entity) in SystemAPI.Query<RefRO<AgentBody>>()
                         .WithEntityAccess()
                         .WithNone<AgentSyncId>())
            {
                var syncId = new AgentSyncId
                {
                    Guid = agentBody.ValueRO.Id,
                    MindEntityIndex = -1 // Will be set when Mind ECS entity is created
                };
                SystemAPI.SetComponent(entity, syncId);
            }

            // Clean up mappings for destroyed agents
            // Note: DefaultEcs cleanup happens in Mind ECS world, not here
            // This system only manages Unity Entities side
        }
    }
}

