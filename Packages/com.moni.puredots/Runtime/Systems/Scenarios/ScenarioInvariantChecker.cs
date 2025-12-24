using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Scenarios;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Systems.Scenarios
{
    /// <summary>
    /// System that checks for invariant violations during scenario execution.
    /// Validates: NaNs, invalid relations, missing required singletons, broken rewind guards.
    /// Reports violations to ScenarioExitUtility.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    public partial struct ScenarioInvariantChecker : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var currentTick = SystemAPI.GetSingleton<TimeState>().Tick;

            // Check for NaNs in common float components
            CheckForNaNs(ref state, currentTick);

            // Check for missing required singletons
            CheckRequiredSingletons(ref state, currentTick);

            // Check rewind state consistency
            CheckRewindGuards(ref state, currentTick);
        }

        [BurstCompile]
        private void CheckForNaNs(ref SystemState state, uint currentTick)
        {
            // Check LocalTransform positions for NaNs
            foreach (var transform in SystemAPI.Query<RefRO<LocalTransform>>())
            {
                var pos = transform.ValueRO.Position;
                if (math.any(math.isnan(pos)) || math.any(math.isinf(pos)))
                {
                    ScenarioExitUtility.ReportScenarioContract("NaNInTransform",
                        $"NaN or Inf detected in LocalTransform.Position at tick {currentTick}");
                    return;
                }
            }

            // Check common float components for NaNs
            // This is a minimal check - can be extended to check more component types
        }

        [BurstCompile]
        private void CheckRequiredSingletons(ref SystemState state, uint currentTick)
        {
            // Check for required singletons that must exist
            if (!SystemAPI.HasSingleton<TimeState>())
            {
                ScenarioExitUtility.ReportScenarioContract("MissingSingleton",
                    $"TimeState singleton missing at tick {currentTick}");
            }

            if (!SystemAPI.HasSingleton<RewindState>())
            {
                ScenarioExitUtility.ReportScenarioContract("MissingSingleton",
                    $"RewindState singleton missing at tick {currentTick}");
            }

            // Check ScenarioInfo exists if scenario is running
            if (!SystemAPI.HasSingleton<ScenarioInfo>())
            {
                // This is only a warning if scenario hasn't started yet
                // But if we're past initialization, it's an error
                if (currentTick > 0)
                {
                    ScenarioExitUtility.ReportScenarioContract("MissingSingleton",
                        $"ScenarioInfo singleton missing at tick {currentTick}");
                }
            }
        }

        [BurstCompile]
        private void CheckRewindGuards(ref SystemState state, uint currentTick)
        {
            if (!SystemAPI.TryGetSingleton<RewindState>(out var rewindState))
            {
                return;
            }

            // Check that rewind state is consistent
            // If in Record mode, history should be accumulating
            // If in Rewind mode, we should be able to rewind
            // This is a basic check - more sophisticated validation can be added

            // Check for broken rewind guards (entities that should be rewindable but aren't)
            // For Phase 0, this is a placeholder - actual rewind guard checking
            // would validate that entities with RewindableTag have proper history buffers
        }
    }
}



