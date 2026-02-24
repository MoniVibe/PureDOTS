using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Dynasty;
using PureDOTS.Runtime.Succession;

namespace PureDOTS.Systems.Dynasty
{
    /// <summary>
    /// Processes dynasty inheritance when succession resolves.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(DynastySuccessionSystem))]
    public partial struct DynastyInheritanceSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
        }

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

            var em = state.EntityManager;
            using var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (wealth, inheritanceState, policy, succession, entity) in SystemAPI
                         .Query<RefRW<DynastyWealth>, RefRW<DynastyInheritanceState>, RefRO<DynastyInheritancePolicy>, RefRO<SuccessionEvent>>()
                         .WithEntityAccess())
            {
                if (succession.ValueRO.WasSuccessful == 0 || succession.ValueRO.SuccessorEntity == Entity.Null)
                {
                    continue;
                }

                if (succession.ValueRO.ResolvedTick == 0 || succession.ValueRO.ResolvedTick <= inheritanceState.ValueRO.LastProcessedTick)
                {
                    continue;
                }

                var heir = succession.ValueRO.SuccessorEntity;
                if (!em.Exists(heir))
                {
                    continue;
                }

                if (!em.HasBuffer<InheritanceItem>(heir))
                {
                    ecb.AddBuffer<InheritanceItem>(heir);
                }

                float transferableWealth = math.max(0f, wealth.ValueRO.SharedWealth * policy.ValueRO.WealthTransferRate);
                float transferEfficiency = math.saturate(1f - policy.ValueRO.InheritanceTaxRate);
                float actualWealth = transferableWealth * transferEfficiency;

                if (transferableWealth > 0f)
                {
                    ecb.AppendToBuffer(heir, new InheritanceItem
                    {
                        ItemType = new FixedString32Bytes("dynasty_wealth"),
                        ItemEntity = entity,
                        Value = transferableWealth,
                        TransferEfficiency = transferEfficiency,
                        RequiresAcceptance = policy.ValueRO.RequiresAcceptance
                    });

                    wealth.ValueRW.SharedWealth = math.max(0f, wealth.ValueRO.SharedWealth - actualWealth);
                }

                inheritanceState.ValueRW.LastProcessedTick = succession.ValueRO.ResolvedTick;
            }

            ecb.Playback(em);
        }
    }
}
