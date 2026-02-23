using PureDOTS.Runtime.Components;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Economy.Policies
{
    /// <summary>
    /// Calculates economic stress (tax burden, price spikes, unemployment) and emits unrest events.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct UnrestSignalSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TickTimeState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton<ScenarioState>(out var scenario) ||
                !scenario.IsInitialized ||
                !scenario.EnableEconomy)
            {
                return;
            }

            if (!SystemAPI.TryGetSingleton<RewindState>(out var rewindState) ||
                rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            foreach (var (unrest, entity) in SystemAPI.Query<RefRW<UnrestSignal>>().WithEntityAccess())
            {
                var taxBurden = 0f;
                var taxSourceCount = 0;
                foreach (var (taxPolicy, runtimeState) in SystemAPI.Query<RefRO<TaxPolicy>, RefRO<TaxPolicyRuntimeState>>())
                {
                    if (taxPolicy.ValueRO.TargetEntity != unrest.ValueRO.TargetEntity)
                    {
                        continue;
                    }

                    var taxableBase = math.max(1e-5f, runtimeState.ValueRO.TotalTaxableBase);
                    taxBurden += math.saturate(runtimeState.ValueRO.TotalCollectedTax / taxableBase);
                    taxSourceCount++;
                }

                unrest.ValueRW.TaxBurden = taxSourceCount > 0
                    ? math.saturate(taxBurden / taxSourceCount)
                    : 0f;
                unrest.ValueRW.PriceSpike = 0f; // Placeholder
                unrest.ValueRW.Unemployment = 0f; // Placeholder
                unrest.ValueRW.TotalStress = math.saturate(
                    (unrest.ValueRW.TaxBurden * 0.5f) +
                    (unrest.ValueRW.PriceSpike * 0.3f) +
                    (unrest.ValueRW.Unemployment * 0.2f));
            }
        }
    }

    /// <summary>
    /// Unrest signal component.
    /// Tracks economic stress factors.
    /// </summary>
    public struct UnrestSignal : IComponentData
    {
        public Entity TargetEntity;
        public float TaxBurden;
        public float PriceSpike;
        public float Unemployment;
        public float TotalStress;
    }
}
