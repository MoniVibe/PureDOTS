using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Core;
using PureDOTS.Runtime.Economy.Ownership;

namespace PureDOTS.Systems.Economy.Ownership.Legal
{
    /// <summary>
    /// Legal entity system managing LegalEntity lifecycle.
    /// Tracks Influence based on owned assets, calculates TaxRate from Influence and policies.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(AggregateEconomySystemGroup))]
    public partial struct LegalEntitySystem : ISystem
    {
        private uint _lastUpdateTick;
        private const uint UpdateIntervalTicks = 300; // 0.2Hz at 60Hz base rate

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RewindState>();
            state.RequireForUpdate<TickTimeState>();
            _lastUpdateTick = 0;
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var rewind = SystemAPI.GetSingleton<RewindState>();
            if (rewind.Mode != RewindMode.Record)
            {
                return;
            }

            var tickState = SystemAPI.GetSingleton<TickTimeState>();
            var currentTick = tickState.Tick;

            // Throttle to 0.2Hz
            if (currentTick - _lastUpdateTick < UpdateIntervalTicks)
            {
                return;
            }

            _lastUpdateTick = currentTick;

            var job = new LegalEntityUpdateJob
            {
                CurrentTick = currentTick
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        private partial struct LegalEntityUpdateJob : IJobEntity
        {
            public uint CurrentTick;

            public void Execute(
                Entity entity,
                ref LegalEntity legalEntity,
                in DynamicBuffer<Portfolio> portfolioBuffer)
            {
                // Calculate Influence based on owned assets
                // Influence = Σ(asset_value * ownership_share) normalized
                float totalAssetValue = 0f;

                for (int i = 0; i < portfolioBuffer.Length; i++)
                {
                    var portfolioEntry = portfolioBuffer[i];
                    // Asset value approximated by ExpectedOutputValue
                    totalAssetValue += portfolioEntry.ExpectedOutputValue * portfolioEntry.OwnershipShare;
                }

                // Normalize influence (0..1) - simple formula, can be enhanced
                legalEntity.Influence = math.saturate(totalAssetValue / 10000f); // Normalize by arbitrary scale

                // Calculate TaxRate from Influence and policies
                // TaxRate = base_rate + (Influence * influence_multiplier)
                float baseTaxRate = 0.1f; // 10% base
                float influenceMultiplier = 0.2f; // Up to 20% additional based on influence
                legalEntity.TaxRate = math.saturate(baseTaxRate + (legalEntity.Influence * influenceMultiplier));

                // Update last update tick
                legalEntity.LastUpdateTick = CurrentTick;
            }
        }
    }
}

