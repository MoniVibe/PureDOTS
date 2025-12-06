using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Math;
using PureDOTS.Runtime.Physics;
using PureDOTS.Runtime.Spatial;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Systems.Physics
{
    /// <summary>
    /// Broad-phase collision detection system using spatial partitioning.
    /// Uses hash grid for efficient neighbor queries and radius-ratio culling.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    [UpdateBefore(typeof(MicroCollisionSystemGroup))]
    [UpdateBefore(typeof(MesoCollisionSystemGroup))]
    [UpdateBefore(typeof(MacroCollisionSystemGroup))]
    public partial struct CollisionBroadPhaseSystem : ISystem
    {
        private ComponentLookup<CollisionProperties> _collisionPropsLookup;
        private ComponentLookup<LocalTransform> _transformLookup;
        private BufferLookup<ImpactEvent> _impactEventLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<SpatialGridConfig>();

            _collisionPropsLookup = state.GetComponentLookup<CollisionProperties>(true);
            _transformLookup = state.GetComponentLookup<LocalTransform>(true);
            _impactEventLookup = state.GetBufferLookup<ImpactEvent>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var rewindState = SystemAPI.GetSingleton<RewindState>();
            if (rewindState.Mode != RewindMode.Record)
                return;

            _collisionPropsLookup.Update(ref state);
            _transformLookup.Update(ref state);
            _impactEventLookup.Update(ref state);

            var job = new BroadPhaseCullingJob
            {
                CollisionPropsLookup = _collisionPropsLookup,
                TransformLookup = _transformLookup,
                ImpactEventLookup = _impactEventLookup
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
        }

        /// <summary>
        /// Checks if a collision pair should be culled based on radius ratio.
        /// Returns true if collision should be skipped (culled).
        /// </summary>
        [BurstCompile]
        public static bool ShouldCullCollision(float radiusA, float radiusB)
        {
            var radiusRatio = radiusA > radiusB 
                ? radiusA / radiusB 
                : radiusB / radiusA;

            // Skip fine physics if radius ratio > 1e5 (use analytic trajectory instead)
            return radiusRatio > CollisionMath.RADIUS_RATIO_CULL_THRESHOLD;
        }

        /// <summary>
        /// Gets neighboring entities from spatial grid for collision checking.
        /// </summary>
        [BurstCompile]
        public static void GetNeighbors(
            Entity entity,
            float3 position,
            float queryRadius,
            in SpatialGridConfig config,
            in DynamicBuffer<SpatialGridCellRange> cellRanges,
            in DynamicBuffer<SpatialGridEntry> entries,
            NativeList<Entity> neighbors)
        {
            neighbors.Clear();

            // Quantize position to cell coordinates
            SpatialHash.Quantize(position, config, out var cellCoords);

            // Check neighboring cells (3x3x3 grid around position)
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        var neighborCell = cellCoords + new int3(dx, dy, dz);

                        // Clamp to valid cell range
                        if (neighborCell.x < 0 || neighborCell.x >= config.CellCounts.x ||
                            neighborCell.y < 0 || neighborCell.y >= config.CellCounts.y ||
                            neighborCell.z < 0 || neighborCell.z >= config.CellCounts.z)
                            continue;

                        var cellId = SpatialHash.Flatten(neighborCell, config);
                        if (cellId < 0 || cellId >= cellRanges.Length)
                            continue;

                        var cellRange = cellRanges[cellId];
                        var startIdx = cellRange.StartIndex;
                        var count = cellRange.Count;

                        // Check entities in this cell
                        for (int i = startIdx; i < startIdx + count && i < entries.Length; i++)
                        {
                            var entry = entries[i];
                            if (entry.Entity == entity)
                                continue;

                            // Distance check
                            var distance = math.distance(position, entry.Position);
                            if (distance <= queryRadius)
                            {
                                neighbors.Add(entry.Entity);
                            }
                        }
                    }
                }
            }
        }

        [BurstCompile]
        [WithAll(typeof(CollisionProperties))]
        partial struct BroadPhaseCullingJob : IJobEntity
        {
            [ReadOnly] public ComponentLookup<CollisionProperties> CollisionPropsLookup;
            [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
            [ReadOnly] public BufferLookup<ImpactEvent> ImpactEventLookup;

            public void Execute(Entity entity, ref CollisionProperties props)
            {
                // Check impact events and cull based on radius ratio
                if (!ImpactEventLookup.HasBuffer(entity))
                    return;

                var impactEvents = ImpactEventLookup[entity];
                for (int i = impactEvents.Length - 1; i >= 0; i--)
                {
                    var impact = impactEvents[i];
                    var otherEntity = impact.A == entity ? impact.B : impact.A;

                    if (!CollisionPropsLookup.HasComponent(otherEntity))
                        continue;

                    var otherProps = CollisionPropsLookup[otherEntity];

                    // Cull if radius ratio is too large
                    if (ShouldCullCollision(props.Radius, otherProps.Radius))
                    {
                        // Remove impact event (culled)
                        impactEvents.RemoveAt(i);
                    }
                }
            }
        }
    }
}

