using PureDOTS.Runtime.Aggregate;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Construction;
using PureDOTS.Runtime.Motivation;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Systems.Construction
{
    /// <summary>
    /// Aggregates individual BuildNeedSignals into group ConstructionIntents.
    /// Combines signals with group-wide metrics, preferences, and motivations.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(BuildNeedSignalSystem))]
    public partial struct ConstructionDemandAggregationSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
            state.RequireForUpdate<ConstructionConfigState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            var rewindState = SystemAPI.GetSingleton<RewindState>();

            if (timeState.IsPaused || rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            var configState = SystemAPI.GetSingleton<ConstructionConfigState>();

            // Only check periodically
            if (timeState.Tick % configState.AggregationCheckFrequency != 0)
            {
                return;
            }

            var currentTick = timeState.Tick;
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

            // Process groups with BuildCoordinator
            foreach (var (coordinator, signals, intents, entity) in SystemAPI.Query<
                RefRO<BuildCoordinator>,
                DynamicBuffer<BuildNeedSignal>,
                DynamicBuffer<ConstructionIntent>>().WithEntityAccess())
            {
                if (coordinator.ValueRO.AutoBuildEnabled == 0)
                    continue;

                // Clear old signals (older than N ticks)
                var signalAgeThreshold = 1000u; // Keep signals for ~11 seconds at 90 TPS
                for (int i = signals.Length - 1; i >= 0; i--)
                {
                    if (currentTick - signals[i].EmittedTick > signalAgeThreshold)
                    {
                        signals.RemoveAt(i);
                    }
                }

                // Get preference profile (if exists)
                var hasPreferences = SystemAPI.HasComponent<BuildPreferenceProfile>(entity);
                var preferences = hasPreferences
                    ? SystemAPI.GetComponent<BuildPreferenceProfile>(entity)
                    : GetDefaultPreferences();

                // Get group motivations (if exists)
                var hasMotivations = SystemAPI.HasComponent<MotivationDrive>(entity) &&
                                    SystemAPI.HasBuffer<MotivationSlot>(entity);
                var motivationMultipliers = hasMotivations
                    ? ComputeMotivationMultipliers(ref state, entity)
                    : GetDefaultMultipliers();

                // Bucket signals by category and compute aggregated demands
                var categoryDemands = new NativeHashMap<BuildCategory, CategoryDemand>(9, Allocator.Temp);

                // Aggregate signals by category
                for (int i = 0; i < signals.Length; i++)
                {
                    var signal = signals[i];
                    if (!categoryDemands.ContainsKey(signal.Category))
                    {
                        categoryDemands[signal.Category] = new CategoryDemand
                        {
                            TotalStrength = 0f,
                            PositionSum = float3.zero,
                            SignalCount = 0
                        };
                    }

                    var demand = categoryDemands[signal.Category];
                    demand.TotalStrength += signal.Strength;
                    demand.PositionSum += signal.Position;
                    demand.SignalCount++;
                    categoryDemands[signal.Category] = demand;
                }

                // Compute group-wide metrics (stub - game-specific systems can extend)
                var groupMetrics = ComputeGroupMetrics(ref state, entity);

                // Create or update ConstructionIntents for each category
                foreach (var kvp in categoryDemands)
                {
                    var category = kvp.Key;
                    var demand = kvp.Value;

                    if (demand.SignalCount == 0)
                        continue;

                    // Compute urgency from signals, preferences, and motivations
                    var avgStrength = demand.TotalStrength / demand.SignalCount;
                    var avgPosition = demand.PositionSum / demand.SignalCount;

                    var preferenceWeight = GetPreferenceWeight(in preferences, category);
                    var motivationMultiplier = GetMotivationMultiplier(in motivationMultipliers, category);

                    var urgency = math.saturate(avgStrength * preferenceWeight * motivationMultiplier);

                    // Find or create intent for this category
                    int intentIndex = -1;
                    for (int i = 0; i < intents.Length; i++)
                    {
                        if (intents[i].Category == category && intents[i].Status == 0) // Planned
                        {
                            intentIndex = i;
                            break;
                        }
                    }

                    if (intentIndex >= 0)
                    {
                        // Update existing intent
                        var intent = intents[intentIndex];
                        intent.Urgency = math.max(intent.Urgency, urgency);
                        intent.SuggestedCenter = avgPosition;
                        intent.DesiredCount = math.max(intent.DesiredCount, demand.SignalCount * 0.1f); // Rough estimate
                        intents[intentIndex] = intent;
                    }
                    else
                    {
                        // Create new intent
                        intents.Add(new ConstructionIntent
                        {
                            PatternId = -1, // Will be set by pattern selection system
                            Category = category,
                            Urgency = urgency,
                            SuggestedCenter = avgPosition,
                            DesiredCount = demand.SignalCount * 0.1f,
                            ExistingCount = 0f,
                            Source = 0, // Needs
                            Status = 0, // Planned
                            CreatedTick = currentTick
                        });
                    }
                }

                categoryDemands.Dispose();
            }
        }

        [BurstCompile]
        private static BuildPreferenceProfile GetDefaultPreferences()
        {
            return new BuildPreferenceProfile
            {
                HousingWeight = 1f,
                StorageWeight = 1f,
                WorshipWeight = 1f,
                DefenseWeight = 1f,
                FoodWeight = 1f,
                ProductionWeight = 1f,
                InfrastructureWeight = 1f,
                AestheticWeight = 1f,
                LastUpdateTick = 0
            };
        }

        [BurstCompile]
        private static float GetPreferenceWeight(in BuildPreferenceProfile preferences, BuildCategory category)
        {
            return category switch
            {
                BuildCategory.Housing => preferences.HousingWeight,
                BuildCategory.Storage => preferences.StorageWeight,
                BuildCategory.Worship => preferences.WorshipWeight,
                BuildCategory.Defense => preferences.DefenseWeight,
                BuildCategory.Food => preferences.FoodWeight,
                BuildCategory.Production => preferences.ProductionWeight,
                BuildCategory.Infrastructure => preferences.InfrastructureWeight,
                BuildCategory.Aesthetic => preferences.AestheticWeight,
                _ => 1f
            };
        }

        [BurstCompile]
        private static NativeHashMap<BuildCategory, float> GetDefaultMultipliers()
        {
            var multipliers = new NativeHashMap<BuildCategory, float>(9, Allocator.TempJob);
            multipliers[BuildCategory.Housing] = 1f;
            multipliers[BuildCategory.Storage] = 1f;
            multipliers[BuildCategory.Worship] = 1f;
            multipliers[BuildCategory.Defense] = 1f;
            multipliers[BuildCategory.Food] = 1f;
            multipliers[BuildCategory.Production] = 1f;
            multipliers[BuildCategory.Infrastructure] = 1f;
            multipliers[BuildCategory.Aesthetic] = 1f;
            multipliers[BuildCategory.Special] = 1f;
            return multipliers;
        }

        [BurstCompile]
        private static NativeHashMap<BuildCategory, float> ComputeMotivationMultipliers(ref SystemState state, Entity groupEntity)
        {
            var multipliers = GetDefaultMultipliers();

            // Read active motivation slots and boost relevant categories
            if (SystemAPI.HasBuffer<MotivationSlot>(groupEntity))
            {
                var slots = SystemAPI.GetBuffer<MotivationSlot>(groupEntity);
                for (int i = 0; i < slots.Length; i++)
                {
                    var slot = slots[i];
                    if (slot.Status == MotivationStatus.InProgress)
                    {
                        // TODO: Read MotivationSpec from catalog to determine which categories to boost
                        // For now, stub - game-specific systems can extend this
                        // Example: If SpecId matches "Build Great Temple" → boost Worship
                        // Example: If SpecId matches "Become Fortress" → boost Defense
                    }
                }
            }

            return multipliers;
        }

        [BurstCompile]
        private static float GetMotivationMultiplier(in NativeHashMap<BuildCategory, float> multipliers, BuildCategory category)
        {
            return multipliers.TryGetValue(category, out var multiplier) ? multiplier : 1f;
        }

        [BurstCompile]
        private static GroupMetrics ComputeGroupMetrics(ref SystemState state, Entity groupEntity)
        {
            // Stub - game-specific systems can extend this
            // Would compute: population, housing capacity, food reserves, storage fill, threat level
            return new GroupMetrics
            {
                Population = 0f,
                HousingCapacity = 0f,
                FoodReserves = 0f,
                StorageFillRatio = 0f,
                ThreatLevel = 0f
            };
        }

        private struct CategoryDemand
        {
            public float TotalStrength;
            public float3 PositionSum;
            public int SignalCount;
        }

        private struct GroupMetrics
        {
            public float Population;
            public float HousingCapacity;
            public float FoodReserves;
            public float StorageFillRatio;
            public float ThreatLevel;
        }
    }
}

