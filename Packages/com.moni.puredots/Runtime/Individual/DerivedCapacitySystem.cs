using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Runtime.Components;

namespace PureDOTS.Runtime.Individual
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct DerivedCapacitySystem : ISystem
    {
        private const uint UpdateIntervalTicks = 30;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            if ((timeState.Tick % UpdateIntervalTicks) != 0)
            {
                return;
            }

            var anatomyLookup = SystemAPI.GetBufferLookup<AnatomyPart>(true);
            var conditionLookup = SystemAPI.GetBufferLookup<Condition>(true);

            foreach (var (capacities, entity) in SystemAPI.Query<RefRW<DerivedCapacities>>().WithEntityAccess())
            {
                var sight = 1f;
                var manipulation = 1f;
                var consciousness = 1f;
                var reactionTime = 1f;
                var boarding = 1f;

                if (conditionLookup.HasBuffer(entity))
                {
                    var conditions = conditionLookup[entity];
                    for (int i = 0; i < conditions.Length; i++)
                    {
                        var condition = conditions[i];
                        if ((condition.Flags & ConditionFlags.OneEyeMissing) != 0 ||
                            ((condition.Flags & ConditionFlags.Missing) != 0 &&
                             (condition.TargetPartId == AnatomyPartIds.EyeLeft ||
                              condition.TargetPartId == AnatomyPartIds.EyeRight)))
                        {
                            sight = math.max(0.6f, sight * 0.75f);
                            boarding = math.max(0.4f, boarding * 0.6f);
                        }
                    }
                }

                capacities.ValueRW.Sight = sight;
                capacities.ValueRW.Manipulation = manipulation;
                capacities.ValueRW.Consciousness = consciousness;
                capacities.ValueRW.ReactionTime = reactionTime;
                capacities.ValueRW.Boarding = boarding;
            }
        }
    }
}
