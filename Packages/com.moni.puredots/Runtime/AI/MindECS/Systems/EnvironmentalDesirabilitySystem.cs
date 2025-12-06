using PureDOTS.AI.MindECS;
using PureDOTS.Environment;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Time;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.AI.MindECS.Systems
{
    /// <summary>
    /// Environmental desirability system.
    /// Weights goals by environmental preferences (comfort vs. fear).
    /// </summary>
    [BurstCompile]
    public partial struct EnvironmentalDesirabilitySystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            var rewindState = SystemAPI.GetSingleton<RewindState>();

            if (rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            if (timeState.IsPaused)
            {
                return;
            }

            // Query entities with environmental preferences and samples
            var entityQuery = SystemAPI.QueryBuilder()
                .WithAll<EnvironmentalPreference, EnvironmentSample>()
                .Build();

            if (entityQuery.IsEmpty)
            {
                return;
            }

            // Update desirability based on environmental match
            // This would integrate with goal evaluation systems
            // For now, this is a placeholder structure
        }
    }
}

