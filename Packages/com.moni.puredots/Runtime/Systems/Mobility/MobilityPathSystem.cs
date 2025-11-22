using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Mobility;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Systems.Mobility
{
    /// <summary>
    /// Resolves mobility path requests against the current mobility network snapshot and enqueues rendezvous/interception events.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(MobilityNetworkSystem))]
    public partial struct MobilityPathSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<MobilityNetwork>();
            state.RequireForUpdate<TimeState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            if (timeState.IsPaused || (SystemAPI.TryGetSingleton(out RewindState rewindState) && rewindState.Mode != RewindMode.Record))
            {
                return;
            }

            var networkEntity = SystemAPI.GetSingletonEntity<MobilityNetwork>();
            var waypoints = state.EntityManager.GetBuffer<MobilityWaypointEntry>(networkEntity);
            var highways = state.EntityManager.GetBuffer<MobilityHighwayEntry>(networkEntity);

            var waypointExists = new NativeHashSet<int>(waypoints.Length, state.WorldUpdateAllocator);
            var distanceById = new NativeParallelHashMap<int, float>(waypoints.Length, state.WorldUpdateAllocator);
            var previousById = new NativeParallelHashMap<int, int>(waypoints.Length, state.WorldUpdateAllocator);

            for (int i = 0; i < waypoints.Length; i++)
            {
                var id = waypoints[i].WaypointId;
                waypointExists.Add(id);
                distanceById.TryAdd(id, float.PositiveInfinity);
            }

            DynamicBuffer<MobilityInterceptionEvent> interceptionBuffer = default;
            if (state.EntityManager.HasBuffer<MobilityInterceptionEvent>(networkEntity))
            {
                interceptionBuffer = state.EntityManager.GetBuffer<MobilityInterceptionEvent>(networkEntity);
            }

            foreach (var (request, result, path) in SystemAPI.Query<RefRO<MobilityPathRequest>, RefRW<MobilityPathResult>, DynamicBuffer<MobilityPathWaypoint>>())
            {
                var req = request.ValueRO;

                if (!waypointExists.Contains(req.FromWaypointId) || !waypointExists.Contains(req.ToWaypointId))
                {
                    result.ValueRW = new MobilityPathResult
                    {
                        Status = MobilityPathStatus.Failed,
                        EstimatedCost = 0f,
                        HopCount = 0,
                        LastUpdateTick = timeState.Tick
                    };
                    path.Clear();
                    continue;
                }

            var cost = SolvePath(req.FromWaypointId, req.ToWaypointId, waypoints, highways, waypointExists, distanceById, previousById, path, state.WorldUpdateAllocator);
            if (req.MaxCost > 0f && cost > req.MaxCost || cost < 0f)
            {
                result.ValueRW = new MobilityPathResult
                {
                        Status = MobilityPathStatus.Failed,
                        EstimatedCost = cost,
                        HopCount = 0,
                        LastUpdateTick = timeState.Tick
                    };
                    path.Clear();
                    continue;
                }

                path.Clear();
                path.Add(new MobilityPathWaypoint { WaypointId = req.FromWaypointId });
                if (req.FromWaypointId != req.ToWaypointId)
                {
                    path.Add(new MobilityPathWaypoint { WaypointId = req.ToWaypointId });
                }

                result.ValueRW = new MobilityPathResult
                {
                    Status = MobilityPathStatus.Assigned,
                    EstimatedCost = cost,
                    HopCount = path.Length,
                    LastUpdateTick = timeState.Tick
                };

                if (interceptionBuffer.IsCreated && (req.Flags & MobilityPathRequestFlags.BroadcastRendezvous) != 0)
                {
                    interceptionBuffer.Add(new MobilityInterceptionEvent
                    {
                        FromWaypointId = req.FromWaypointId,
                        ToWaypointId = req.ToWaypointId,
                        Tick = timeState.Tick,
                        Type = 0
                    });
                }
            }
        }

        private static float SolvePath(
            int fromId,
            int toId,
            DynamicBuffer<MobilityWaypointEntry> waypoints,
            DynamicBuffer<MobilityHighwayEntry> highways,
            NativeHashSet<int> waypointExists,
            NativeParallelHashMap<int, float> distanceById,
            NativeParallelHashMap<int, int> previousById,
            DynamicBuffer<MobilityPathWaypoint> outputPath,
            Allocator allocator)
        {
            var adjacency = new NativeParallelMultiHashMap<int, HighwayEdge>(math.max(1, highways.Length * 2), allocator);
            for (int i = 0; i < highways.Length; i++)
            {
                var h = highways[i];
                if ((h.Flags & (byte)HighwayFlags.Blocked) != 0)
                {
                    continue;
                }

                adjacency.Add(h.FromWaypointId, new HighwayEdge { To = h.ToWaypointId, Cost = h.Cost > 0f ? h.Cost : math.max(0.01f, h.TravelTime) });
                adjacency.Add(h.ToWaypointId, new HighwayEdge { To = h.FromWaypointId, Cost = h.Cost > 0f ? h.Cost : math.max(0.01f, h.TravelTime) });
            }

            var openList = new NativeList<int>(allocator);

            for (int i = 0; i < waypoints.Length; i++)
            {
                var waypointId = waypoints[i].WaypointId;
                distanceById[waypointId] = float.PositiveInfinity;
                previousById.Remove(waypointId);
            }

            distanceById[fromId] = 0f;
            openList.Add(fromId);

            while (openList.Length > 0)
            {
                // Pick lowest cost node in open list.
                var currentIndex = 0;
                var currentId = openList[0];
                var currentCost = distanceById[currentId];
                for (int i = 1; i < openList.Length; i++)
                {
                    var candidateId = openList[i];
                    var candidateCost = distanceById[candidateId];
                    if (candidateCost < currentCost)
                    {
                        currentCost = candidateCost;
                        currentId = candidateId;
                        currentIndex = i;
                    }
                }

                // Remove current from open list
                openList.RemoveAtSwapBack(currentIndex);

                if (currentId == toId)
                {
                    break;
                }

                if (!adjacency.TryGetFirstValue(currentId, out var edge, out var it))
                {
                    continue;
                }

                do
                {
                    if (!waypointExists.Contains(edge.To))
                    {
                        continue;
                    }

                    var tentative = currentCost + edge.Cost;
                    if (!distanceById.TryGetValue(edge.To, out var oldCost) || tentative + 0.0001f < oldCost)
                    {
                        distanceById[edge.To] = tentative;
                        previousById[edge.To] = currentId;
                        if (!ListContains(openList, edge.To))
                        {
                            openList.Add(edge.To);
                        }
                    }
                } while (adjacency.TryGetNextValue(out edge, ref it));
            }

            if (!distanceById.TryGetValue(toId, out var finalCost) || float.IsPositiveInfinity(finalCost))
            {
                outputPath.Clear();
                adjacency.Dispose();
                openList.Dispose();
                return -1f;
            }

            // Reconstruct path.
            var path = new NativeList<int>(allocator);
            var walker = toId;
            path.Add(walker);
            while (previousById.TryGetValue(walker, out var prev))
            {
                walker = prev;
                path.Add(walker);
                if (walker == fromId)
                {
                    break;
                }
            }

            outputPath.Clear();
            for (int i = path.Length - 1; i >= 0; i--)
            {
                outputPath.Add(new MobilityPathWaypoint { WaypointId = path[i] });
            }

            adjacency.Dispose();
            openList.Dispose();
            path.Dispose();
            return finalCost;
        }

        private struct HighwayEdge
        {
            public int To;
            public float Cost;
        }

        private static bool ListContains(NativeList<int> list, int value)
        {
            for (int i = 0; i < list.Length; i++)
            {
                if (list[i] == value)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
