using Unity.Burst;
using Unity.Entities;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Dynasty;

namespace PureDOTS.Systems.Dynasty
{
    /// <summary>
    /// Ensures dynasty policy components exist for all dynasties.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct DynastyPolicyBootstrapSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var em = state.EntityManager;
            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);

            foreach (var (_, entity) in SystemAPI.Query<RefRO<DynastyIdentity>>().WithEntityAccess())
            {
                if (!em.HasComponent<DynastySuccessionPreferences>(entity))
                {
                    ecb.AddComponent(entity, new DynastySuccessionPreferences
                    {
                        LineageWeight = 0.4f,
                        WarlikeWeight = 0.2f,
                        MaterialistWeight = 0.15f,
                        IntellectWeight = 0.1f,
                        DiplomacyWeight = 0.1f,
                        SpiritualWeight = 0.05f,
                        WealthWeight = 0.05f,
                        PrestigeWeight = 0.05f,
                        ClaimDisputeThreshold = 0.65f,
                        MeritTieBreak = 0.1f
                    });
                }

                if (!em.HasComponent<DynastyInheritancePolicy>(entity))
                {
                    ecb.AddComponent(entity, new DynastyInheritancePolicy
                    {
                        WealthTransferRate = 0.35f,
                        LegacyTransferRate = 0.5f,
                        InheritanceTaxRate = 0.15f,
                        RequiresAcceptance = 0
                    });
                }

                if (!em.HasComponent<DynastyInheritanceState>(entity))
                {
                    ecb.AddComponent(entity, new DynastyInheritanceState
                    {
                        LastProcessedTick = 0
                    });
                }
            }

            ecb.Playback(em);
        }
    }
}
