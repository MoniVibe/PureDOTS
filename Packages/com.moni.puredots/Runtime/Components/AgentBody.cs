using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Shared;

namespace PureDOTS.Runtime.Components
{
    /// <summary>
    /// Body ECS component representing physical agent state.
    /// Burst-safe, deterministic simulation component.
    /// </summary>
    public struct AgentBody : IComponentData
    {
        public AgentGuid Id;
        public float3 Position;
        public quaternion Rotation;
    }

    /// <summary>
    /// Intent command issued by Mind ECS and resolved by Body ECS systems.
    /// Burst-safe component for deterministic intent resolution.
    /// </summary>
    public struct IntentCommand : IComponentData
    {
        public PureDOTS.Runtime.Bridges.IntentKind Kind;
        public float3 TargetPosition;
        public Entity TargetEntity;
        public byte Priority;
        public uint TickNumber; // Timestamp when intent was issued
    }
}

