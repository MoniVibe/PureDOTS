using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Components
{
    public enum HandState : byte
    {
        Empty = 0,
        Holding = 1,
        Dragging = 2,
        SlingshotAim = 3,
        Dumping = 4
    }

    public enum DivineHandEventType : byte
    {
        StateChanged = 0,
        TypeChanged = 1,
        AmountChanged = 2
    }

    public static class DivineHandConstants
    {
        public const ushort NoResourceType = ushort.MaxValue;
    }

    public struct DivineHandTag : IComponentData { }

    public struct DivineHandConfig : IComponentData
    {
        public float PickupRadius;
        public float MaxGrabDistance;
        public float HoldLerp;
        public float ThrowImpulse;
        public float ThrowChargeMultiplier;
        public float HoldHeightOffset;
        public float CooldownAfterThrowSeconds;
        public float MinChargeSeconds;
        public float MaxChargeSeconds;
        public int HysteresisFrames;
        public int HeldCapacity;
        public float SiphonRate;
        public float DumpRate;
    }

    public struct DivineHandState : IComponentData
    {
        public Entity HeldEntity;
        public float3 CursorPosition;
        public float3 AimDirection;
        public float3 HeldLocalOffset;
        public HandState CurrentState;
        public HandState PreviousState;
        public float ChargeTimer;
        public float CooldownTimer;
        public ushort HeldResourceTypeIndex;
        public int HeldAmount;
        public int HeldCapacity;
        public byte Flags;
    }

    public struct DivineHandInput : IComponentData
    {
        public float3 CursorPosition;
        public float3 AimDirection;
        public byte GrabPressed;
        public byte GrabReleased;
        public byte ThrowPressed;
        public float ThrowCharge;
    }

    public struct DivineHandEvent : IBufferElementData
    {
        public DivineHandEventType Type;
        public HandState FromState;
        public HandState ToState;
        public ushort ResourceTypeIndex;
        public int Amount;
        public int Capacity;

        public static DivineHandEvent StateChange(HandState from, HandState to)
        {
            return new DivineHandEvent
            {
                Type = DivineHandEventType.StateChanged,
                FromState = from,
                ToState = to,
                ResourceTypeIndex = DivineHandConstants.NoResourceType
            };
        }

        public static DivineHandEvent TypeChange(ushort resourceTypeIndex)
        {
            return new DivineHandEvent
            {
                Type = DivineHandEventType.TypeChanged,
                ResourceTypeIndex = resourceTypeIndex
            };
        }

        public static DivineHandEvent AmountChange(int amount, int capacity)
        {
            return new DivineHandEvent
            {
                Type = DivineHandEventType.AmountChanged,
                Amount = amount,
                Capacity = capacity,
                ResourceTypeIndex = DivineHandConstants.NoResourceType
            };
        }
    }

    public enum DivineHandCommandType : byte
    {
        None = 0,
        DumpToStorehouse = 1,
        SiphonPile = 2,
        GroundDrip = 3
    }

    public struct DivineHandCommand : IComponentData
    {
        public DivineHandCommandType Type;
        public Entity TargetEntity;
        public float3 TargetPosition;
        public float3 TargetNormal;
        public float TimeSinceIssued;
    }

    public enum HandHighlightType : byte
    {
        None = 0,
        Storehouse = 1,
        Pile = 2,
        Draggable = 3,
        Ground = 4
    }

    public struct DivineHandHighlight : IComponentData
    {
        public HandHighlightType Type;
        public Entity TargetEntity;
        public float3 Position;
        public float3 Normal;
    }

    public struct HandPickable : IComponentData
    {
        public float Mass;
        public float MaxHoldDistance;
        public float ThrowImpulseMultiplier;
        public float FollowLerp;
    }

    public struct HandHeldTag : IComponentData
    {
        public Entity Holder;
    }
}
