using PureDOTS.Runtime.Components;
using PureDOTS.Systems;
using Space4X.Runtime;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Space4X.Systems
{
    /// <summary>
    /// Maintains crew aggregate data (membership, averages, duty state) using the shared AggregateEntity contract.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(VillagerNeedsSystem))]
    [UpdateBefore(typeof(AggregateAggregationSystem))]
    public partial struct Space4XCrewAggregationSystem : ISystem
    {
        private ComponentLookup<VillagerDisciplineState> _disciplineLookup;
        private ComponentLookup<AggregateEntity> _aggregateLookup;
        private EntityQuery _crewAggregateQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _disciplineLookup = state.GetComponentLookup<VillagerDisciplineState>(true);
            _aggregateLookup = state.GetComponentLookup<AggregateEntity>(true);
            state.RequireForUpdate<Space4XCrewAssignment>();
            state.RequireForUpdate<AggregateEntity>();
            state.RequireForUpdate<Space4XCrewAggregateData>();

            _crewAggregateQuery = state.GetEntityQuery(
                ComponentType.ReadWrite<AggregateEntity>(),
                ComponentType.ReadWrite<Space4XCrewAggregateData>(),
                ComponentType.ReadWrite<AggregateMember>());
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var allocator = state.WorldUpdateAllocator;
            var crewCount = math.max(1, _crewAggregateQuery.CalculateEntityCount());

            var accumulators = new NativeParallelHashMap<Entity, CrewAccumulator>(crewCount, allocator);
            var membership = new NativeParallelMultiHashMap<Entity, AggregateMember>(crewCount * 4, allocator);

            _disciplineLookup.Update(ref state);
            _aggregateLookup.Update(ref state);

            foreach (var (assignment, needs, entity) in SystemAPI
                         .Query<RefRO<Space4XCrewAssignment>, RefRO<VillagerNeeds>>()
                         .WithEntityAccess())
            {
                var crewEntity = assignment.ValueRO.CrewAggregate;
                if (crewEntity == Entity.Null)
                {
                    continue;
                }

                if (!_aggregateLookup.HasComponent(crewEntity))
                {
                    continue;
                }

                var aggregate = _aggregateLookup[crewEntity];
                if (aggregate.Category != AggregateCategory.Crew)
                {
                    continue;
                }

                if (!accumulators.TryGetValue(crewEntity, out var acc))
                {
                    acc = default;
                }

                acc.MemberCount++;
                acc.MoraleSum += needs.ValueRO.MoraleFloat;
                acc.EnergySum += needs.ValueRO.EnergyFloat;

                if (_disciplineLookup.HasComponent(entity))
                {
                    acc.DisciplineLevelSum += _disciplineLookup[entity].Level;
                }

                acc.StressSum += needs.ValueRO.Health <= 0f ? 1f : 0f;

                switch (assignment.ValueRO.Duty)
                {
                    case Space4XCrewDuty.Docked:
                        acc.DockedCount++;
                        break;
                    case Space4XCrewDuty.Sortie:
                        acc.SortieCount++;
                        break;
                    case Space4XCrewDuty.Combat:
                        acc.CombatCount++;
                        break;
                    case Space4XCrewDuty.Transfer:
                        acc.TransferCount++;
                        break;
                    default:
                        acc.IdleCount++;
                        break;
                }

                if (assignment.ValueRO.CurrentCraft != Entity.Null)
                {
                    if (acc.CurrentCraft == Entity.Null)
                    {
                        acc.CurrentCraft = assignment.ValueRO.CurrentCraft;
                    }
                    else if (acc.CurrentCraft != assignment.ValueRO.CurrentCraft)
                    {
                        acc.CurrentCraft = Entity.Null;
                    }
                }

                if (assignment.ValueRO.HomeCarrier != Entity.Null)
                {
                    if (acc.HomeCarrier == Entity.Null)
                    {
                        acc.HomeCarrier = assignment.ValueRO.HomeCarrier;
                    }
                    else if (acc.HomeCarrier != assignment.ValueRO.HomeCarrier)
                    {
                        acc.HomeCarrier = Entity.Null;
                    }
                }

                accumulators[crewEntity] = acc;
                membership.Add(crewEntity, new AggregateMember { Member = entity, Weight = 1f });
            }

            var memberIterator = default(NativeParallelMultiHashMapIterator<Entity>);

            foreach (var (aggregateRef, crewDataRef, buffer, entity) in SystemAPI
                         .Query<RefRW<AggregateEntity>, RefRW<Space4XCrewAggregateData>, DynamicBuffer<AggregateMember>>()
                         .WithEntityAccess())
            {
                ref var aggregate = ref aggregateRef.ValueRW;
                ref var crewData = ref crewDataRef.ValueRW;

                if (aggregate.Category != AggregateCategory.Crew)
                {
                    continue;
                }

                buffer.Clear();

                if (!accumulators.TryGetValue(entity, out var acc) || acc.MemberCount == 0)
                {
                    aggregate.MemberCount = 0;
                    aggregate.Morale = 0f;
                    aggregate.Cohesion = 0f;
                    aggregate.Stress = 0f;

                    crewData.AverageEnergy = 0f;
                    crewData.AverageDisciplineLevel = 0f;
                    crewData.Duty = Space4XCrewDuty.Idle;
                    crewData.CurrentCraft = Entity.Null;
                    continue;
                }

                aggregate.MemberCount = acc.MemberCount;
                aggregate.Morale = acc.MoraleSum / acc.MemberCount;
                aggregate.Cohesion = math.saturate(acc.MemberCount > 0 ? 1f - (acc.StressSum / acc.MemberCount) : aggregate.Cohesion);
                aggregate.Stress = acc.StressSum / acc.MemberCount;

                crewData.AverageEnergy = acc.EnergySum / acc.MemberCount;
                crewData.AverageDisciplineLevel = acc.DisciplineLevelSum > 0
                    ? acc.DisciplineLevelSum / acc.MemberCount
                    : 0f;
                crewData.Duty = acc.ResolveDuty();
                crewData.CurrentCraft = acc.CurrentCraft == Entity.Null ? crewData.CurrentCraft : acc.CurrentCraft;
                if (acc.HomeCarrier != Entity.Null)
                {
                    crewData.HomeCarrier = acc.HomeCarrier;
                }

                if (membership.TryGetFirstValue(entity, out var member, out memberIterator))
                {
                    do
                    {
                        buffer.Add(member);
                    } while (membership.TryGetNextValue(out member, ref memberIterator));
                }
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        private struct CrewAccumulator
        {
            public int MemberCount;
            public float MoraleSum;
            public float EnergySum;
            public float DisciplineLevelSum;
            public float StressSum;
            public int IdleCount;
            public int DockedCount;
            public int SortieCount;
            public int CombatCount;
            public int TransferCount;
            public Entity CurrentCraft;
            public Entity HomeCarrier;

            public Space4XCrewDuty ResolveDuty()
            {
                var duty = Space4XCrewDuty.Idle;
                var maxCount = IdleCount;

                if (DockedCount > maxCount)
                {
                    maxCount = DockedCount;
                    duty = Space4XCrewDuty.Docked;
                }

                if (SortieCount > maxCount)
                {
                    maxCount = SortieCount;
                    duty = Space4XCrewDuty.Sortie;
                }

                if (CombatCount > maxCount)
                {
                    maxCount = CombatCount;
                    duty = Space4XCrewDuty.Combat;
                }

                if (TransferCount > maxCount)
                {
                    duty = Space4XCrewDuty.Transfer;
                }

                return duty;
            }
        }
    }
}
