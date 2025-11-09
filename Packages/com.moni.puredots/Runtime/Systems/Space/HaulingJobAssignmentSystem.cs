using PureDOTS.Runtime.Space;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Systems.Space
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(HaulingJobManagerSystem))]
    public partial struct HaulingJobAssignmentSystem : ISystem
    {
        private HaulingJobManagerSystem _jobManager;
        private ComponentLookup<ResourcePile> _pileLookup;
        private ComponentLookup<LocalTransform> _transformLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _pileLookup = state.GetComponentLookup<ResourcePile>(true);
            _transformLookup = state.GetComponentLookup<LocalTransform>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _jobManager = state.WorldUnmanaged.GetExistingSystemState<HaulingJobManagerSystem>().GetManagedSystem<HaulingJobManagerSystem>();
            var queueLookup = _jobManager.GetQueueLookup(ref state);
            var queueEntity = _jobManager.GetQueueEntity();
            if (!queueLookup.HasBuffer(queueEntity))
            {
                return;
            }

            var queue = queueLookup[queueEntity];
            if (queue.Length == 0)
            {
                return;
            }

            _pileLookup.Update(ref state);
            _transformLookup.Update(ref state);

            foreach (var (haulerRole, loopConfig, transform, entity) in SystemAPI
                         .Query<RefRO<HaulerRole>, RefRO<HaulingLoopConfig>, RefRO<LocalTransform>>()
                         .WithEntityAccess())
            {
                if (!SystemAPI.HasComponent<HaulingJob>(entity))
                {
                    var bestIndex = -1;
                    var bestScore = float.MinValue;
                    for (int i = 0; i < queue.Length; i++)
                    {
                        var job = queue[i];
                        if (job.SourceEntity == Entity.Null || !_pileLookup.HasComponent(job.SourceEntity))
                        {
                            continue;
                        }

                        var pile = _pileLookup[job.SourceEntity];
                        var score = ComputeScore(job, pile, haulerRole.ValueRO, loopConfig.ValueRO, transform.ValueRO.Position);
                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestIndex = i;
                        }
                    }

                    if (bestIndex >= 0)
                    {
                        var job = queue[bestIndex];
                        SystemAPI.SetComponent(entity, new HaulingJob
                        {
                            Priority = job.Priority,
                            SourceEntity = job.SourceEntity,
                            DestinationEntity = job.DestinationEntity,
                            RequestedAmount = job.RequestedAmount,
                            Urgency = job.Urgency,
                            ResourceValue = job.ResourceValue
                        });
                        queue.RemoveAt(bestIndex);
                    }
                }
            }
        }

        private float ComputeScore(HaulingJobQueueEntry job, ResourcePile pile, HaulerRole role, HaulingLoopConfig config, float3 haulerPosition)
        {
            var speedWeight = config.TravelSpeedMetersPerSecond;
            var cargoFit = config.MaxCargo > 0f ? math.min(1f, job.RequestedAmount / config.MaxCargo) : 0f;
            var roleWeight = role.IsDedicatedFreighter > 0 ? 2f : 1f;
            var valueWeight = job.ResourceValue;
            var urgencyWeight = job.Urgency;
            var distanceWeight = math.rcp(1f + math.length(pile.Position - haulerPosition));
            return roleWeight * speedWeight * valueWeight * urgencyWeight * distanceWeight * (0.5f + cargoFit * 0.5f);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }
    }
}
