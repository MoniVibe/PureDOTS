using PureDOTS.Runtime.Shared;
using PureDOTS.Systems.Shared;
using Unity.Burst;
using Unity.Entities;

namespace Godgame.Items
{
    /// <summary>
    /// Applies quality multipliers to equipment runtime stats via QualityCurveBlob.
    /// Runs in FixedStep simulation group after InstanceQualityCalculationSystem.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(InstanceQualityCalculationSystem))]
    public partial struct ApplyQualityToEquipmentSystem : ISystem
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
                ref EquipmentRuntimeStats stats)
            {
                ref var curves = ref Curves.Value;
                // Sample curves for damage and durability multipliers
                float damageMult = QualityEval.SampleCurve(ref curves.Damage, quality.Score01);
                float durabilityMult = QualityEval.SampleCurve(ref curves.Durability, quality.Score01);

                // Apply multipliers to runtime stats (not catalog spec)
                stats.Damage *= damageMult;
                stats.Durability *= durabilityMult;
                stats.MaxDurability *= durabilityMult;
            }
        }
    }

    /// <summary>
    /// Runtime stats for equipment that can be modified by quality.
    /// </summary>
    public struct EquipmentRuntimeStats : IComponentData
    {
        public float Damage;
        public float Durability;
        public float MaxDurability;
    }
}

