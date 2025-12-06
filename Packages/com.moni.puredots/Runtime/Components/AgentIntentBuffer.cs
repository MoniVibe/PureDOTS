using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Runtime.Bridges;

namespace PureDOTS.Runtime.Components
{
    /// <summary>
    /// Buffer element storing intents from Mind ECS.
    /// Burst-safe buffer for deterministic intent resolution.
    /// </summary>
    public struct AgentIntentBuffer : IBufferElementData
    {
        public IntentKind Kind;
        public float3 TargetPosition;
        public Entity TargetEntity;
        public byte Priority;
        public uint TickNumber; // Timestamp when intent was issued
    }
}

