using PureDOTS.Runtime.Shared;
using PureDOTS.Runtime.Space;
using PureDOTS.Systems.Shared;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Space4X.Systems
{
    /// <summary>
    /// Applies quality multipliers to module runtime stats via QualityCurveBlob.
    /// Runs in FixedStep simulation group after InstanceQualityCalculationSystem.
    /// Rarity is separate (economy-driven) and not applied here.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(InstanceQualityCalculationSystem))]
    public partial struct ApplyQualityToModuleSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton<QualityCurveBlobRef>(out var curveRef) ||
                !curveRef.Blob.IsCreated)
            {
                return;
            }

            new ApplyQualityJob { Curves = curveRef.Blob }.ScheduleParallel();
        }

        [BurstCompile]
        public partial struct ApplyQualityJob : IJobEntity
        {
            [ReadOnly]
            public BlobAssetReference<QualityCurveBlob> Curves;

            void Execute(
                Entity entity,
                in InstanceQuality quality,
                ref ModuleRuntimeStats stats)
            {
                ref var curves = ref Curves.Value;
                // Sample curves for module stat multipliers
                float damageMult = QualityEval.SampleCurve(ref curves.Damage, quality.Score01);
                float reliabilityMult = QualityEval.SampleCurve(ref curves.Reliability, quality.Score01);
                float heatMult = QualityEval.SampleCurve(ref curves.Heat, quality.Score01);

                // Apply multipliers to runtime stats (not catalog spec)
                stats.Damage *= damageMult;
                stats.FireRate *= reliabilityMult;
                stats.SpreadDeg *= reliabilityMult; // Better quality = tighter spread
                stats.HeatPerSec *= heatMult; // Better quality = less heat
                stats.FailureChance *= (2f - reliabilityMult); // Better quality = lower failure chance
            }
        }
    }

    /// <summary>
    /// Runtime stats for modules that can be modified by quality.
    /// </summary>
    public struct ModuleRuntimeStats : IComponentData
    {
        public float Damage;
        public float FireRate;
        public float SpreadDeg;
        public float HeatPerSec;
        public float FailureChance;
    }
}

