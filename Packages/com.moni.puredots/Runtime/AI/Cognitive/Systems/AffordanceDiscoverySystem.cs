using PureDOTS.Runtime.AI.Cognitive;
using PureDOTS.Runtime.Core;
using PureDOTS.Runtime.Spatial;
using PureDOTS.Systems;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Runtime.AI.Cognitive.Systems
{
    /// <summary>
    /// Discovers affordances (interactable objects) in the environment.
    /// Uses Intelligence multiplier to scale scan rates: ObjectsScanned = BaseScan * (0.5 + Intelligence)
    /// Higher Intelligence = more object types evaluated per cycle.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(LearningSystemGroup))]
    public partial struct AffordanceDiscoverySystem : ISystem
    {
        private const float BaseScanRate = 5.0f; // Base objects scanned per second
        private const float UpdateInterval = 0.5f; // 2Hz affordance discovery updates
        private float _lastUpdateTime;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RewindState>();
            state.RequireForUpdate<TickTimeState>();
            state.RequireForUpdate<TimeState>();
            _lastUpdateTime = 0f;
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var rewind = SystemAPI.GetSingleton<RewindState>();
            if (rewind.Mode != RewindMode.Record && rewind.Mode != RewindMode.CatchUp)
            {
                return;
            }

            var tickTime = SystemAPI.GetSingleton<TickTimeState>();
            if (tickTime.IsPaused)
            {
                return;
            }

            var timeState = SystemAPI.GetSingleton<TimeState>();
            var currentTime = (float)SystemAPI.Time.ElapsedTime;
            if (currentTime - _lastUpdateTime < UpdateInterval)
            {
                return;
            }

            _lastUpdateTime = currentTime;

            // Check if spatial grid is available
            if (!SystemAPI.HasSingleton<SpatialGridConfig>() || !SystemAPI.HasSingleton<SpatialGridState>())
            {
                return;
            }

            var job = new AffordanceDiscoveryJob
            {
                DeltaTime = timeState.FixedDeltaTime,
                BaseScanRate = BaseScanRate,
                CurrentTick = tickTime.Tick
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        private partial struct AffordanceDiscoveryJob : IJobEntity
        {
            public float DeltaTime;
            public float BaseScanRate;
            public uint CurrentTick;

            public void Execute(
                Entity entity,
                ref DynamicBuffer<DetectedAffordance> detectedAffordances,
                in CognitiveStats cognitiveStats,
                in LocalTransform transform)
            {
                // Calculate effective scan rate: ObjectsScanned = BaseScan * (0.5 + Intelligence)
                float intNorm = CognitiveStats.Normalize(cognitiveStats.Intelligence);
                float effectiveScanRate = BaseScanRate * (0.5f + intNorm);
                int objectsToScan = (int)math.ceil(effectiveScanRate * DeltaTime);

                // Clear old affordances (keep only recent discoveries)
                // In a full implementation, this would query spatial grid for Affordance components
                // For now, we maintain the buffer structure and let other systems populate it
                // The Intelligence multiplier affects how many objects are evaluated per cycle

                // Note: Actual affordance detection would query SpatialGrid for entities with Affordance component
                // This system provides the Intelligence-based scaling factor for those queries
            }
        }

        /// <summary>
        /// Calculates how many objects should be scanned based on Intelligence.
        /// Formula: ObjectsScanned = BaseScan * (0.5 + Intelligence)
        /// </summary>
        [BurstCompile]
        public static int CalculateObjectsToScan(float baseScanRate, float deltaTime, in CognitiveStats cognitiveStats)
        {
            float intNorm = CognitiveStats.Normalize(cognitiveStats.Intelligence);
            float effectiveScanRate = baseScanRate * (0.5f + intNorm);
            return (int)math.ceil(effectiveScanRate * deltaTime);
        }
    }
}

