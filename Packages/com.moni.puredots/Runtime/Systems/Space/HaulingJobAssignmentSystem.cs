using PureDOTS.Runtime.Space;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Systems.Space
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(HaulingJobManagerSystem))]
    public partial struct HaulingJobAssignmentSystem : ISystem
    {
        private HaulingJobManagerSystem _jobManager;
        private ComponentLookup<ResourcePile> _pileLookup;
        private BufferLookup<ResourceValueEntry> _valueLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _pileLookup = state.GetComponentLookup<ResourcePile>(true);
            _valueLookup = state.GetBufferLookup<ResourceValueEntry>(true);
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
            _valueLookup.Update(ref state);

            foreach (var (haulerRole, loopConfig, entity) in SystemAPI
                         .Query<RefRO<HaulerRole>, RefRO<HaulingLoopConfig>>()
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
                        var score = ComputeScore(job, pile, haulerRole.ValueRO, loopConfig.ValueRO);
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

        private float ComputeScore(HaulingJobQueueEntry job, ResourcePile pile, HaulerRole role, HaulingLoopConfig config)
        {
            var speedWeight = config.TravelSpeedMetersPerSecond;
            var cargoFit = config.MaxCargo > 0f ? math.min(1f, job.RequestedAmount / config.MaxCargo) : 0f;
            var roleWeight = role.IsDedicatedFreighter > 0 ? 2f : 1f;
            var valueWeight = job.ResourceValue;
            var urgencyWeight = job.Urgency;
            return roleWeight * speedWeight * valueWeight * urgencyWeight * (0.5f + cargoFit * 0.5f);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }
    }
}
