using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Input
{
    public struct HandInputTag : IComponentData { }

    public struct RtsInputSingletonTag : IComponentData { }

    public enum HandRoutePhase : byte
    {
        None = 0,
        Started = 1,
        Canceled = 2
    }

    public enum DivineHandCommandType : byte
    {
        None = 0,
        Select = 1,
        Place = 2
    }

    public struct HandInputRouteRequest : IBufferElementData
    {
        public HandRoutePhase Phase;
        public int Priority;
        public int Source;
        public DivineHandCommandType CommandType;
        public Entity TargetEntity;
        public float3 TargetPosition;
        public float3 TargetNormal;
    }

    public struct HandInputRouteResult : IComponentData
    {
        public int Source;
        public int Priority;
        public DivineHandCommandType CommandType;
        public Entity TargetEntity;
        public float3 TargetPosition;
        public float3 TargetNormal;

        public static HandInputRouteResult None => default;
    }

    public struct DivineHandCommand : IComponentData
    {
        public DivineHandCommandType Type;
        public Entity TargetEntity;
        public float3 TargetPosition;
        public float3 TargetNormal;
        public float TimeSinceIssued;
    }

    public struct GodIntent : IComponentData
    {
        public byte CancelAction;
        public byte StartSelect;
        public byte ConfirmPlace;
    }

    public struct RockBreakEvent : IBufferElementData
    {
        public Entity RockEntity;
        public float3 HitPosition;
    }

    public enum SaveLoadCommandKind : byte
    {
        QuickSave = 0,
        QuickLoad = 1
    }

    public struct SaveLoadCommandEvent : IBufferElementData
    {
        public SaveLoadCommandKind Kind;
    }
}

namespace PureDOTS.Systems.Input
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct InputBridgeSystem : ISystem
    {
        public void OnCreate(ref SystemState state) { }

        public void OnDestroy(ref SystemState state) { }

        public void OnUpdate(ref SystemState state) { }
    }
}
