// [TRI-STUB] This is an ahead-of-time stub. Safe to compile, does nothing at runtime.
using Unity.Entities;

namespace PureDOTS.Runtime.Behavior
{
    public struct BehaviorProfileId : IComponentData
    {
        public int Profile;
    }

    public struct BehaviorModifier : IComponentData
    {
        public float Value;
    }

    public struct InitiativeStat : IComponentData
    {
        public float Charge;
        public float Cooldown;
    }

    public struct NeedCategory : IComponentData
    {
        public byte Type;
    }

    public struct NeedSatisfaction : IComponentData
    {
        public float Value;
    }

    public struct NeedRequestElement : IBufferElementData
    {
        public byte NeedType;
        public float Urgency;
    }
}
