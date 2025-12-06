using PureDOTS.Shared;
using Unity.Entities;

namespace PureDOTS.Runtime.Components
{
    /// <summary>
    /// Burst-safe component indicating which aggregate an agent belongs to.
    /// Used for linking Body ECS entities to Aggregate ECS entities.
    /// </summary>
    public struct AggregateMembership : IComponentData
    {
        public AgentGuid AggregateGuid; // Which aggregate this agent belongs to
        public byte Role; // Role within aggregate (optional, 0 = default member)
    }
}

