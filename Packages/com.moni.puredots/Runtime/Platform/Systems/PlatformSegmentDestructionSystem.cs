using PureDOTS.Runtime.Platform;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Systems.Platform
{
    /// <summary>
    /// Detects when segments are destroyed and queues connectivity updates.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PlatformHitResolutionSystem))]
    public partial struct PlatformSegmentDestructionSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (transform, segmentStates, entity) in SystemAPI.Query<
                RefRO<LocalTransform>,
                DynamicBuffer<PlatformSegmentState>>().WithEntityAccess())
            {
                bool needsUpdate = false;

                for (int i = 0; i < segmentStates.Length; i++)
                {
                    var segmentState = segmentStates[i];
                    if (segmentState.HP <= 0f && (segmentState.Status & SegmentStatusFlags.Destroyed) == 0)
                    {
                        segmentState.Status |= SegmentStatusFlags.Destroyed;
                        segmentStates[i] = segmentState;
                        needsUpdate = true;

                        EmitSegmentDestroyedEvent(ref ecb, entity, segmentState.SegmentIndex, transform.ValueRO.Position);
                    }
                }

                if (needsUpdate)
                {
                    if (!SystemAPI.HasComponent<NeedConnectivityUpdate>(entity))
                    {
                        ecb.AddComponent<NeedConnectivityUpdate>(entity);
                    }
                }
            }
        }

        [BurstCompile]
        private static void EmitSegmentDestroyedEvent(
            ref EntityCommandBuffer ecb,
            Entity platformEntity,
            int segmentIndex,
            float3 worldPosition)
        {
            ecb.AddBuffer<PlatformSegmentDestroyedEvent>(platformEntity);
            var eventBuffer = ecb.SetBuffer<PlatformSegmentDestroyedEvent>(platformEntity);
            eventBuffer.Add(new PlatformSegmentDestroyedEvent
            {
                PlatformEntity = platformEntity,
                SegmentIndex = segmentIndex,
                WorldPosition = worldPosition
            });
        }
    }

    /// <summary>
    /// Tag component indicating platform needs connectivity update.
    /// </summary>
    public struct NeedConnectivityUpdate : IComponentData
    {
    }
}

