using PureDOTS.Input;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Camera;
#if DEVTOOLS_ENABLED
using PureDOTS.Runtime.Devtools;
#endif
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace PureDOTS.Systems.Input
{
    /// <summary>
    /// Copies input snapshots from Mono bridge to ECS once per DOTS tick.
    /// Runs OrderFirst in SimulationSystemGroup to ensure input is available for all downstream systems.
    /// Handles multi-tick catch-up by clamping deltas if multiple ticks occur in one frame.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    public partial struct CopyInputToEcsSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
        }

        public void OnUpdate(ref SystemState state)
        {
            // Find Mono bridge (ISystem structs must be unmanaged, so we can't cache the reference)
            var bridge = Object.FindFirstObjectByType<InputSnapshotBridge>();
            if (bridge == null)
            {
                return; // No bridge found, skip
            }

            var timeState = SystemAPI.GetSingleton<TimeState>();
            uint currentTick = timeState.Tick;

            // Find hand and camera entities
            Entity handEntity = Entity.Null;
            Entity cameraEntity = Entity.Null;
            Entity timeControlEntity = Entity.Null;

            using (var handQuery = SystemAPI.QueryBuilder()
                .WithAll<DivineHandTag>()
                .Build())
            {
                if (!handQuery.IsEmptyIgnoreFilter)
                {
                    handEntity = handQuery.GetSingletonEntity();
                }
            }

            using (var cameraQuery = SystemAPI.QueryBuilder()
                .WithAll<CameraTag>()
                .Build())
            {
                if (!cameraQuery.IsEmptyIgnoreFilter)
                {
                    cameraEntity = cameraQuery.GetSingletonEntity();
                }
            }

            using (var timeQuery = SystemAPI.QueryBuilder()
                .WithAll<TimeControlInputState, TimeControlSingletonTag>()
                .Build())
            {
                if (!timeQuery.IsEmptyIgnoreFilter)
                {
                    timeControlEntity = timeQuery.GetSingletonEntity();
                }
            }

            // Flush snapshot to ECS
            bridge.FlushSnapshotToEcs(state.EntityManager, handEntity, cameraEntity, timeControlEntity, currentTick);

#if DEVTOOLS_ENABLED
            // Update cursor hit cache for devtools
            UpdateCursorHitCache(ref state, bridge, currentTick);
#endif
        }

#if DEVTOOLS_ENABLED
        private void UpdateCursorHitCache(ref SystemState state, InputSnapshotBridge bridge, uint currentTick)
        {
            if (bridge == null)
            {
                return;
            }

            // Find or create cursor hit cache entity
            Entity cacheEntity;
            using (var query = SystemAPI.QueryBuilder().WithAll<CursorHitCache>().Build())
            {
                if (!query.IsEmptyIgnoreFilter)
                {
                    cacheEntity = query.GetSingletonEntity();
                }
                else
                {
                    cacheEntity = state.EntityManager.CreateEntity(typeof(CursorHitCache));
                }
            }

            // Get raycast hit from bridge
            bridge.GetCursorHit(out var ray, out var hasHit, out var hit, out var modifierKeys);

            var cache = new CursorHitCache
            {
                SampleTick = currentTick,
                RayOrigin = new float3(ray.origin.x, ray.origin.y, ray.origin.z),
                RayDirection = new float3(ray.direction.x, ray.direction.y, ray.direction.z),
                HasHit = hasHit,
                ModifierKeys = modifierKeys
            };

            if (hasHit)
            {
                cache.HitPoint = new float3(hit.point.x, hit.point.y, hit.point.z);
                cache.HitNormal = new float3(hit.normal.x, hit.normal.y, hit.normal.z);
                // Try to get hit entity if it has a collider with a GameObject
                if (hit.collider != null && hit.collider.gameObject != null)
                {
                    // Try to find entity via GameObject conversion (if using hybrid renderer or conversion)
                    // For now, leave as null - would need GameObject-to-Entity mapping
                    cache.HitEntity = Entity.Null;
                }
            }

            state.EntityManager.SetComponentData(cacheEntity, cache);
        }
#endif

        public void OnDestroy(ref SystemState state)
        {
            // No cleanup needed - bridge is found each frame
        }
    }
}
