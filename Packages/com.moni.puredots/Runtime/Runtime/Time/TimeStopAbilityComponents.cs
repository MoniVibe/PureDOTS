using Unity.Entities;

namespace PureDOTS.Runtime.Components
{
    public enum TimeStopMode : byte
    {
        Stop = 0,
        Slow = 1
    }

    /// <summary>
    /// Request a global time stop/slow. Consumed by TimeStopSystem.
    /// </summary>
    public struct TimeStopRequest : IComponentData
    {
        public Entity Source;
        public float DurationSeconds;
        public float TimeScale;
        public TimeStopMode Mode;
    }

    /// <summary>
    /// Singleton state tracking active time stop.
    /// </summary>
    public struct TimeStopState : IComponentData
    {
        public Entity Source;
        public float RemainingSeconds;
        public float ActiveTimeScale;
        public byte IsActive;
        public byte PauseApplied;
        public byte OverrideApplied;
        public byte PreviousPaused;
        public byte PreviousOverride;
        public float PreviousTimeScaleOverride;
    }
}
