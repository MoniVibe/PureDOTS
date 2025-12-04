using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Identity
{
    /// <summary>
    /// Cultural drift system: aggregate alignment and outlook drift over time toward external influences
    /// (gods, empires, dominant forces) and member composition.
    /// </summary>
    [BurstCompile]
    public partial struct CulturalDriftSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<AggregateAlignment>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Run at low frequency (e.g., once per in-game day/week)
            // For now, this is a framework - games implement specific drift logic

            foreach (var (entity, aggregateAlignment, members) in SystemAPI.Query<RefRW<AggregateAlignment>, RefRO<AggregateMember>>()
                .WithEntityAccess())
            {
                if (members.ValueRO.Length == 0)
                    continue;

                // Calculate target alignment from members
                var targetAlignment = CalculateMemberAverageAlignment(ref state, members.ValueRO);

                // Drift toward target based on cohesion and drift rate
                var current = aggregateAlignment.ValueRO;
                float driftSpeed = current.DriftRate * (1f - current.Cohesion); // Lower cohesion = faster drift

                aggregateAlignment.ValueRW.Moral = math.lerp(current.Moral, targetAlignment.Moral, driftSpeed * 0.01f);
                aggregateAlignment.ValueRW.Order = math.lerp(current.Order, targetAlignment.Order, driftSpeed * 0.01f);
                aggregateAlignment.ValueRW.Purity = math.lerp(current.Purity, targetAlignment.Purity, driftSpeed * 0.01f);
            }
        }

        [BurstCompile]
        private static EntityAlignment CalculateMemberAverageAlignment(ref SystemState state, DynamicBuffer<AggregateMember> members)
        {
            float totalMoral = 0f;
            float totalOrder = 0f;
            float totalPurity = 0f;
            float totalWeight = 0f;

            foreach (var member in members)
            {
                if (!state.EntityManager.HasComponent<EntityAlignment>(member.MemberEntity))
                    continue;

                var alignment = state.EntityManager.GetComponentData<EntityAlignment>(member.MemberEntity);
                float weight = member.InfluenceWeight > 0f ? member.InfluenceWeight : 1f;

                totalMoral += alignment.Moral * weight;
                totalOrder += alignment.Order * weight;
                totalPurity += alignment.Purity * weight;
                totalWeight += weight;
            }

            if (totalWeight == 0f)
                return new EntityAlignment(); // Default neutral

            return new EntityAlignment
            {
                Moral = totalMoral / totalWeight,
                Order = totalOrder / totalWeight,
                Purity = totalPurity / totalWeight,
                Strength = 0.5f // Default
            };
        }
    }
}

