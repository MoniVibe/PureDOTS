using PureDOTS.Runtime.AI.Cognitive;
using PureDOTS.Runtime.Core;
using PureDOTS.Runtime.Spatial;
using PureDOTS.Systems;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Runtime.AI.Cognitive.Systems.Learning
{
    /// <summary>
    /// Affordance detection system - 1Hz cognitive layer.
    /// Scans nearby objects via spatial grid, filters by reachability, ranks by utility.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(LearningSystemGroup))]
    public partial struct AffordanceDetectionSystem : ISystem
    {
        private const float UpdateInterval = 1.0f; // 1Hz
        private const float MaxDetectionRange = 10.0f; // Max distance to detect affordances
        private float _lastUpdateTime;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RewindState>();
            state.RequireForUpdate<TickTimeState>();
            state.RequireForUpdate<SpatialGridConfig>();
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

            var currentTime = (float)SystemAPI.Time.ElapsedTime;
            if (currentTime - _lastUpdateTime < UpdateInterval)
            {
                return;
            }

            _lastUpdateTime = currentTime;

            var gridEntity = SystemAPI.GetSingletonEntity<SpatialGridConfig>();
            var gridConfig = SystemAPI.GetSingleton<SpatialGridConfig>();
            var rangesBuffer = state.EntityManager.GetBuffer<SpatialGridCellRange>(gridEntity);
            var entriesBuffer = state.EntityManager.GetBuffer<SpatialGridEntry>(gridEntity);

            if (rangesBuffer.Length == 0 || entriesBuffer.Length == 0)
            {
                return;
            }

            var affordanceLookup = SystemAPI.GetComponentLookup<Affordance>(true);
            var transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
            affordanceLookup.Update(ref state);
            transformLookup.Update(ref state);

            var job = new AffordanceDetectionJob
            {
                GridConfig = gridConfig,
                CellRanges = rangesBuffer.AsNativeArray(),
                Entries = entriesBuffer.AsNativeArray(),
                AffordanceLookup = affordanceLookup,
                TransformLookup = transformLookup,
                MaxRange = MaxDetectionRange
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        private partial struct AffordanceDetectionJob : IJobEntity
        {
            [ReadOnly] public SpatialGridConfig GridConfig;
            [ReadOnly] public NativeArray<SpatialGridCellRange> CellRanges;
            [ReadOnly] public NativeArray<SpatialGridEntry> Entries;
            [ReadOnly] public ComponentLookup<Affordance> AffordanceLookup;
            [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
            public float MaxRange;

            public void Execute(
                Entity entity,
                [ChunkIndexInQuery] int chunkIndex,
                in LocalTransform transform,
                ref DynamicBuffer<DetectedAffordance> detectedAffordances)
            {
                detectedAffordances.Clear();

                float3 agentPos = transform.Position;
                float maxRangeSq = MaxRange * MaxRange;

                // Query spatial grid for nearby entities
                var queryDescriptor = new SpatialQueryDescriptor
                {
                    Origin = agentPos,
                    Radius = MaxRange,
                    MaxResults = 32, // Process up to 32 objects per SIMD packet
                    Options = SpatialQueryOptions.IgnoreSelf | SpatialQueryOptions.RequireDeterministicSorting,
                    Tolerance = 1e-4f,
                    ExcludedEntity = entity
                };

                // Perform spatial query using SpatialQueryHelper
                var results = new NativeList<KNearestResult>(16, Allocator.Temp);
                
                // Use spatial query helper to find nearby entities with affordances
                float3 queryPos = agentPos;
                SpatialQueryHelper.FindKNearestInRadius(
                    ref queryPos,
                    MaxRange,
                    32, // Max results
                    GridConfig,
                    CellRanges,
                    Entries,
                    ref results,
                    new AffordanceFilter
                    {
                        AffordanceLookup = AffordanceLookup,
                        TransformLookup = TransformLookup
                    });

                // Process results and rank by utility
                for (int i = 0; i < results.Length; i++)
                {
                    var result = results[i];
                    if (!AffordanceLookup.HasComponent(result.Entity))
                    {
                        continue;
                    }

                    var affordance = AffordanceLookup[result.Entity];

                    // Rank by RewardPotential / Effort (utility score)
                    float effort = math.max(affordance.Effort, 0.01f); // Avoid division by zero
                    float utilityScore = affordance.RewardPotential / effort;

                    detectedAffordances.Add(new DetectedAffordance
                    {
                        ObjectEntity = result.Entity,
                        Type = affordance.Type,
                        UtilityScore = utilityScore,
                        DistanceSq = result.DistanceSq
                    });
                }

                // Sort by utility score (highest first)
                detectedAffordances.Sort(new AffordanceUtilityComparer());

                // Keep top N affordances
                const int maxAffordances = 8;
                if (detectedAffordances.Length > maxAffordances)
                {
                    // Remove excess (keep first maxAffordances)
                    for (int i = detectedAffordances.Length - 1; i >= maxAffordances; i--)
                    {
                        detectedAffordances.RemoveAt(i);
                    }
                }

                results.Dispose();
            }
        }

        /// <summary>
        /// Filter for spatial queries to only return entities with affordances.
        /// </summary>
        private struct AffordanceFilter : ISpatialQueryFilter
        {
            [ReadOnly] public ComponentLookup<Affordance> AffordanceLookup;
            [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;

            public bool Accept(int descriptorIndex, in SpatialQueryDescriptor descriptor, in SpatialGridEntry entry)
            {
                return AffordanceLookup.HasComponent(entry.Entity) && TransformLookup.HasComponent(entry.Entity);
            }
        }

        /// <summary>
        /// Comparer for sorting affordances by utility score.
        /// </summary>
        private struct AffordanceUtilityComparer : System.Collections.Generic.IComparer<DetectedAffordance>
        {
            public int Compare(DetectedAffordance x, DetectedAffordance y)
            {
                // Sort descending by utility score
                return y.UtilityScore.CompareTo(x.UtilityScore);
            }
        }
    }
}

