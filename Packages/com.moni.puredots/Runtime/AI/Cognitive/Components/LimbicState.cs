using Unity.Entities;

namespace PureDOTS.Runtime.AI.Cognitive
{
    /// <summary>
    /// Limbic state component storing emotion and motivation variables.
    /// Modulates cognitive planning and exploration behavior.
    /// </summary>
    public struct LimbicState : IComponentData
    {
        /// <summary>
        /// Curiosity level (0.0 to 1.0).
        /// Higher values increase exploration probability.
        /// </summary>
        public float Curiosity;

        /// <summary>
        /// Fear level (0.0 to 1.0).
        /// Higher values cause avoidance of high-failure contexts.
        /// </summary>
        public float Fear;

        /// <summary>
        /// Frustration level (0.0 to 1.0).
        /// Higher values trigger help-seeking or aggression behaviors.
        /// </summary>
        public float Frustration;

        /// <summary>
        /// Recent success rate (0.0 to 1.0) over last N actions.
        /// </summary>
        public float RecentSuccessRate;

        /// <summary>
        /// Count of recent failures.
        /// </summary>
        public int RecentFailures;

        /// <summary>
        /// Last tick when emotion state was updated.
        /// </summary>
        public uint LastEmotionUpdateTick;

        /// <summary>
        /// Threshold for considering success rate "stable" (for curiosity decay).
        /// </summary>
        public float StabilityThreshold;

        /// <summary>
        /// Number of actions considered for recent success rate calculation.
        /// </summary>
        public byte RecentActionWindow;
    }
}

