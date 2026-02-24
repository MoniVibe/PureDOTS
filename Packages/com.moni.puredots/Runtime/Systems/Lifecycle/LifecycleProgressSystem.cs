using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Lifecycle;

namespace PureDOTS.Systems.Lifecycle
{
    /// <summary>
    /// Advances lifecycle progress and stage transitions based on age.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(EntityReproductionSystem))]
    public partial struct LifecycleProgressSystem : ISystem
    {
        private ComponentLookup<AgingEffects> _agingEffectsLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
            _agingEffectsLookup = state.GetComponentLookup<AgingEffects>(false);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton<RewindState>(out var rewindState) || rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            var timeState = SystemAPI.GetSingleton<TimeState>();
            if (timeState.IsPaused)
            {
                return;
            }

            _agingEffectsLookup.Update(ref state);

            uint currentTick = timeState.Tick;

            foreach (var (lifecycle, config, entity) in SystemAPI
                         .Query<RefRW<LifecycleState>, RefRO<LifecycleConfig>>()
                         .WithEntityAccess())
            {
                var stateValue = lifecycle.ValueRO;

                if (stateValue.IsFrozen != 0)
                {
                    continue;
                }

                stateValue.Type = config.ValueRO.Type;

                float progressDelta = math.max(0f, config.ValueRO.ProgressRate);
                stateValue.TotalAge += progressDelta;

                if (config.ValueRO.AdvanceTrigger == StageTrigger.Age)
                {
                    float stageDuration = LifecycleHelpers.GetStageDuration(stateValue.CurrentStage, config.ValueRO);
                    if (stageDuration > 0f)
                    {
                        stateValue.StageProgress = math.saturate(stateValue.StageProgress + progressDelta / stageDuration);
                    }
                    else
                    {
                        stateValue.StageProgress = 1f;
                    }

                    LifecycleHelpers.TryAdvanceStage(ref stateValue, config.ValueRO, currentTick, out _);
                }

                lifecycle.ValueRW = stateValue;

                if (_agingEffectsLookup.HasComponent(entity))
                {
                    _agingEffectsLookup[entity] = LifecycleHelpers.CalculateAgingEffects(stateValue, config.ValueRO);
                }
            }
        }
    }
}
