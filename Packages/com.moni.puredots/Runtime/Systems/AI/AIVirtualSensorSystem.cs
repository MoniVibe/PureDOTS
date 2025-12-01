using PureDOTS.Runtime.AI;
using PureDOTS.Runtime.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Systems.AI
{
    /// <summary>
    /// Populates virtual sensor readings for internal villager needs (Hunger, Energy, Morale).
    /// These readings are inserted at fixed indices (0, 1, 2) so utility curves can reference them.
    /// Runs after AISensorUpdateSystem to inject virtual readings before the scoring stage.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(AISystemGroup))]
    [UpdateAfter(typeof(AISensorUpdateSystem))]
    public partial struct AIVirtualSensorSystem : ISystem
    {
        private EntityQuery _villagerQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _villagerQuery = SystemAPI.QueryBuilder()
                .WithAll<VillagerNeeds, VillagerMood, AISensorReading>()
                .WithNone<PlaybackGuardTag>()
                .Build();

            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            if (timeState.IsPaused)
            {
                return;
            }

            var rewindState = SystemAPI.GetSingleton<RewindState>();
            if (rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            if (_villagerQuery.IsEmpty)
            {
                return;
            }

            var job = new PopulateVirtualSensorsJob();
            state.Dependency = job.ScheduleParallel(_villagerQuery, state.Dependency);
        }

        [BurstCompile]
        public partial struct PopulateVirtualSensorsJob : IJobEntity
        {
            public void Execute(
                Entity entity,
                in VillagerNeeds needs,
                in VillagerMood mood,
                DynamicBuffer<AISensorReading> readings)
            {
                // Virtual sensor indices:
                // 0 = Hunger (inverted: 1.0 - Food/100, so high score = high need)
                // 1 = Energy (inverted: 1.0 - Energy/100, so high score = high need)
                // 2 = Morale (inverted: 1.0 - Morale/100, so high score = low morale = high need)

                var hungerScore = 1f - math.saturate(needs.HungerFloat / 100f);
                var energyScore = 1f - math.saturate(needs.EnergyFloat / 100f);
                var moraleScore = 1f - math.saturate(mood.Mood / 100f);

                // Store existing readings
                var existingCount = readings.Length;
                if (existingCount > 0)
                {
                    // Create temporary array to hold existing readings
                    var existingReadings = new NativeArray<AISensorReading>(existingCount, Allocator.Temp);
                    for (int i = 0; i < existingCount; i++)
                    {
                        existingReadings[i] = readings[i];
                    }

                    // Resize buffer to accommodate virtual sensors + existing
                    readings.ResizeUninitialized(existingCount + 3);

                    // Shift existing readings to indices 3+
                    for (int i = 0; i < existingCount; i++)
                    {
                        readings[i + 3] = existingReadings[i];
                    }

                    existingReadings.Dispose();
                }
                else
                {
                    // No existing readings, just resize
                    readings.ResizeUninitialized(3);
                }

                // Insert virtual sensor readings at fixed indices
                readings[0] = new AISensorReading
                {
                    Target = entity, // Self-reference for virtual sensors
                    DistanceSq = 0f,
                    NormalizedScore = hungerScore,
                    CellId = -1,
                    SpatialVersion = 0,
                    Category = AISensorCategory.None
                };

                readings[1] = new AISensorReading
                {
                    Target = entity,
                    DistanceSq = 0f,
                    NormalizedScore = energyScore,
                    CellId = -1,
                    SpatialVersion = 0,
                    Category = AISensorCategory.None
                };

                readings[2] = new AISensorReading
                {
                    Target = entity,
                    DistanceSq = 0f,
                    NormalizedScore = moraleScore,
                    CellId = -1,
                    SpatialVersion = 0,
                    Category = AISensorCategory.None
                };
            }
        }
    }
}

