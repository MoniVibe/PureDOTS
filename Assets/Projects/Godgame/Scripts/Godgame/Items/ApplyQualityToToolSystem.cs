using PureDOTS.Runtime.Shared;
using PureDOTS.Systems.Shared;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Godgame.Items
{
    /// <summary>
    /// Applies quality multipliers to tool runtime stats via QualityCurveBlob.
    /// Runs in FixedStep simulation group after InstanceQualityCalculationSystem.
    /// </summary>
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(InstanceQualityCalculationSystem))]
    public partial struct ApplyQualityToToolSystem : ISystem
    {
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
                ref ToolRuntimeStats stats)
            {
                ref var curves = ref Curves.Value;
                // Sample curves for durability and efficiency multipliers
                float durabilityMult = QualityEval.SampleCurve(ref curves.Durability, quality.Score01);
                float reliabilityMult = QualityEval.SampleCurve(ref curves.Reliability, quality.Score01);

                // Apply multipliers to runtime stats (not catalog spec)
                stats.Durability *= durabilityMult;
                stats.MaxDurability *= durabilityMult;
                stats.EfficiencyRate *= reliabilityMult;
            }
        }
    }

    /// <summary>
    /// Runtime stats for tools that can be modified by quality.
    /// </summary>
    public struct ToolRuntimeStats : IComponentData
    {
        public float Durability;
        public float MaxDurability;
        public float EfficiencyRate; // Work done per unit time
    }
}

