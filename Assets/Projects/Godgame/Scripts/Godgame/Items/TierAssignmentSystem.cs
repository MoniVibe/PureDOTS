using PureDOTS.Runtime.Shared;
using PureDOTS.Systems.Shared;
using Unity.Burst;
using Unity.Entities;

namespace Godgame.Items
{
    /// <summary>
    /// Assigns QualityTier from InstanceQuality.Score01 via formula blob cutoffs.
    /// Runs in FixedStep simulation group after InstanceQualityCalculationSystem.
    /// Includes temporary legacy rarity mapping behind a feature flag.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(InstanceQualityCalculationSystem))]
    public partial struct TierAssignmentSystem : ISystem
    {
        /// <summary>
        /// Temporary flag for legacy rarity mapping. Set to false once economy-driven rarity is implemented.
        /// </summary>
        private const bool UseLegacyRarityMapping = true;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton<QualityFormulaBlobRef>(out var formulaRef) ||
                !formulaRef.Blob.IsCreated)
            {
                return;
            }

            var formula = formulaRef.Blob.Value;
            new AssignTierJob { Formula = formula }.ScheduleParallel();
        }

        [BurstCompile]
        public partial struct AssignTierJob : IJobEntity
        {
            [ReadOnly]
            public QualityFormulaBlob Formula;

            void Execute(
                Entity entity,
                in InstanceQuality quality,
                ref ToolRarity rarity)
            {
                // Tier is already set by InstanceQualityCalculationSystem
                // This system only handles legacy rarity mapping if enabled

                if (UseLegacyRarityMapping)
                {
                    // Temporary: Map quality score to rarity for backward compatibility
                    // TODO: Remove this once economy-driven rarity is implemented
                    Rarity mappedRarity = quality.Score01 switch
                    {
                        >= 0.90f => Rarity.Legendary,
                        >= 0.70f => Rarity.Epic,
                        >= 0.50f => Rarity.Rare,
                        >= 0.30f => Rarity.Uncommon,
                        _ => Rarity.Common
                    };
                    rarity.Value = mappedRarity;
                }
                // Otherwise, rarity should be set by economy/market systems
            }
        }
    }
}

