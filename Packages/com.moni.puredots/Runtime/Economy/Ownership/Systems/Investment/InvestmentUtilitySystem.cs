using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Core;
using PureDOTS.Runtime.Economy.Ownership;

namespace PureDOTS.Systems.Economy.Ownership.Investment
{
    /// <summary>
    /// Investment utility system calculating utility scores using MindECS traits.
    /// Utility(asset) = ExpectedReturn * Intelligence + EmotionalBias * Greed - Risk * Fear
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(MindEconomySystemGroup))]
    [UpdateAfter(typeof(InvestmentDecisionSystem))]
    public partial struct InvestmentUtilitySystem : ISystem
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

            // Investment utility calculation would:
            // 1. Query entities with MindECS profile traits (Intelligence, Greed, Fear)
            // 2. For each available asset, calculate:
            //    Utility = ExpectedReturn * Intelligence + EmotionalBias * Greed - Risk * Fear
            // 3. Store utility scores for use by InvestmentDecisionSystem
            // 4. ExpectedReturn comes from Learning system (historical ROI)
            // 5. Risk comes from RiskAssessmentSystem

            // Placeholder implementation - full version would integrate with MindECS traits
        }
    }
}

