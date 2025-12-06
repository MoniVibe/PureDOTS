using PureDOTS.Runtime.AI;
using PureDOTS.Runtime.Bridges;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Core;
using PureDOTS.Systems;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Runtime.AI.Cognitive.Systems.Reflex
{
    /// <summary>
    /// Reflex system - 60Hz reactive sensor→action mapping.
    /// Pure reactive layer with no learning, instant response to sensor inputs.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(ReflexSystemGroup))]
    public partial struct ReflexSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RewindState>();
            state.RequireForUpdate<TickTimeState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var rewind = SystemAPI.GetSingleton<RewindState>();
            if (rewind.Mode != RewindMode.Record && rewind.Mode != RewindMode.CatchUp)
            {
                return;
            }

            var tickTime = SystemAPI.GetSingleton<TickTimeState>();
            if (tickTime.IsPaused)
            {
                return;
            }

            var job = new ReflexJob
            {
                CurrentTick = tickTime.Tick
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        private partial struct ReflexJob : IJobEntity
        {
            public uint CurrentTick;

            public void Execute(
                Entity entity,
                [ChunkIndexInQuery] int chunkIndex,
                in LocalTransform transform,
                in DynamicBuffer<AISensorReading> sensorReadings,
                ref DynamicBuffer<AgentIntentBuffer> intentBuffer)
            {
                if (sensorReadings.Length == 0)
                {
                    return;
                }

                // Find highest priority sensor reading
                float bestScore = float.MinValue;
                Entity bestTarget = Entity.Null;
                float3 bestPosition = float3.zero;

                for (int i = 0; i < sensorReadings.Length; i++)
                {
                    var reading = sensorReadings[i];
                    if (reading.NormalizedScore > bestScore)
                    {
                        bestScore = reading.NormalizedScore;
                        bestTarget = reading.Target;
                        // Use transform position as fallback if target entity position unavailable
                        bestPosition = transform.Position;
                    }
                }

                // Generate reactive intent based on sensor reading
                if (bestTarget != Entity.Null && bestScore > 0.1f)
                {
                    intentBuffer.Add(new AgentIntentBuffer
                    {
                        Kind = IntentKind.Move,
                        TargetPosition = bestPosition,
                        TargetEntity = bestTarget,
                        Priority = (byte)math.clamp((int)(bestScore * 255f), 0, 255),
                        TickNumber = CurrentTick
                    });
                }
            }
        }
    }
}

