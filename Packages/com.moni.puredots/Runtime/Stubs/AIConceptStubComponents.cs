// [TRI-STUB] Ahead-of-time stub. Compile only when PUREDOTS_STUBS is defined.
using Unity.Entities;

namespace PureDOTS.Runtime.AI
{
    public struct BehaviorTreeHandle : IComponentData
    {
        public int TreeId;
        public byte Version;
    }

    public struct BehaviorTaskState : IComponentData
    {
        public int ActiveNodeId;
        public byte Phase;
    }

    public struct BehaviorNodeState : IBufferElementData
    {
        public int NodeId;
        public byte Status;
        public byte Flags;
    }

    public struct PerceptionConfig : IComponentData
    {
        public float Range;
        public float CooldownSeconds;
        public byte ChannelsMask;
        public byte MaxStimuli;
    }

    public struct PerceptionStimulus : IBufferElementData
    {
        public Entity Source;
        public float Strength;
        public byte Channel;
        public uint Timestamp;
    }
}
