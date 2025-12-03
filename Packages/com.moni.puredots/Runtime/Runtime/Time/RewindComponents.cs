using Unity.Entities;

namespace PureDOTS.Runtime.Components
{
    /// <summary>
    /// Defines the current mode of the rewind system.
    /// </summary>
    public enum RewindMode : byte
    {
        /// <summary>Normal forward simulation, recording history.</summary>
        Record = 0,
        /// <summary>Playing back recorded history (rewinding).</summary>
        Playback = 1,
        /// <summary>Rapidly catching up from playback to current time.</summary>
        CatchUp = 2,
        /// <summary>System idle, no recording or playback active.</summary>
        Idle = 3
    }

    /// <summary>
    /// Direction of scrubbing through time.
    /// </summary>
    public enum ScrubDirection : byte
    {
        None = 0,
        Forward = 1,
        Backward = 2
    }

    /// <summary>
    /// Singleton component tracking the global rewind/playback state.
    /// The RewindCoordinatorSystem is the authoritative writer for this component.
    /// </summary>
    public struct RewindState : IComponentData
    {
        /// <summary>Current rewind mode (Record, Playback, CatchUp, Idle).</summary>
        public RewindMode Mode;
        /// <summary>Current simulation tick (may differ from TickTimeState during playback).</summary>
        public uint CurrentTick;
        /// <summary>Target tick for rewind operations.</summary>
        public uint TargetTick;
        /// <summary>Playback speed multiplier during rewind.</summary>
        public float PlaybackSpeed;
        /// <summary>Tick at which rewind was initiated.</summary>
        public uint StartTick;
        /// <summary>Current tick during playback iteration.</summary>
        public uint PlaybackTick;
        /// <summary>Rate of playback in ticks per second.</summary>
        public float PlaybackTicksPerSecond;
        /// <summary>Current scrub direction.</summary>
        public ScrubDirection ScrubDirection;
        /// <summary>Speed multiplier for scrubbing.</summary>
        public float ScrubSpeedMultiplier;
        /// <summary>Window of ticks available for rewind (based on history).</summary>
        public uint RewindWindowTicks;
    }

    /// <summary>
    /// Phase of the preview-based rewind system.
    /// Used for preview/scrub rewind where the world stays frozen while ghosts preview the rewind.
    /// </summary>
    public enum RewindPhase : byte
    {
        /// <summary>Normal time, no rewind preview active.</summary>
        Inactive = 0,
        /// <summary>Holding rewind, ghosts scrub backwards through time.</summary>
        ScrubbingPreview = 1,
        /// <summary>Released rewind key, ghosts paused at preview position, world frozen.</summary>
        FrozenPreview = 2,
        /// <summary>One-shot: apply rewind to world, then transition back to Inactive.</summary>
        CommitPlayback = 3
    }

    /// <summary>
    /// Singleton component tracking the preview-based rewind control state.
    /// Manages the preview/scrub rewind flow where the world stays frozen at PresentTickAtStart
    /// while ghosts preview different time positions via PreviewTick.
    /// The RewindControlSystem is the authoritative writer for this component.
    /// </summary>
    public struct RewindControlState : IComponentData
    {
        /// <summary>Current rewind phase (Inactive, ScrubbingPreview, FrozenPreview, CommitPlayback).</summary>
        public RewindPhase Phase;
        /// <summary>Global tick when preview started (current "present" at the moment rewind began).</summary>
        /// <remarks>This is the anchor point - if we cancel, we resume from here.</remarks>
        public int PresentTickAtStart;
        /// <summary>The tick ghosts are previewing (scrub position).</summary>
        /// <remarks>During ScrubbingPreview, this updates based on ScrubSpeed. During FrozenPreview, it stays fixed.</remarks>
        public int PreviewTick;
        /// <summary>Rewind speed multiplier (1-4x, can be float).</summary>
        /// <remarks>Controls how fast ghosts scrub through history during ScrubbingPreview.</remarks>
        public float ScrubSpeed;
    }
}
