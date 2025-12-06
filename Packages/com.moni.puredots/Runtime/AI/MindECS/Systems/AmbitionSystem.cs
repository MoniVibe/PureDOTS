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
    /// Ambition system evaluating "expand farm" / "seek shade" / "migrate" goals.
    /// Ties through GoalProfile and BehaviorProfile in Mind ECS.
    /// </summary>
    [BurstCompile]
    public partial struct AmbitionSystem : ISystem
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

            // Query entities with ambition goals and environmental samples
            var entityQuery = SystemAPI.QueryBuilder()
                .WithAll<AmbitionGoal, EnvironmentSample, EnvironmentalPreference>()
                .Build();

            if (entityQuery.IsEmpty)
            {
                return;
            }

            // Evaluate ambitions based on environmental conditions
            // "expand farm" - when soil fertility and moisture are favorable
            // "seek shade" - when light is too high and temperature is uncomfortable
            // "migrate" - when environmental conditions are unfavorable
            // This would integrate with Mind ECS goal evaluation
            // For now, this is a placeholder structure
        }
    }
}

