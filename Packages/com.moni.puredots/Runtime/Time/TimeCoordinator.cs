using Unity.Entities;

namespace PureDOTS.Runtime.Time
{
    /// <summary>
    /// Singleton that coordinates time scales across multiple ECS worlds.
    /// Allows independent time acceleration/freezing per world.
    /// </summary>
    public struct TimeCoordinator : IComponentData
    {
        /// <summary>
        /// Time scale for this world (1.0 = normal, 10.0 = 10x speed, 0.0 = frozen).
        /// </summary>
        public float TimeScale;

        /// <summary>
        /// World identifier (0-255).
        /// </summary>
        public byte WorldId;

        /// <summary>
        /// Whether this world's time is frozen.
        /// </summary>
        public bool IsFrozen;

        public TimeCoordinator(byte worldId, float timeScale = 1.0f)
        {
            WorldId = worldId;
            TimeScale = timeScale;
            IsFrozen = timeScale <= 0f;
        }
    }
}

