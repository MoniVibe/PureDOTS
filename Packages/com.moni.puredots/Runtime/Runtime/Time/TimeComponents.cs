using System.Runtime.InteropServices;
using Unity.Entities;

namespace PureDOTS.Runtime.Components
{
    /// <summary>
    /// High-level time state singleton component.
    /// 
    /// DESIGN INVARIANT: There is exactly one TimeState singleton in a world.
    /// DESIGN INVARIANT: TimeState.Tick is monotonically increasing in real time and is the canonical "world time index".
    /// DESIGN INVARIANT: Rewind is always expressed as playback over history, NOT by decrementing Tick.
    /// DESIGN INVARIANT: All history and snapshots are keyed by Tick, not modified by rewind operations.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct TimeState : IComponentData
    {
        /// <summary>Current simulation tick (monotonically increasing, canonical world time index).</summary>
        public uint Tick;
        /// <summary>Frame delta time, scaled by CurrentSpeedMultiplier.</summary>
        public float DeltaTime;
        /// <summary>Elapsed time in simulation space (wall-clock time scaled by speed).</summary>
        public float ElapsedTime;
        /// <summary>Whether the simulation is currently paused.</summary>
        [MarshalAs(UnmanagedType.U1)]
        public bool IsPaused;
        /// <summary>Base fixed timestep (e.g., 1/60 seconds).</summary>
        public float FixedDeltaTime;
        /// <summary>Current speed multiplier (0.01-16.0, configured via ScriptableObjects).</summary>
        public float CurrentSpeedMultiplier;
    }

    /// <summary>
    /// Canonical tick time state singleton component.
    /// 
    /// DESIGN INVARIANT: There is exactly one TickTimeState singleton in a world.
    /// DESIGN INVARIANT: TickTimeState.Tick is monotonically increasing in real time and is the canonical tick source.
    /// DESIGN INVARIANT: Rewind operations do NOT decrement Tick; they use playback over history instead.
    /// DESIGN INVARIANT: All history and snapshots are keyed by Tick, providing an index over time.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct TickTimeState : IComponentData
    {
        /// <summary>Current simulation tick (monotonically increasing, canonical tick source).</summary>
        public uint Tick;
        /// <summary>Base fixed timestep (e.g., 1/60 seconds).</summary>
        public float FixedDeltaTime;
        /// <summary>Current speed multiplier (0.01-16.0, configured via ScriptableObjects).</summary>
        public float CurrentSpeedMultiplier;
        /// <summary>Target tick for catch-up operations.</summary>
        public uint TargetTick;
        /// <summary>Whether the simulation is currently paused.</summary>
        [MarshalAs(UnmanagedType.U1)]
        public bool IsPaused;
        /// <summary>Whether the simulation is currently playing (not paused).</summary>
        [MarshalAs(UnmanagedType.U1)]
        public bool IsPlaying;
    }
}
