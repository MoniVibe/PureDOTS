using PureDOTS.Runtime.Camera;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace PureDOTS.Systems.Presentation
{
    /// <summary>
    /// Maintains double-buffered presentation snapshots for temporal coherency.
    /// Copies LocalTransform to PresentationSnapshot buffers each tick.
    /// Presentation systems interpolate between buffers A and B to prevent half-updated reads.
    /// 
    /// See: Docs/Guides/SimulationPresentationTimeSeparationGuide.md for usage examples.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(Unity.Entities.PresentationSystemGroup))]
    [UpdateAfter(typeof(Unity.Entities.SimulationSystemGroup))]
    public partial struct PresentationSnapshotSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TickTimeState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var tickState = SystemAPI.GetSingleton<TickTimeState>();
            uint currentTick = tickState.Tick;

            // Process entities with LocalTransform
            // Add PresentationSnapshot component if missing
            var ecb = new Unity.Entities.EntityCommandBuffer(Unity.Collections.Allocator.Temp);
            foreach (var (transform, entity) in SystemAPI.Query<RefRO<Unity.Transforms.LocalTransform>>()
                         .WithNone<PresentationSnapshot>()
                         .WithEntityAccess())
            {
                ecb.AddComponent(entity, new PresentationSnapshot
                {
                    PositionA = transform.ValueRO.Position,
                    PositionB = transform.ValueRO.Position,
                    RotationA = transform.ValueRO.Rotation,
                    RotationB = transform.ValueRO.Rotation,
                    TickA = currentTick,
                    TickB = currentTick,
                    IsBufferASwap = false
                });
            }
            ecb.Playback(state.EntityManager);
            ecb.Dispose();

            // Update existing snapshots
            foreach (var (transform, snapshot, entity) in SystemAPI.Query<RefRO<Unity.Transforms.LocalTransform>, RefRW<PresentationSnapshot>>()
                         .WithEntityAccess())
            {
                var pos = transform.ValueRO.Position;
                var rot = transform.ValueRO.Rotation;

                ref var snap = ref snapshot.ValueRW;

                // Swap buffers: write current transform to the inactive buffer
                if (snap.IsBufferASwap)
                {
                    // Write to buffer B
                    snap.PositionB = pos;
                    snap.RotationB = rot;
                    snap.TickB = currentTick;
                    snap.IsBufferASwap = false;
                }
                else
                {
                    // Write to buffer A
                    snap.PositionA = pos;
                    snap.RotationA = rot;
                    snap.TickA = currentTick;
                    snap.IsBufferASwap = true;
                }
            }
        }
    }
}

