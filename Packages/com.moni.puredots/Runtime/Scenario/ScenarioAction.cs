using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Runtime.Scenario
{
    /// <summary>
    /// Represents an action taken in the scenario editor.
    /// Stored in DynamicBuffer for serialization.
    /// </summary>
    public struct ScenarioAction : IBufferElementData
    {
        public ScenarioActionType Type;
        public Entity PrefabEntity; // For AddEntity actions
        public Entity TargetEntity; // For component modification actions
        public float3 Position;
        public FixedString128Bytes ComponentTypeName; // For component actions
        public FixedString512Bytes ComponentDataJson; // Serialized component data
    }

    /// <summary>
    /// Types of actions that can be recorded in a scenario.
    /// </summary>
    public enum ScenarioActionType : byte
    {
        AddEntity = 0,
        AddComponent = 1,
        ModifyComponent = 2
    }
}

