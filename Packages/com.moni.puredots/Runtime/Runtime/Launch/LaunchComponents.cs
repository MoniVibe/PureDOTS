using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Launch
{
    /// <summary>
    /// State of a launch queue entry.
    /// </summary>
    public enum LaunchEntryState : byte
    {
        /// <summary>Waiting to be launched at scheduled tick.</summary>
        Pending = 0,
        /// <summary>Launch executed, velocity applied.</summary>
        Launched = 1,
        /// <summary>Entry processed and can be removed.</summary>
        Consumed = 2
    }

    /// <summary>
    /// Request to launch a payload from a launcher.
    /// Written by game adapters, consumed by LaunchRequestIntakeSystem.
    /// </summary>
    [InternalBufferCapacity(4)]
    public struct LaunchRequest : IBufferElementData
    {
        /// <summary>The launcher entity making the request.</summary>
        public Entity SourceEntity;

        /// <summary>The payload entity to launch.</summary>
        public Entity PayloadEntity;

        /// <summary>Tick at which to launch (0 = next available tick).</summary>
        public uint LaunchTick;

        /// <summary>Initial velocity to apply to payload.</summary>
        public float3 InitialVelocity;

        /// <summary>Optional flags (reserved for future use).</summary>
        public byte Flags;
    }

    /// <summary>
    /// Entry in a launcher's queue of pending launches.
    /// Managed by PureDOTS launch systems.
    /// </summary>
    [InternalBufferCapacity(8)]
    public struct LaunchQueueEntry : IBufferElementData
    {
        /// <summary>The payload entity to launch.</summary>
        public Entity PayloadEntity;

        /// <summary>Tick at which to execute the launch.</summary>
        public uint ScheduledTick;

        /// <summary>Initial velocity to apply.</summary>
        public float3 InitialVelocity;

        /// <summary>Current state of this entry.</summary>
        public LaunchEntryState State;
    }

    /// <summary>
    /// Configuration for a launcher entity.
    /// Set at bake time via authoring.
    /// </summary>
    public struct LauncherConfig : IComponentData
    {
        /// <summary>Maximum number of pending launches in queue.</summary>
        public byte MaxQueueSize;

        /// <summary>Minimum ticks between launches (cooldown).</summary>
        public uint CooldownTicks;

        /// <summary>Default launch speed if not specified in request.</summary>
        public float DefaultSpeed;

        /// <summary>Creates default launcher config.</summary>
        public static LauncherConfig CreateDefault()
        {
            return new LauncherConfig
            {
                MaxQueueSize = 8,
                CooldownTicks = 10, // ~0.17s at 60 ticks/sec
                DefaultSpeed = 10f
            };
        }
    }

    /// <summary>
    /// Runtime state for a launcher entity.
    /// Managed by PureDOTS launch systems.
    /// </summary>
    public struct LauncherState : IComponentData
    {
        /// <summary>Tick when last launch occurred.</summary>
        public uint LastLaunchTick;

        /// <summary>Number of entries currently in queue.</summary>
        public byte QueueCount;

        /// <summary>Version counter for change detection.</summary>
        public uint Version;
    }

    /// <summary>
    /// Tag component marking an entity as a launcher.
    /// </summary>
    public struct LauncherTag : IComponentData { }

    /// <summary>
    /// Tag component marking an entity as a launched projectile.
    /// Added when payload is launched, can be used for collision filtering.
    /// </summary>
    public struct LaunchedProjectileTag : IComponentData
    {
        /// <summary>Tick when launched.</summary>
        public uint LaunchTick;

        /// <summary>Source launcher entity.</summary>
        public Entity SourceLauncher;
    }
}






