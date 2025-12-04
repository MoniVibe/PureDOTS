using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Environment;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Systems.Environment
{
    /// <summary>
    /// Compares environment (climate, moisture, sunlight) vs VegetationNeeds and calculates stress/growth factors.
    /// Updates VegetationStress component based on how well environmental conditions meet plant needs.
    /// Runs in EnvironmentSystemGroup to provide stress data for growth systems.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(EnvironmentSystemGroup))]
    public partial struct VegetationStressSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            var rewindState = SystemAPI.GetSingleton<RewindState>();

            // Skip if paused or rewinding
            if (timeState.IsPaused || rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            var currentTick = timeState.Tick;

            // Get environment state (use defaults if singletons don't exist)
            PureDOTS.Runtime.Environment.ClimateState climateState;
            if (SystemAPI.TryGetSingleton<ClimateState>(out var climate))
            {
                climateState = climate;
            }
            else
            {
                climateState = new PureDOTS.Runtime.Environment.ClimateState
                {
                    Temperature = 20f,
                    Humidity = 0.5f,
                    SeasonIndex = 0,
                    SeasonTick = 0,
                    SeasonLength = 250u,
                    LastUpdateTick = 0
                };
            }

            SunlightState sunlightState;
            if (SystemAPI.TryGetSingleton<SunlightState>(out var sunlight))
            {
                sunlightState = sunlight;
            }
            else
            {
                sunlightState = new SunlightState
                {
                    GlobalIntensity = 1f,
                    SourceStar = Entity.Null,
                    LastUpdateTick = 0
                };
            }

            // Process all vegetation entities with needs and stress components
            foreach (var (needs, stress, entity) in SystemAPI.Query<
                RefRO<VegetationNeeds>,
                RefRW<VegetationStress>>().WithEntityAccess())
            {
                var needsValue = needs.ValueRO;
                var stressValue = stress.ValueRO;

                // Get moisture for this entity's cell (Tier-1: use global average, Tier-2: sample from grid)
                float moisture = GetMoistureForEntity(in entity, climateState);

                // Calculate environmental factors
                var moistureFactor = CalculateFactor(
                    moisture,
                    needsValue.MoistureMin,
                    needsValue.MoistureMax);

                var tempFactor = CalculateFactor(
                    climateState.Temperature,
                    needsValue.TempMin,
                    needsValue.TempMax);

                var sunlightFactor = CalculateFactor(
                    sunlightState.GlobalIntensity,
                    needsValue.SunlightMin,
                    needsValue.SunlightMax);

                // Overall growth factor is minimum of all factors (bottleneck principle)
                var overallFactor = math.min(math.min(moistureFactor, tempFactor), sunlightFactor);

                // Stress is inverse of growth factor (low factor = high stress)
                var calculatedStress = 1f - overallFactor;

                // Update stress component
                stressValue.Stress = math.clamp(calculatedStress, 0f, 1f);
                stressValue.GrowthFactor = overallFactor;
                stressValue.MoistureFactor = moistureFactor;
                stressValue.TempFactor = tempFactor;
                stressValue.SunlightFactor = sunlightFactor;
                stressValue.LastStressCheckTick = currentTick;
                stress.ValueRW = stressValue;
            }
        }

        /// <summary>
        /// Calculates how well a value meets needs (0-1, where 1 = optimal).
        /// Public for testing purposes.
        /// </summary>
        [BurstCompile]
        public static float CalculateFactor(float value, float min, float max)
        {
            if (min >= max)
            {
                return 1f; // Invalid range, assume optimal
            }

            if (value < min)
            {
                // Below minimum: factor decreases linearly
                return math.max(0f, value / min);
            }
            else if (value > max)
            {
                // Above maximum: factor decreases linearly
                var excess = value - max;
                var range = max - min;
                return math.max(0f, 1f - (excess / range));
            }
            else
            {
                // Within range: optimal
                return 1f;
            }
        }

        [BurstCompile]
        private static float GetMoistureForEntity(in Entity entity, in PureDOTS.Runtime.Environment.ClimateState climate)
        {
            // Tier-1: Use global humidity as moisture proxy
            // Tier-2: Sample from MoistureGridState at entity's cell
            // For now, use humidity as a simple approximation
            return climate.Humidity;
        }
    }
}

