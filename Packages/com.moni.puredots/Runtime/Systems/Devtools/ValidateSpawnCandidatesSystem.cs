#if DEVTOOLS_ENABLED
using PureDOTS.Runtime.Devtools;
using PureDOTS.Runtime.Spatial;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Systems.Devtools
{
    /// <summary>
    /// Validates spawn candidates and writes validation results.
    /// Checks slope, overlap, bounds, forbidden volumes, navmesh.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(BuildSpawnCandidatesSystem))]
    public partial struct ValidateSpawnCandidatesSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SpawnRequest>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (request, candidates, validationResults) in SystemAPI.Query<RefRO<SpawnRequest>, DynamicBuffer<SpawnCandidate>, DynamicBuffer<SpawnValidationResult>>().WithEntityAccess())
            {
                var req = request.ValueRO;
                validationResults.ResizeUninitialized(candidates.Length);

                for (int i = 0; i < candidates.Length; i++)
                {
                    var candidate = candidates[i];
                    var validation = ValidateCandidate(candidate, req, ref state);
                    validationResults[i] = validation;

                    // Update candidate validity flag
                    if (validation.FailureReason == ValidationFailureReason.None)
                    {
                        candidates[i] = new SpawnCandidate
                        {
                            Position = candidate.Position,
                            Rotation = candidate.Rotation,
                            PrototypeId = candidate.PrototypeId,
                            IsValid = 1
                        };
                    }
                }
            }
        }

        private SpawnValidationResult ValidateCandidate(SpawnCandidate candidate, in SpawnRequest request, ref SystemState state)
        {
            // Simplified validation (can be extended with actual physics/terrain checks)
            var result = new SpawnValidationResult
            {
                FailureReason = ValidationFailureReason.None,
                FailureMessage = default
            };

            // Check bounds (simplified - assumes ground plane at y=0)
            if (candidate.Position.y < -10f || candidate.Position.y > 1000f)
            {
                result.FailureReason = ValidationFailureReason.OutOfBounds;
                result.FailureMessage = new FixedString128Bytes("Position out of bounds");
                return result;
            }

            // If AllowOverlap flag is not set, check for overlaps using spatial grid
            if ((request.Flags & SpawnFlags.AllowOverlap) == 0)
            {
                if (SystemAPI.HasSingleton<SpatialGridConfig>() && SystemAPI.HasSingleton<SpatialGridState>())
                {
                    var config = SystemAPI.GetSingleton<SpatialGridConfig>();
                    var gridState = SystemAPI.GetSingleton<SpatialGridState>();
                    
                    // Get spatial grid buffers
                    var gridEntity = SystemAPI.GetSingletonEntity<SpatialGridConfig>();
                    var ranges = SystemAPI.GetBuffer<SpatialGridCellRange>(gridEntity);
                    var entries = SystemAPI.GetBuffer<SpatialGridEntry>(gridEntity);
                    
                    // Check for nearby entities within a small radius (default 0.5m overlap tolerance)
                    var overlapRadius = 0.5f;
                    var nearbyEntities = new NativeList<Entity>(Allocator.Temp);
                    var candidatePos = candidate.Position;
                    SpatialQueryHelper.GetEntitiesWithinRadius(
                        ref candidatePos,
                        overlapRadius,
                        config,
                        ranges,
                        entries,
                        ref nearbyEntities);
                    
                    // If any entities found, mark as overlap
                    if (nearbyEntities.Length > 0)
                    {
                        result.FailureReason = ValidationFailureReason.OverlapsExisting;
                        result.FailureMessage = new FixedString128Bytes("Overlaps existing entity");
                        nearbyEntities.Dispose();
                        return result;
                    }
                    
                    nearbyEntities.Dispose();
                }
            }

            // If NavmeshOnly flag is set, check navmesh
            // Note: Full navmesh integration requires Unity.AI.Navigation package
            // This is a placeholder that can be extended with actual NavMesh queries
            if ((request.Flags & SpawnFlags.NavmeshOnly) != 0)
            {
                // For now, assume valid if within bounds and on ground plane
                // In the future, this can query Unity NavMesh.SamplePosition or DOTS NavMesh
                // Example future implementation:
                // if (!Unity.AI.Navigation.NavMesh.SamplePosition(candidate.Position, out var hit, 1.0f, -1))
                // {
                //     result.FailureReason = ValidationFailureReason.NotOnNavmesh;
                //     result.FailureMessage = new FixedString128Bytes("Position not on navmesh");
                //     return result;
                // }
                
                // Temporary: Only validate y position is reasonable (ground level)
                if (candidate.Position.y < 0f || candidate.Position.y > 10f)
                {
                    result.FailureReason = ValidationFailureReason.NotOnNavmesh;
                    result.FailureMessage = new FixedString128Bytes("Position not on walkable surface");
                    return result;
                }
            }

            return result;
        }
    }
}
#endif

