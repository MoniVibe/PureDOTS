using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Components.Orbital;
using PureDOTS.Runtime.Core;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Systems.Orbital
{
    /// <summary>
    /// Orbital time-step budgeting system.
    /// Coordinates CPU budgets across different orbital update domains:
    /// - GalacticFrameSystem: 0.001 Hz, < 0.1 ms budget
    /// - StellarOrbitSystem: 0.01 Hz, < 0.5 ms budget
    /// - Planetary6DoFSystem: 1 Hz, < 2 ms budget
    /// - Local6DoFSystem: 60 Hz, < 3 ms budget
    /// Burst job scheduler respects budgets by limiting job batches per domain.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(FrameHierarchySystem))]
    public partial struct OrbitalBudgetSystem : ISystem
    {
        /// <summary>Budget configuration for orbital systems.</summary>
        public struct OrbitalBudgetConfig : IComponentData
        {
            public float GalacticFrameBudgetMs;    // 0.1 ms
            public float StellarOrbitBudgetMs;      // 0.5 ms
            public float Planetary6DoFBudgetMs;    // 2.0 ms
            public float Local6DoFBudgetMs;         // 3.0 ms

            public float GalacticFrameFrequency;     // 0.001 Hz
            public float StellarOrbitFrequency;      // 0.01 Hz
            public float Planetary6DoFFrequency;    // 1.0 Hz
            public float Local6DoFFrequency;        // 60.0 Hz
        }

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TickTimeState>();
            state.RequireForUpdate<RewindState>();

            // Initialize budget config if it doesn't exist
            if (!SystemAPI.HasSingleton<OrbitalBudgetConfig>())
            {
                var config = new OrbitalBudgetConfig
                {
                    GalacticFrameBudgetMs = 0.1f,
                    StellarOrbitBudgetMs = 0.5f,
                    Planetary6DoFBudgetMs = 2.0f,
                    Local6DoFBudgetMs = 3.0f,
                    GalacticFrameFrequency = 0.001f,
                    StellarOrbitFrequency = 0.01f,
                    Planetary6DoFFrequency = 1.0f,
                    Local6DoFFrequency = 60.0f
                };
                SystemAPI.SetSingleton(config);
            }
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var rewindState = SystemAPI.GetSingleton<RewindState>();
            if (rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            // Budget system coordinates timing - actual budget enforcement
            // would be done by individual systems checking their allocated time
            // This is a coordination system that tracks budgets
        }

        /// <summary>
        /// Checks if a system domain should update based on frequency and budget.
        /// </summary>
        public static bool ShouldUpdateDomain(
            uint currentTick,
            float frequency,
            uint lastUpdateTick,
            float budgetMs,
            float actualCostMs)
        {
            // Check frequency
            float ticksPerUpdate = 60.0f / frequency; // Assuming 60 Hz base
            if (currentTick - lastUpdateTick < (uint)ticksPerUpdate)
            {
                return false;
            }

            // Check budget
            if (actualCostMs > budgetMs)
            {
                // Over budget - skip this update
                return false;
            }

            return true;
        }
    }
}

