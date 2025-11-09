using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Hand;
using PureDOTS.Systems.Input;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Systems
{
    /// <summary>
    /// Centralises RMB routing by resolving the highest-priority hand interaction request each frame.
    /// Now consumes GodIntent to gate routing deterministically based on player intent.
    /// Downstream systems rely on the resolved <see cref="DivineHandCommand"/> without duplicating priority logic.
    /// Note: Runs in HandSystemGroup, which executes after SimulationSystemGroup where IntentMappingSystem runs.
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
            foreach (var (requests, resultRef, commandRef, intentRef, entity) in SystemAPI
                         .Query<DynamicBuffer<HandInputRouteRequest>, RefRW<HandInputRouteResult>, RefRW<DivineHandCommand>, RefRO<GodIntent>>()
                         .WithEntityAccess())
            {
                var result = resultRef.ValueRO;
                var command = commandRef.ValueRW;
                var intent = intentRef.ValueRO;

                // Gate routing based on intent
                // If intent says cancel, clear command
                if (intent.CancelAction != 0)
                {
                    command.Type = DivineHandCommandType.None;
                    command.TargetEntity = Entity.Null;
                    command.TargetPosition = float3.zero;
                    command.TargetNormal = new float3(0f, 1f, 0f);
                    command.TimeSinceIssued = 0f;
                    resultRef.ValueRW = HandInputRouteResult.None;
                    requests.Clear();
                    continue;
                }

                // Only resolve routes if intent allows selection
                // (This gates routing during playback/rewind, etc.)
                if (intent.StartSelect == 0 && intent.ConfirmPlace == 0)
                {
                    // No active intent, but still resolve existing requests for highlights
                    var resolved = ResolveRoute(requests, result);
                    resultRef.ValueRW = resolved;
                    requests.Clear();
                    continue;
                }

                var resolvedRoute = ResolveRoute(requests, result);
                var commandChanged = resolvedRoute.CommandType != command.Type ||
                                     resolvedRoute.TargetEntity != command.TargetEntity;

                command.Type = resolvedRoute.CommandType;
                command.TargetEntity = resolvedRoute.TargetEntity;
                command.TargetPosition = resolvedRoute.TargetPosition;
                command.TargetNormal = resolvedRoute.TargetNormal;
                if (commandChanged)
                {
                    command.TimeSinceIssued = 0f;
                }

                resultRef.ValueRW = resolvedRoute;
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
