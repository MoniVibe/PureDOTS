using Unity.Entities;
using PureDOTS.Runtime.Components;

namespace PureDOTS.Systems
{
    /// <summary>
    /// System group for rewind simulation.
    /// Runs systems in reverse dependency order during rewind ticks.
    /// Burst executes restore jobs as memcpy kernels (Δ → state).
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(TimeSystemGroup))]
    public partial class RewindSimulationSystemGroup : ComponentSystemGroup
    {
        /// <summary>
        /// Whether we're currently in rewind mode.
        /// </summary>
        public bool IsRewinding { get; private set; }

        protected override void OnCreate()
        {
            base.OnCreate();
            IsRewinding = false;
        }

        protected override void OnUpdate()
        {
            // Check if we're in rewind mode
            if (SystemAPI.HasSingleton<RewindState>())
            {
                var rewindState = SystemAPI.GetSingleton<RewindState>();
                IsRewinding = rewindState.Mode == RewindMode.Playback || rewindState.Mode == RewindMode.CatchUp;
            }

            if (IsRewinding)
            {
                // During rewind, run systems in reverse dependency order
                UpdateReverseOrder();
            }
            else
            {
                // Normal forward execution
                base.OnUpdate();
            }
        }

        /// <summary>
        /// Update systems in reverse dependency order for rewind.
        /// </summary>
        private void UpdateReverseOrder()
        {
            // Get all systems in this group
            var systems = Systems;

            // Reverse the order for rewind
            for (int i = systems.Count - 1; i >= 0; i--)
            {
                var system = systems[i];
                if (system.Enabled)
                {
                    system.Update(this);
                }
            }
        }
    }
}

