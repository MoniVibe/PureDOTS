using Unity.Entities;

namespace PureDOTS.Runtime
{
    /// <summary>
    /// Demo scenario modes for switching between different test/demo configurations.
    /// Used to gate systems and control what's active in showcase scenes.
    /// </summary>
    public enum DemoScenario : byte
    {
        /// <summary>
        /// All systems active (default showcase mode).
        /// </summary>
        AllSystemsShowcase = 0,

        /// <summary>
        /// Only Space4X physics, mining, and hand systems active.
        /// </summary>
        Space4XPhysicsOnly = 1,

        /// <summary>
        /// Only Godgame physics and resource systems active.
        /// </summary>
        GodgamePhysicsOnly = 2,

        /// <summary>
        /// Hand throw sandbox - minimal systems, focused on grab/throw testing.
        /// </summary>
        HandThrowSandbox = 3
    }

    /// <summary>
    /// Boot phase for demo scenario spawning.
    /// Used to spread spawning across multiple frames.
    /// </summary>
    public enum DemoBootPhase : byte
    {
        None = 0,
        SpawnGodgame = 1,
        SpawnSpace4x = 2,
        Done = 3
    }

    /// <summary>
    /// Singleton component tracking the current demo scenario mode.
    /// Systems should check this to determine if they should run.
    /// </summary>
    public struct DemoScenarioState : IComponentData
    {
        /// <summary>
        /// Current active scenario mode.
        /// </summary>
        public DemoScenario Current;

        /// <summary>
        /// Whether the demo scenario has been initialized (entities spawned).
        /// </summary>
        public bool IsInitialized;

        /// <summary>
        /// Current boot phase for phased spawning.
        /// </summary>
        public DemoBootPhase BootPhase;

        /// <summary>
        /// Enable Godgame slice (villages, villagers, terrain).
        /// </summary>
        public bool EnableGodgame;

        /// <summary>
        /// Enable Space4X slice (carriers, miners, asteroids).
        /// </summary>
        public bool EnableSpace4x;
    }
}




