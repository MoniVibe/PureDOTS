using Unity.Entities;

namespace PureDOTS.Runtime.Components
{
    /// <summary>
    /// Canonical timekeeping singleton for deterministic tick progression.
    /// Mirrors <see cref="TimeState"/> for backward compatibility and extends it with target tick + play state.
    /// </summary>
    public struct TickTimeState : IComponentData
    {
        public float FixedDeltaTime;
        public float CurrentSpeedMultiplier;
        public uint Tick;
        public uint TargetTick;
        public bool IsPaused;
        public bool IsPlaying;
    }

    /// <summary>
    /// Global time state used by simulation and presentation systems.
    /// Mirrors the deterministic tick progression used in the legacy DOTS stack.
    /// </summary>
    public struct TimeState : IComponentData
    {
        public float FixedDeltaTime;
        public float CurrentSpeedMultiplier;
        public uint Tick;
        public bool IsPaused;
    }

    /// <summary>
    /// Mirrors the simulation fixed-step cadence used by <see cref="FixedStepSimulationSystemGroup"/>.
    /// Updated alongside <see cref="TimeState"/> to keep deterministic systems in sync.
    /// </summary>
    public struct GameplayFixedStep : IComponentData
    {
        public float FixedDeltaTime;
    }

    /// <summary>
    /// Optional authoring/config singleton that sets up <see cref="TimeState"/> at runtime.
    /// </summary>
    public struct TimeSettingsConfig : IComponentData
    {
        public float FixedDeltaTime;
        public float DefaultSpeedMultiplier;
        public bool PauseOnStart;
    }

    public static class TimeSettingsDefaults
    {
        public const float FixedDeltaTime = 1f / 60f;
        public const float DefaultSpeedMultiplier = 1f;
        public const bool PauseOnStart = false;

        public static TickTimeState CreateTickTimeDefault() => new TickTimeState
        {
            FixedDeltaTime = FixedDeltaTime,
            CurrentSpeedMultiplier = DefaultSpeedMultiplier,
            Tick = 0,
            TargetTick = 0,
            IsPaused = PauseOnStart,
            IsPlaying = !PauseOnStart
        };

        public static TimeSettingsConfig CreateDefault() => new TimeSettingsConfig
        {
            FixedDeltaTime = FixedDeltaTime,
            DefaultSpeedMultiplier = DefaultSpeedMultiplier,
            PauseOnStart = PauseOnStart
        };
    }

    public enum RewindMode : byte
    {
        Record = 0,
        Playback = 1,
        CatchUp = 2
    }

    /// <summary>
    /// Tracks the current rewind / playback state for routing simulation groups.
    /// </summary>
    public struct RewindState : IComponentData
    {
        public RewindMode Mode;
        public uint StartTick;
        public uint TargetTick;
        public uint PlaybackTick;
        public float PlaybackTicksPerSecond;
        public sbyte ScrubDirection;
        public float ScrubSpeedMultiplier;
    }
}
