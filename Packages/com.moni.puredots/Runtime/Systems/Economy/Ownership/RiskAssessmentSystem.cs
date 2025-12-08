using Unity.Burst;
using Unity.Entities;
using PureDOTS.Runtime.Economy.Ownership;
using PureDOTS.Systems;

namespace PureDOTS.Systems.Economy.Ownership.Mind
{
    [BurstCompile]
    [UpdateInGroup(typeof(MindEconomySystemGroup))]
    public partial struct RiskAssessmentSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RewindState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var rewind = SystemAPI.GetSingleton<RewindState>();
            if (rewind.Mode != RewindMode.Record)
            {
                return;
            }

            foreach (var (ledger, portfolio) in SystemAPI.Query<RefRO<Ledger>, DynamicBuffer<Portfolio>>())
            {
                float exposure = 0f;
                for (int i = 0; i < portfolio.Length; i++)
                {
                    var entry = portfolio[i];
                    exposure += entry.ExpectedOutputValue * entry.OwnershipShare;
                }

                // Placeholder: exposure could drive risk metrics (not stored yet).
                _ = exposure;
            }
        }
    }
}
