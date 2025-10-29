using PureDOTS.Runtime.Components;
using Unity.Burst;
using Unity.Entities;

namespace PureDOTS.Systems
{
    /// <summary>
    /// Centralises RMB routing by resolving the highest-priority hand interaction request each frame.
    /// Downstream systems rely on the resolved <see cref="DivineHandCommand"/> without duplicating priority logic.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(HandSystemGroup), OrderFirst = true)]
    public partial struct HandInputRouterSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<HandInputRouteResult>();
            state.RequireForUpdate<DivineHandCommand>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (requests, resultRef, commandRef, entity) in SystemAPI
                         .Query<DynamicBuffer<HandInputRouteRequest>, RefRW<HandInputRouteResult>, RefRW<DivineHandCommand>>()
                         .WithEntityAccess())
            {
                var result = resultRef.ValueRO;
                var command = commandRef.ValueRW;

                var resolved = ResolveRoute(requests, result);
                var commandChanged = resolved.CommandType != command.Type ||
                                     resolved.TargetEntity != command.TargetEntity;

                command.Type = resolved.CommandType;
                command.TargetEntity = resolved.TargetEntity;
                command.TargetPosition = resolved.TargetPosition;
                command.TargetNormal = resolved.TargetNormal;
                if (commandChanged)
                {
                    command.TimeSinceIssued = 0f;
                }

                resultRef.ValueRW = resolved;
                commandRef.ValueRW = command;
                requests.Clear();
            }
        }

        static HandInputRouteResult ResolveRoute(DynamicBuffer<HandInputRouteRequest> requests, in HandInputRouteResult current)
        {
            var best = current;
            bool hasCandidate = false;

            for (int i = 0; i < requests.Length; i++)
            {
                var request = requests[i];
                if (request.Phase == HandRoutePhase.Canceled)
                {
                    if (best.CommandType == request.CommandType && best.Source == request.Source)
                    {
                        best = HandInputRouteResult.None;
                        hasCandidate = true;
                    }
                    continue;
                }

                if (!hasCandidate || request.Priority > best.Priority ||
                    (request.Priority == best.Priority && request.Source > best.Source))
                {
                    best = new HandInputRouteResult
                    {
                        Source = request.Source,
                        Priority = request.Priority,
                        CommandType = request.CommandType,
                        TargetEntity = request.TargetEntity,
                        TargetPosition = request.TargetPosition,
                        TargetNormal = request.TargetNormal
                    };
                    hasCandidate = true;
                }
            }

            return hasCandidate ? best : HandInputRouteResult.None;
        }
    }
}
