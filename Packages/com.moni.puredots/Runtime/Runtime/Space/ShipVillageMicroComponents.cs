using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.Space
{
    // Keep the root entity compact; larger transient data lives on companion entities.
    public struct ShipRootTag : IComponentData
    {
    }

    public struct ShipId : IComponentData
    {
        public int Value;
    }

    public struct ShipDesignRef : IComponentData
    {
        public int Value;
    }

    public enum ShipOrderType : byte
    {
        HoldCourse = 0,
        SurveyArc = 1,
        FocusTargeting = 2
    }

    public enum ShipOrderState : byte
    {
        Idle = 0,
        Issued = 1,
        Executing = 2,
        Complete = 3
    }

    public struct ShipOrder : IComponentData
    {
        public ShipOrderType Type;
        public ShipOrderState State;
        public uint Sequence;
        public uint IssuedTick;
    }

    public struct ShipIntent : IComponentData
    {
        public float Readiness;
        public float Coordination;
        public byte CanExecute;
        public uint LastAppliedTick;
    }

    public struct ShipOrderCadence : IComponentData
    {
        public uint NextInjectTick;
        public uint InjectEveryTicks;
    }

    public enum SeatRoleKind : byte
    {
        Captain = 0,
        Navigation = 1,
        Sensors = 2,
        Weapons = 3,
        Logistics = 4,
        Engineering = 5
    }

    public struct SeatRole : IComponentData
    {
        public SeatRoleKind Value;
    }

    public struct SeatAssignment : IComponentData
    {
        public Entity Ship;
    }

    public struct SeatState : IComponentData
    {
        public byte Manned;
        public byte CrewAssigned;
        public float Efficiency;
        public float Readiness;
        public uint LastDecisionTick;
    }

    public struct SeatIntent : IComponentData
    {
        public float ReadinessDelta;
        public float CoordinationDelta;
        public byte ConfirmedOrder;
    }

    public struct CrewId : IComponentData
    {
        public int Value;
    }

    public struct CrewProfileRef : IComponentData
    {
        public int Value;
    }

    public struct CrewState : IComponentData
    {
        public float Stress;
        public float Fatigue;
        public float Skill;
    }

    public struct CrewSeatAssignment : IComponentData
    {
        public Entity Seat;
    }

    public struct ShipCommsTag : IComponentData
    {
    }

    public struct ShipCommsLink : IComponentData
    {
        public Entity CommsEntity;
    }

    public struct ShipCommsRuntime : IComponentData
    {
        public uint TotalEvents;
        public uint EventsSinceTranscript;
        public uint LastTranscriptTick;
        public byte LastEventCode;
        public SeatRoleKind LastFromRole;
        public SeatRoleKind LastToRole;
    }

    [InternalBufferCapacity(24)]
    public struct CommsEvent : IBufferElementData
    {
        public uint Tick;
        public SeatRoleKind FromRole;
        public SeatRoleKind ToRole;
        public byte EventCode;
        public float Payload;
    }
}
