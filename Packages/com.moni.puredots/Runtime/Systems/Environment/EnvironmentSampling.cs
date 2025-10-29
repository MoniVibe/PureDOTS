using PureDOTS.Environment;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Systems.Environment
{
    public readonly struct EnvironmentScalarSample
    {
        public readonly float Base;
        public readonly float Contribution;

        public EnvironmentScalarSample(float baseValue, float contribution)
        {
            Base = baseValue;
            Contribution = contribution;
        }

        public float Value => Base + Contribution;
    }

    public readonly struct EnvironmentSunlightSample
    {
        public readonly SunlightSample Base;
        public readonly SunlightSample Contribution;

        public EnvironmentSunlightSample(SunlightSample baseSample, SunlightSample contribution)
        {
            Base = baseSample;
            Contribution = contribution;
        }

        public SunlightSample Value
        {
            get
            {
                var occluder = math.clamp((int)Base.OccluderCount + Contribution.OccluderCount, 0, ushort.MaxValue);
                return new SunlightSample
                {
                    DirectLight = Base.DirectLight + Contribution.DirectLight,
                    AmbientLight = Base.AmbientLight + Contribution.AmbientLight,
                    OccluderCount = (ushort)occluder
                };
            }
        }
    }

    public readonly struct EnvironmentWindSample
    {
        public readonly WindSample Base;
        public readonly float2 DirectionContribution;
        public readonly float StrengthContribution;

        public EnvironmentWindSample(WindSample baseSample, float2 directionContribution, float strengthContribution)
        {
            Base = baseSample;
            DirectionContribution = directionContribution;
            StrengthContribution = strengthContribution;
        }

        public WindSample Value
        {
            get
            {
                var direction = Base.Direction + DirectionContribution;
                if (math.lengthsq(direction) > 1e-6f)
                {
                    direction = math.normalize(direction);
                }
                else
                {
                    direction = Base.Direction;
                }

                var strength = math.max(0f, Base.Strength + StrengthContribution);
                return new WindSample
                {
                    Direction = direction,
                    Strength = strength
                };
            }
        }
    }

    /// <summary>
    /// Convenience helpers for sampling shared environment state from systems without
    /// needing to duplicate singleton lookup boilerplate.
    /// </summary>
    public static class EnvironmentSampling
    {
        public static EnvironmentScalarSample SampleMoistureDetailed(float3 worldPosition, float defaultValue = 0f)
        {
            if (!TryGetEntityManager(out var entityManager) || !TryGetSingletonEntity<MoistureGrid>(entityManager, out var gridEntity))
            {
                return new EnvironmentScalarSample(defaultValue, 0f);
            }

            var grid = entityManager.GetComponentData<MoistureGrid>(gridEntity);

            float baseValue;
            if (entityManager.HasBuffer<MoistureGridRuntimeCell>(gridEntity))
            {
                var runtime = entityManager.GetBuffer<MoistureGridRuntimeCell>(gridEntity).AsNativeArray();
                baseValue = EnvironmentGridMath.SampleBilinear(grid.Metadata, runtime, worldPosition, defaultValue);
            }
            else
            {
                baseValue = grid.SampleBilinear(worldPosition, defaultValue);
            }
            var contribution = SampleScalarContribution(entityManager, grid.ChannelId, worldPosition, 0f);
            return new EnvironmentScalarSample(baseValue, contribution);
        }

        public static float SampleMoisture(float3 worldPosition, float defaultValue = 0f)
        {
            return SampleMoistureDetailed(worldPosition, defaultValue).Value;
        }

        public static EnvironmentScalarSample SampleTemperatureDetailed(float3 worldPosition, float defaultValue = 0f)
        {
            if (!TryGetEntityManager(out var entityManager) || !TryGetSingletonEntity<TemperatureGrid>(entityManager, out var gridEntity))
            {
                return new EnvironmentScalarSample(defaultValue, 0f);
            }

            var grid = entityManager.GetComponentData<TemperatureGrid>(gridEntity);
            var baseValue = grid.SampleBilinear(worldPosition, defaultValue);
            var contribution = SampleScalarContribution(entityManager, grid.ChannelId, worldPosition, 0f);
            return new EnvironmentScalarSample(baseValue, contribution);
        }

        public static float SampleTemperature(float3 worldPosition, float defaultValue = 0f)
        {
            return SampleTemperatureDetailed(worldPosition, defaultValue).Value;
        }

        public static EnvironmentSunlightSample SampleSunlightDetailed(float3 worldPosition, SunlightSample defaultValue = default)
        {
            if (!TryGetEntityManager(out var entityManager) || !TryGetSingletonEntity<SunlightGrid>(entityManager, out var gridEntity))
            {
                return new EnvironmentSunlightSample(defaultValue, default);
            }

            var grid = entityManager.GetComponentData<SunlightGrid>(gridEntity);
            SunlightSample baseSample;
            if (entityManager.HasBuffer<SunlightGridRuntimeSample>(gridEntity))
            {
                var runtime = entityManager.GetBuffer<SunlightGridRuntimeSample>(gridEntity);
                var runtimeSamples = runtime.Reinterpret<SunlightSample>().AsNativeArray();
                baseSample = EnvironmentGridMath.SampleBilinear(grid.Metadata, runtimeSamples, worldPosition, defaultValue);
            }
            else
            {
                baseSample = grid.SampleBilinear(worldPosition, defaultValue);
            }
            var vectorContribution = SampleVectorContribution(entityManager, grid.ChannelId, worldPosition);
            var contributionSample = new SunlightSample
            {
                DirectLight = vectorContribution.x,
                AmbientLight = vectorContribution.y,
                OccluderCount = (ushort)math.clamp(math.round(vectorContribution.z), 0f, ushort.MaxValue)
            };

            return new EnvironmentSunlightSample(baseSample, contributionSample);
        }

        public static SunlightSample SampleSunlight(float3 worldPosition, SunlightSample defaultValue = default)
        {
            return SampleSunlightDetailed(worldPosition, defaultValue).Value;
        }

        public static EnvironmentWindSample SampleWindDetailed(float3 worldPosition, WindSample defaultValue = default)
        {
            if (!TryGetEntityManager(out var entityManager) || !TryGetSingletonEntity<WindField>(entityManager, out var gridEntity))
            {
                return new EnvironmentWindSample(defaultValue, float2.zero, 0f);
            }

            var grid = entityManager.GetComponentData<WindField>(gridEntity);
            var baseSample = grid.SampleBilinear(worldPosition, defaultValue);
            var vectorContribution = SampleVectorContribution(entityManager, grid.ChannelId, worldPosition);
            var directionContribution = new float2(vectorContribution.x, vectorContribution.y);
            var strengthContribution = vectorContribution.z;

            return new EnvironmentWindSample(baseSample, directionContribution, strengthContribution);
        }

        public static WindSample SampleWind(float3 worldPosition, WindSample defaultValue = default)
        {
            return SampleWindDetailed(worldPosition, defaultValue).Value;
        }

        public static BiomeType SampleBiome(float3 worldPosition, BiomeType defaultValue = BiomeType.Unknown)
        {
            if (!TryGetEntityManager(out var entityManager) || !TryGetSingletonEntity<BiomeGrid>(entityManager, out var gridEntity))
            {
                return defaultValue;
            }

            var grid = entityManager.GetComponentData<BiomeGrid>(gridEntity);
            return grid.SampleNearest(worldPosition, defaultValue);
        }

        public static ClimateState GetClimateStateOrDefault()
        {
            return TryGetEntityManager(out var entityManager) && TryGetSingletonEntity<ClimateState>(entityManager, out var climateEntity)
                ? entityManager.GetComponentData<ClimateState>(climateEntity)
                : default;
        }

        private static float SampleScalarContribution(EntityManager entityManager, FixedString64Bytes channelId, float3 worldPosition, float defaultValue)
        {
            if (!TryGetSingletonEntity<EnvironmentEffectCatalogData>(entityManager, out var effectEntity))
            {
                return defaultValue;
            }

            var descriptors = entityManager.GetBuffer<EnvironmentScalarChannelDescriptor>(effectEntity);
            var contributions = entityManager.GetBuffer<EnvironmentScalarContribution>(effectEntity).Reinterpret<float>().AsNativeArray();

            for (var i = 0; i < descriptors.Length; i++)
            {
                var descriptor = descriptors[i];
                if (descriptor.ChannelId != channelId)
                {
                    continue;
                }

                var slice = contributions.GetSubArray(descriptor.Offset, descriptor.Length);
                return EnvironmentGridMath.SampleBilinear(descriptor.Metadata, slice, worldPosition, defaultValue);
            }

            return defaultValue;
        }

        private static float3 SampleVectorContribution(EntityManager entityManager, FixedString64Bytes channelId, float3 worldPosition)
        {
            if (!TryGetSingletonEntity<EnvironmentEffectCatalogData>(entityManager, out var effectEntity))
            {
                return float3.zero;
            }

            var descriptors = entityManager.GetBuffer<EnvironmentVectorChannelDescriptor>(effectEntity);
            var contributions = entityManager.GetBuffer<EnvironmentVectorContribution>(effectEntity).Reinterpret<float3>().AsNativeArray();

            for (var i = 0; i < descriptors.Length; i++)
            {
                var descriptor = descriptors[i];
                if (descriptor.ChannelId != channelId)
                {
                    continue;
                }

                var slice = contributions.GetSubArray(descriptor.Offset, descriptor.Length);
                return EnvironmentGridMath.SampleBilinearVector(descriptor.Metadata, slice, worldPosition, float3.zero);
            }

            return float3.zero;
        }

        private static bool TryGetEntityManager(out EntityManager entityManager)
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
            {
                entityManager = default;
                return false;
            }

            entityManager = world.EntityManager;
            return true;
        }

        private static bool TryGetSingletonEntity<T>(EntityManager entityManager, out Entity entity)
            where T : unmanaged, IComponentData
        {
            using var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<T>());
            if (query.IsEmptyIgnoreFilter)
            {
                entity = Entity.Null;
                return false;
            }

            entity = query.GetSingletonEntity();
            return entity != Entity.Null;
        }
    }
}

