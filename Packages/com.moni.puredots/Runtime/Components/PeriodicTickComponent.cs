using Unity.Entities;

namespace PureDOTS.Runtime.Components
{
    /// <summary>
    /// Component for systems that update periodically (every N ticks) instead of every tick.
    /// Reduces CPU cost by skipping unchanged entities.
    /// </summary>
    public struct PeriodicTickComponent : IComponentData
    {
        /// <summary>Update stride (update every N ticks).</summary>
        public uint UpdateStride;
        
        /// <summary>Last tick when this entity was updated.</summary>
        public uint LastUpdateTick;
        
        /// <summary>Current tick counter (incremented each tick, reset when update occurs).</summary>
        public uint TickCounter;
    }

    /// <summary>
    /// Helper for periodic tick updates.
    /// </summary>
    public static class PeriodicTickHelper
    {
        /// <summary>
        /// Checks if entity should be updated this tick.
        /// </summary>
        public static bool ShouldUpdate(uint currentTick, ref PeriodicTickComponent periodic)
        {
            if (periodic.UpdateStride == 0)
            {
                periodic.UpdateStride = 1; // Default to every tick
            }

            periodic.TickCounter++;
            if (periodic.TickCounter >= periodic.UpdateStride)
            {
                periodic.TickCounter = 0;
                periodic.LastUpdateTick = currentTick;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Initializes periodic tick component with stride.
        /// </summary>
        public static PeriodicTickComponent Create(uint stride)
        {
            return new PeriodicTickComponent
            {
                UpdateStride = stride,
                LastUpdateTick = 0,
                TickCounter = 0
            };
        }
    }
}

