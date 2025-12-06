using PureDOTS.Environment;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Miracles;
using PureDOTS.Runtime.Time;
using PureDOTS.Systems;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Systems.Miracles
{
    /// <summary>
    /// Converts miracle effects into terraforming events that modify environment fields.
    /// Links miracles to climate/biome changes (rain → moisture, heat → temperature, etc.).
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(MiracleEffectSystemGroup))]
    [UpdateAfter(typeof(MiracleActivationSystem))]
    public partial struct MiracleEnvironmentSystem : ISystem
    {
        private TimeAwareController _timeAware;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();

            _timeAware = new TimeAwareController(
                TimeAwareExecutionPhase.Record | TimeAwareExecutionPhase.CatchUp,
                TimeAwareExecutionOptions.SkipWhenPaused);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            var rewindState = SystemAPI.GetSingleton<RewindState>();

            if (!_timeAware.TryBegin(timeState, rewindState, out _))
            {
                return;
            }

            // Find terraforming event buffer (singleton or per-miracle)
            Entity terraformingEntity;
            if (!SystemAPI.TryGetSingletonEntity<TerraformingEvent>(out terraformingEntity))
            {
                // Create singleton entity for terraforming events if it doesn't exist
                terraformingEntity = state.EntityManager.CreateEntity();
                state.EntityManager.AddBuffer<TerraformingEvent>(terraformingEntity);
            }

            var terraformingEvents = SystemAPI.GetBuffer<TerraformingEvent>(terraformingEntity);

            // Process active miracle effects
            foreach (var (effect, transform, entity) in SystemAPI.Query<RefRO<MiracleEffect>, RefRO<Unity.Transforms.LocalTransform>>().WithEntityAccess())
            {
                var miracleEffect = effect.ValueRO;
                var position = transform.ValueRO.Position;

                // Convert miracle effect to terraforming event based on effect type
                // This is a simplified mapping - extend based on actual miracle types
                TerraformingEvent? terraformingEvent = ConvertMiracleToTerraforming(miracleEffect, position, timeState.Tick);

                if (terraformingEvent.HasValue)
                {
                    terraformingEvents.Add(terraformingEvent.Value);
                }
            }
        }

        private static TerraformingEvent? ConvertMiracleToTerraforming(MiracleEffect miracle, float3 position, uint tick)
        {
            // Map miracle types to terraforming effects
            // This is a placeholder - actual mapping depends on miracle catalog definitions

            // Example: Rain miracle → moisture increase
            // Example: Heat miracle → temperature increase
            // Example: Light miracle → light increase

            // For now, return null - this will be extended based on actual miracle catalog
            // The miracle catalog should define environment deltas per miracle type

            return null;
        }
    }
}

