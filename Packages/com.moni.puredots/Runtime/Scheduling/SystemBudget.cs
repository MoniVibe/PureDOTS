using Unity.Entities;

namespace PureDOTS.Runtime.Scheduling
{
    /// <summary>
    /// Performance budget for a system, used by the job graph scheduler.
    /// </summary>
    public struct SystemBudget : IComponentData
    {
        /// <summary>
        /// Estimated cost in milliseconds per execution.
        /// </summary>
        public float CostMs;

        /// <summary>
        /// Priority level (0-255). Higher values indicate higher priority.
        /// </summary>
        public byte Priority;

        /// <summary>
        /// Maximum allowed execution time in milliseconds before throttling.
        /// </summary>
        public float MaxMs;

        public SystemBudget(float costMs, byte priority, float maxMs = float.MaxValue)
        {
            CostMs = costMs;
            Priority = priority;
            MaxMs = maxMs;
        }
    }
}

