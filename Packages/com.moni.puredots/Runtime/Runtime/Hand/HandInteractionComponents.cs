using Unity.Entities;

namespace PureDOTS.Runtime.Components
{
    /// <summary>
    /// Shared hand interaction state consumed by resource and miracle systems.
    /// Mirrors the authoritative values driven by <see cref="DivineHandSystem"/>.
    /// </summary>
    public struct HandInteractionState : IComponentData
    {
        public Entity HandEntity;
        public HandState CurrentState;
        public HandState PreviousState;
        public DivineHandCommandType ActiveCommand;
        public ushort ActiveResourceType;
        public int HeldAmount;
        public int HeldCapacity;
        public float CooldownSeconds;
        public uint LastUpdateTick;
        public byte Flags;

        public const byte FlagMiracleArmed = 1 << 0;
        public const byte FlagSiphoning = 1 << 1;
        public const byte FlagDumping = 1 << 2;
    }

    /// <summary>
    /// Aggregated siphon state so resource and miracle chains operate on identical data.
    /// </summary>
    public struct ResourceSiphonState : IComponentData
    {
        public Entity HandEntity;
        public Entity TargetEntity;
        public ushort ResourceTypeIndex;
        public float SiphonRate;
        public float DumpRate;
        public float AccumulatedUnits;
        public uint LastUpdateTick;
        public byte Flags;

        public const byte FlagSiphoning = 1 << 0;
        public const byte FlagDumpCommandPending = 1 << 1;
    }
}
