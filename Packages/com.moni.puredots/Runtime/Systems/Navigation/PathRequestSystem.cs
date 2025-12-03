using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Navigation;
using Unity.Burst;
using Unity.Entities;

namespace PureDOTS.Systems.Navigation
{
    /// <summary>
    /// Processes PathRequest components and triggers pathfinding computation.
    /// Creates PathState and PathResult buffers for entities requesting paths.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SpatialSystemGroup))]
    [UpdateBefore(typeof(PathfindingSystem))]
    public partial struct PathRequestSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
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

            // Process path requests
            foreach (var (request, entity) in
                SystemAPI.Query<RefRO<PathRequest>>()
                .WithEntityAccess())
            {
                if (request.ValueRO.IsActive == 0)
                {
                    continue;
                }

                // Ensure entity has PathState and PathResult
                if (!SystemAPI.HasComponent<PathState>(entity))
                {
                    state.EntityManager.AddComponent<PathState>(entity);
                }

                if (!SystemAPI.HasBuffer<PathResult>(entity))
                {
                    state.EntityManager.AddBuffer<PathResult>(entity);
                }

                // PathfindingSystem will process the request and populate PathResult
                // This system just ensures components exist
            }
        }
    }
}

