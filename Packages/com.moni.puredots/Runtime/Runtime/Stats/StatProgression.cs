using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Stats
{
    /// <summary>
    /// Stat history sample for rewind compatibility.
    /// Tracks stat changes over time for deterministic replay.
    /// </summary>
    public struct StatHistorySample : IBufferElementData
    {
        /// <summary>
        /// Tick when this sample was recorded.
        /// </summary>
        public uint Tick;

        /// <summary>
        /// Command stat at this tick.
        /// </summary>
        public half Command;

        /// <summary>
        /// Tactics stat at this tick.
        /// </summary>
        public half Tactics;

        /// <summary>
        /// Logistics stat at this tick.
        /// </summary>
        public half Logistics;

        /// <summary>
        /// Diplomacy stat at this tick.
        /// </summary>
        public half Diplomacy;

        /// <summary>
        /// Engineering stat at this tick.
        /// </summary>
        public half Engineering;

        /// <summary>
        /// Resolve stat at this tick.
        /// </summary>
        public half Resolve;

        /// <summary>
        /// General XP pool at this tick.
        /// </summary>
        public float GeneralXP;
    }

    /// <summary>
    /// XP command log entry for stat progression tracking.
    /// Records XP gains/spending/resets for deterministic replay.
    /// </summary>
    public struct StatXPCommandLogEntry : IBufferElementData
    {
        /// <summary>
        /// Tick when this XP change occurred.
        /// </summary>
        public uint Tick;

        /// <summary>
        /// Target entity receiving the XP change.
        /// </summary>
        public Entity TargetEntity;

        /// <summary>
        /// Stat type affected (Command, Tactics, Logistics, Diplomacy, Engineering, Resolve, Physique, Finesse, Will, General).
        /// </summary>
        public byte StatType;

        /// <summary>
        /// XP amount (positive for gain, negative for spend).
        /// </summary>
        public float XPAmount;

        /// <summary>
        /// Change type: 0 = Gain, 1 = Spend, 2 = Reset.
        /// </summary>
        public byte ChangeType;
    }

    /// <summary>
    /// Stat type enumeration for XP command logs.
    /// </summary>
    public enum StatType : byte
    {
        Command = 0,
        Tactics = 1,
        Logistics = 2,
        Diplomacy = 3,
        Engineering = 4,
        Resolve = 5,
        Physique = 6,
        Finesse = 7,
        Will = 8,
        General = 9
    }

    /// <summary>
    /// XP change type enumeration.
    /// </summary>
    public enum StatXPChangeType : byte
    {
        Gain = 0,
        Spend = 1,
        Reset = 2
    }

    /// <summary>
    /// Service contract component for tracking employment agreements.
    /// </summary>
    public struct ServiceContract : IComponentData
    {
        /// <summary>
        /// Employer identifier (Fleet, manufacturer, guild).
        /// </summary>
        public FixedString64Bytes EmployerId;

        /// <summary>
        /// Contract type: 0 = Fleet, 1 = Manufacturer, 2 = MercenaryGuild.
        /// </summary>
        public byte Type;

        /// <summary>
        /// Tick when contract started.
        /// </summary>
        public uint StartTick;

        /// <summary>
        /// Contract duration in ticks (1-5 years).
        /// </summary>
        public uint DurationTicks;

        /// <summary>
        /// Tick when contract expires.
        /// </summary>
        public uint ExpiryTick;

        /// <summary>
        /// Whether contract is currently active (1 = active, 0 = inactive).
        /// </summary>
        public byte IsActive;
    }

    /// <summary>
    /// Stat display binding for presentation layer.
    /// Allows HUDs to subscribe to stat data.
    /// </summary>
    public struct StatDisplayBinding : IComponentData
    {
        /// <summary>
        /// Reference to entity with stats.
        /// </summary>
        public FixedString64Bytes EntityId;

        /// <summary>
        /// Display mode: 0 = Current, 1 = Max, 2 = Average, 3 = Trend.
        /// </summary>
        public byte Mode;

        /// <summary>
        /// Which stats to display (bitmask or array index).
        /// </summary>
        public byte VisibleStatsMask;
    }
}

