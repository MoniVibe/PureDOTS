using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Spatial;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Systems.Spatial
{
    /// <summary>
    /// Applies instantaneous skip jumps by relocating entities with SkipJumpRequest.
    /// </summary>
    [UpdateInGroup(typeof(SpatialSystemGroup))]
    [UpdateBefore(typeof(SpatialGridBuildSystem))]
    public partial struct SkipJumpSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            if (timeState.IsPaused)
            {
                return;
            }

            var rewind = SystemAPI.GetSingleton<RewindState>();
            if (rewind.Mode != RewindMode.Record)
            {
                return;
            }

            var skipStateLookup = state.GetComponentLookup<SkipJumpState>(false);
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (request, transform, entity) in SystemAPI
                         .Query<RefRO<SkipJumpRequest>, RefRW<LocalTransform>>()
                         .WithEntityAccess())
            {
                var origin = transform.ValueRO.Position;
                var destination = request.ValueRO.Destination;
                var maxRange = math.max(0f, request.ValueRO.MaxRange);
                var minRange = math.max(0f, request.ValueRO.MinRange);

                if (request.ValueRO.ClampToRange != 0)
                {
                    var delta = destination - origin;
                    var distance = math.length(delta);
                    if (distance > 0.0001f)
                    {
                        if (maxRange > 0f && distance > maxRange)
                        {
                            destination = origin + delta / distance * maxRange;
                        }
                        else if (minRange > 0f && distance < minRange)
                        {
                            destination = origin + delta / distance * minRange;
                        }
                    }
                }

                transform.ValueRW.Position = destination;
                ecb.RemoveComponent<SkipJumpRequest>(entity);

                if (skipStateLookup.HasComponent(entity))
                {
                    var skipState = skipStateLookup[entity];
                    skipState.LastJumpTick = timeState.Tick;
                    skipStateLookup[entity] = skipState;
                }
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}
