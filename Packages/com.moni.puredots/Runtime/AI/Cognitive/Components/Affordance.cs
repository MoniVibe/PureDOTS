using Unity.Entities;

namespace PureDOTS.Runtime.AI.Cognitive
{
    /// <summary>
    /// Affordance type enum for object interaction capabilities.
    /// </summary>
    public enum AffordanceType : byte
    {
        None = 0,
        Climbable = 1,
        Movable = 2,
        Throwable = 3,
        Usable = 4,
        Grabable = 5,
        Pushable = 6,
        Pullable = 7
    }

    /// <summary>
    /// Affordance component attached to objects that agents can interact with.
    /// Describes what actions are possible and their cost/benefit.
    /// </summary>
    public struct Affordance : IComponentData
    {
        /// <summary>
        /// Type of affordance (climbable, movable, etc.).
        /// </summary>
        public AffordanceType Type;

        /// <summary>
        /// Effort required to perform action (0.0 = easy, 1.0 = very difficult).
        /// </summary>
        public float Effort;

        /// <summary>
        /// Potential reward from performing action (0.0 = none, 1.0 = high reward).
        /// </summary>
        public float RewardPotential;

        /// <summary>
        /// Entity that owns this affordance (the object itself).
        /// </summary>
        public Entity ObjectEntity;
    }

    /// <summary>
    /// Buffer storing detected affordances for an agent.
    /// Populated by AffordanceDetectionSystem.
    /// </summary>
    public struct DetectedAffordance : Unity.Entities.IBufferElementData
    {
        /// <summary>
        /// Entity with the affordance.
        /// </summary>
        public Entity ObjectEntity;

        /// <summary>
        /// Affordance type.
        /// </summary>
        public AffordanceType Type;

        /// <summary>
        /// Computed utility score (RewardPotential / Effort).
        /// </summary>
        public float UtilityScore;

        /// <summary>
        /// Distance squared to the object.
        /// </summary>
        public float DistanceSq;
    }
}

