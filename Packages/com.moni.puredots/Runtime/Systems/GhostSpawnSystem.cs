using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using PureDOTS.Runtime.Components;
using UnityEngine;

namespace PureDOTS.Systems
{
    /// <summary>
    /// Spawns and updates ghost entities during rewind preview phases.
    /// Ghosts show historical positions/states while the real world stays frozen.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(RewindControlSystem))]
    public partial struct GhostSpawnSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RewindControlState>();
        }

        [BurstDiscard] // Contains Debug.Log calls
        public void OnUpdate(ref SystemState state)
        {
            var controlState = SystemAPI.GetSingleton<RewindControlState>();

            // Only spawn/update ghosts during preview phases
            if (controlState.Phase != RewindPhase.ScrubbingPreview &&
                controlState.Phase != RewindPhase.FrozenPreview)
            {
                // Clean up any existing ghosts when not in preview
                CleanupGhosts(ref state);
                return;
            }

            int previewTick = controlState.PreviewTick;
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            // Find all entities that should have ghosts (entities with history and RewindableTag)
            foreach (var (transform, historyBuffer, entity) in SystemAPI.Query<RefRO<LocalTransform>, DynamicBuffer<ComponentHistory<LocalTransform>>>()
                .WithAll<RewindableTag>()
                .WithNone<GhostTag>()
                .WithEntityAccess())
            {
                // Spawn ghost for this entity
                SpawnGhost(ref state, entity, previewTick, ecb);
            }

            // Update existing ghosts to match PreviewTick
            foreach (var (ghostSource, ghostPreviewTick, ghostTransform) in SystemAPI.Query<RefRO<GhostSourceEntity>, RefRW<GhostPreviewTick>, RefRW<LocalTransform>>()
                .WithAll<GhostTag>())
            {
                if (ghostPreviewTick.ValueRO.Tick != previewTick)
                {
                    // Update ghost position from history
                    UpdateGhostPosition(ref state, ghostSource.ValueRO.SourceEntity, previewTick, ref ghostTransform.ValueRW);
                    ghostPreviewTick.ValueRW.Tick = previewTick;
                }
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstDiscard]
        private void SpawnGhost(ref SystemState state, Entity sourceEntity, int previewTick, EntityCommandBuffer ecb)
        {
            // TODO: Full implementation will:
            // 1. Create ghost entity with GhostTag, GhostSourceEntity, GhostPreviewTick
            // 2. Copy relevant components from source entity (LocalTransform, etc.)
            // 3. Set initial position from ComponentHistory at previewTick
            // 4. Add GhostVisualStyle for rendering hints
            // 5. Mark ghost as non-rewindable (no RewindableTag)

            // Stub: Just log for now
            UnityEngine.Debug.Log($"[GhostSpawn] Would spawn ghost for entity {sourceEntity.Index} at preview tick {previewTick}");
        }

        private void UpdateGhostPosition(ref SystemState state, Entity sourceEntity, int previewTick, ref LocalTransform ghostTransform)
        {
            // TODO: Full implementation will:
            // 1. Get ComponentHistory<LocalTransform> from sourceEntity
            // 2. Use TimeHistoryPlaybackSystem.TryGetInterpolatedSample to get position at previewTick
            // 3. Update ghostTransform with the historical position

            // Stub: For now, just log
            // In full implementation, we'd do something like:
            // if (state.EntityManager.HasBuffer<ComponentHistory<LocalTransform>>(sourceEntity))
            // {
            //     var historyBuffer = state.EntityManager.GetBuffer<ComponentHistory<LocalTransform>>(sourceEntity);
            //     LocalTransform historicalTransform = default;
            //     if (TimeHistoryPlaybackSystem.TryGetInterpolatedSample(ref historyBuffer, (uint)previewTick, ref historicalTransform))
            //     {
            //         ghostTransform = historicalTransform;
            //     }
            // }
        }

        [BurstDiscard]
        private void CleanupGhosts(ref SystemState state)
        {
            // Remove all ghost entities when not in preview phase
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            int ghostCount = 0;

            foreach (var (_, entity) in SystemAPI.Query<RefRO<GhostTag>>().WithEntityAccess())
            {
                ecb.DestroyEntity(entity);
                ghostCount++;
            }

            if (ghostCount > 0)
            {
                ecb.Playback(state.EntityManager);
                UnityEngine.Debug.Log($"[GhostSpawn] Cleaned up {ghostCount} ghost entities");
            }

            ecb.Dispose();
        }
    }
}

