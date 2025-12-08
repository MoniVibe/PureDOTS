using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Rendering;
using PureDOTS.Runtime.Villagers;
using PureDOTS.Systems;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Runtime.Systems.Aggregates
{
    /// <summary>
    /// System that updates village aggregate summaries based on member villagers.
    /// Runs at configurable intervals to minimize overhead.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [BurstCompile]
    public partial struct VillageAggregateSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
            state.RequireForUpdate<VillageTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton<RewindState>(out var rewindState) || rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            var timeState = SystemAPI.GetSingleton<TimeState>();
            uint currentTick = timeState.Tick;

            // Update village aggregates
            foreach (var (villageState, renderSummary, aggregateState, aggregateSummary, members, entity) in
                SystemAPI.Query<RefRW<VillageState>, RefRW<VillageRenderSummary>,
                                RefRW<AggregateState>, RefRW<AggregateRenderSummary>,
                                DynamicBuffer<AggregateMemberElement>>()
                    .WithAll<VillageTag>()
                    .WithEntityAccess())
            {
                // Check update interval
                if (currentTick - villageState.ValueRO.LastUpdateTick < villageState.ValueRO.UpdateInterval)
                {
                    continue;
                }

                // Calculate aggregate statistics
                int population = 0;
                float3 totalPosition = float3.zero;
                float3 minBounds = new float3(float.MaxValue);
                float3 maxBounds = new float3(float.MinValue);
                float totalWealth = 0f;
                float totalMorale = 0f;
                float totalFaith = 0f;
                float totalHealth = 0f;

                for (int i = 0; i < members.Length; i++)
                {
                    var member = members[i];
                    if (member.MemberEntity == Entity.Null)
                    {
                        continue;
                    }

                    // Get member position
                    if (SystemAPI.HasComponent<LocalTransform>(member.MemberEntity))
                    {
                        var transform = SystemAPI.GetComponent<LocalTransform>(member.MemberEntity);
                        totalPosition += transform.Position;
                        minBounds = math.min(minBounds, transform.Position);
                        maxBounds = math.max(maxBounds, transform.Position);
                    }

                    // Get member needs
                    if (SystemAPI.HasComponent<VillagerNeeds>(member.MemberEntity))
                    {
                        var needs = SystemAPI.GetComponent<VillagerNeeds>(member.MemberEntity);
                        totalHealth += needs.Health;
                        totalMorale += needs.Morale;
                    }

                    // Get member belief
                    if (SystemAPI.HasComponent<VillagerBeliefOptimized>(member.MemberEntity))
                    {
                        var belief = SystemAPI.GetComponent<VillagerBeliefOptimized>(member.MemberEntity);
                        totalFaith += belief.FaithNormalized;
                    }

                    totalWealth += member.StrengthContribution;
                    population++;
                }

                if (population == 0)
                {
                    continue;
                }

                float3 avgPosition = totalPosition / population;
                float3 boundsCenter = (minBounds + maxBounds) / 2f;
                float boundsRadius = math.length(maxBounds - minBounds) / 2f;

                // Update village state
                villageState.ValueRW.PopulationCount = population;
                villageState.ValueRW.CenterPosition = avgPosition;
                villageState.ValueRW.BoundsMin = minBounds;
                villageState.ValueRW.BoundsMax = maxBounds;
                villageState.ValueRW.TotalWealth = totalWealth;
                villageState.ValueRW.AverageMorale = totalMorale / population;
                villageState.ValueRW.AverageFaith = totalFaith / population;
                villageState.ValueRW.LastUpdateTick = currentTick;

                // Update render summary
                renderSummary.ValueRW.PopulationCount = population;
                renderSummary.ValueRW.CenterPosition = avgPosition;
                renderSummary.ValueRW.BoundsCenter = boundsCenter;
                renderSummary.ValueRW.BoundsRadius = boundsRadius;
                renderSummary.ValueRW.TotalWealth = totalWealth;
                renderSummary.ValueRW.AverageMorale = totalMorale / population;
                renderSummary.ValueRW.AverageFaith = totalFaith / population;
                renderSummary.ValueRW.LastUpdateTick = currentTick;

                // Update generic aggregate state
                aggregateState.ValueRW.MemberCount = population;
                aggregateState.ValueRW.AveragePosition = avgPosition;
                aggregateState.ValueRW.BoundsMin = minBounds;
                aggregateState.ValueRW.BoundsMax = maxBounds;
                aggregateState.ValueRW.TotalHealth = totalHealth;
                aggregateState.ValueRW.AverageMorale = totalMorale / population;
                aggregateState.ValueRW.LastAggregationTick = currentTick;

                // Update generic render summary
                aggregateSummary.ValueRW.MemberCount = population;
                aggregateSummary.ValueRW.AveragePosition = avgPosition;
                aggregateSummary.ValueRW.BoundsCenter = boundsCenter;
                aggregateSummary.ValueRW.BoundsRadius = boundsRadius;
                aggregateSummary.ValueRW.TotalHealth = totalHealth;
                aggregateSummary.ValueRW.AverageMorale = totalMorale / population;
                aggregateSummary.ValueRW.LastUpdateTick = currentTick;
            }
        }
    }
}

