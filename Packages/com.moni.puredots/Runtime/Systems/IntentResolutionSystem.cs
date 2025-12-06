using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Bridges;
using PureDOTS.Runtime.Core;

namespace PureDOTS.Systems
{
    /// <summary>
    /// Burst-compiled system that reads AgentIntentBuffer (from Mind ECS) and IntentCommand,
    /// applying Burst-safe changes. Updates movement, combat, resource gathering based on intents.
    /// Maintains determinism.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(Bridges.MindToBodySyncSystem))]
    public partial struct IntentResolutionSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TickTimeState>();
            // System works with either IntentCommand or AgentIntentBuffer
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TickTimeState>();
            
            // Process AgentIntentBuffer (from Mind ECS via bridge)
            foreach (var (intentBuffer, transform, entity) in SystemAPI.Query<
                         DynamicBuffer<AgentIntentBuffer>,
                         RefRW<LocalTransform>>()
                         .WithEntityAccess())
            {
                if (intentBuffer.IsEmpty)
                    continue;

                // Process most recent intent (last in buffer)
                var intentCmd = intentBuffer[intentBuffer.Length - 1];
                ProcessIntent(ref transform.ValueRW, intentCmd, timeState.Tick);
                
                // Clear processed intents
                intentBuffer.Clear();
            }

            // Also process legacy IntentCommand component (for backwards compatibility)
            foreach (var (intent, transform, entity) in SystemAPI.Query<
                         RefRO<IntentCommand>,
                         RefRW<LocalTransform>>()
                         .WithEntityAccess())
            {
                var intentCmd = intent.ValueRO;
                ProcessIntent(ref transform.ValueRW, intentCmd, timeState.Tick);
            }
        }

        [BurstCompile]
        private void ProcessIntent(ref LocalTransform transform, AgentIntentBuffer intentCmd, uint currentTick)
        {
            // Only process intents from current or recent ticks
            if (intentCmd.TickNumber < currentTick - 10)
            {
                return; // Stale intent, skip
            }

            var tickTimeState = SystemAPI.GetSingleton<TickTimeState>();
            var fixedDeltaTime = tickTimeState.FixedDeltaTime;

            switch (intentCmd.Kind)
            {
                case IntentKind.Move:
                    // Update position towards target
                    var direction = intentCmd.TargetPosition - transform.Position;
                    var distance = math.length(direction);
                    
                    if (distance > 0.1f)
                    {
                        var moveSpeed = 5f; // TODO: Get from agent stats
                        var moveDistance = math.min(moveSpeed * fixedDeltaTime, distance);
                        var normalizedDir = direction / distance;
                        transform.Position += normalizedDir * moveDistance;
                        
                        // Update rotation to face movement direction
                        if (math.lengthsq(normalizedDir) > 0.01f)
                        {
                            transform.Rotation = quaternion.LookRotationSafe(normalizedDir, math.up());
                        }
                    }
                    break;

                case IntentKind.Attack:
                    // TODO: Implement attack logic
                    // For now, move towards target
                    if (intentCmd.TargetEntity != Entity.Null)
                    {
                        // Move towards target entity
                        // Attack logic will be handled by combat systems
                    }
                    break;

                case IntentKind.Harvest:
                    // TODO: Implement harvest logic
                    // Move towards resource and interact
                    break;

                case IntentKind.Rest:
                    // TODO: Implement rest logic
                    // Stop movement, restore energy
                    break;

                case IntentKind.Flee:
                    // Move away from target
                    var fleeDirection = transform.Position - intentCmd.TargetPosition;
                    var fleeDistance = math.length(fleeDirection);
                    
                    if (fleeDistance > 0.1f)
                    {
                        var fleeSpeed = 7f; // Faster than normal move
                        var normalizedFleeDir = fleeDirection / fleeDistance;
                        transform.Position += normalizedFleeDir * fleeSpeed * fixedDeltaTime;
                        transform.Rotation = quaternion.LookRotationSafe(normalizedFleeDir, math.up());
                    }
                    break;

                case IntentKind.None:
                default:
                    // No action
                    break;
            }
        }

        [BurstCompile]
        private void ProcessIntent(ref LocalTransform transform, IntentCommand intentCmd, uint currentTick)
        {
            // Convert IntentCommand to AgentIntentBuffer format for processing
            var bufferIntent = new AgentIntentBuffer
            {
                Kind = intentCmd.Kind,
                TargetPosition = intentCmd.TargetPosition,
                TargetEntity = intentCmd.TargetEntity,
                Priority = intentCmd.Priority,
                TickNumber = intentCmd.TickNumber
            };
            ProcessIntent(ref transform, bufferIntent, currentTick);
            }
        }
    }
}

