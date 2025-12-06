using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.AI
{
    /// <summary>
    /// Type of limb capability.
    /// </summary>
    public enum LimbType : byte
    {
        Sensor = 0,
        Manipulator = 1,
        Weapon = 2,
        Locomotion = 3
    }

    /// <summary>
    /// Capability action types for limb activation.
    /// </summary>
    public enum LimbAction : byte
    {
        None = 0,
        Activate = 1,
        Deactivate = 2,
        Target = 3,
        Use = 4
    }

    /// <summary>
    /// Buffer element linking limb entities to their parent agent.
    /// </summary>
    public struct LimbElement : IBufferElementData
    {
        public Entity LimbEntity;
    }

    /// <summary>
    /// Health and damage state for a limb.
    /// </summary>
    public struct LimbHealth : IComponentData
    {
        public float Value;
        public float MaxValue;
        public byte IsDestroyed; // 0 = functional, 1 = destroyed
    }

    /// <summary>
    /// Capability configuration for a limb.
    /// </summary>
    public struct LimbCapability : IComponentData
    {
        public LimbType Type;
        public byte Active; // 0 = inactive, 1 = active
        public byte Enabled; // 0 = disabled, 1 = enabled
    }

    /// <summary>
    /// Links a limb entity to its parent agent entity.
    /// </summary>
    public struct LimbParent : IComponentData
    {
        public Entity ParentAgent;
    }
}

