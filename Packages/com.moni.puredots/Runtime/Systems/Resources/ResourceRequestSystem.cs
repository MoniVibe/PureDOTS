using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Resources;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Systems.Resources
{
    /// <summary>
    /// System that creates and manages resource requests.
    /// Agents/groups can create requests for resources they need.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct ResourceRequestSystem : ISystem
    {
        private Entity _requestIdGeneratorEntity;
        private EntityQuery _requestIdGeneratorQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            _requestIdGeneratorQuery = state.GetEntityQuery(ComponentType.ReadOnly<ResourceRequestIdGenerator>());
            EnsureRequestIdGeneratorExists(ref state);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton<TimeState>(out var timeState))
            {
                return;
            }

            var currentTick = timeState.Tick;

            // Phase 2: Basic request management
            // Clean up fulfilled/expired requests
            foreach (var (requests, entity) in SystemAPI.Query<DynamicBuffer<NeedRequest>>().WithEntityAccess())
            {
                var requestsBuffer = requests;
                for (int i = requestsBuffer.Length - 1; i >= 0; i--)
                {
                    var request = requestsBuffer[i];
                    
                    // Remove expired requests (older than 1000 ticks)
                    if (currentTick - request.CreatedTick > 1000)
                    {
                        requestsBuffer.RemoveAt(i);
                        continue;
                    }

                    // Phase 2: Will check for fulfillment and remove fulfilled requests
                    // For now, requests persist until expired
                }
            }
        }

        /// <summary>
        /// Creates a new resource request.
        /// </summary>
        public void CreateRequest(
            ref SystemState state,
            ref DynamicBuffer<NeedRequest> requests,
            FixedString32Bytes resourceTypeId,
            float amount,
            Entity requester,
            float priority,
            uint currentTick,
            Entity targetEntity = default)
        {
            var requestId = GetNextRequestId(ref state);
            requests.Add(new NeedRequest
            {
                ResourceTypeId = resourceTypeId,
                Amount = amount,
                RequesterEntity = requester,
                Priority = priority,
                CreatedTick = currentTick,
                TargetEntity = targetEntity,
                RequestId = requestId
            });
        }

        private void EnsureRequestIdGeneratorExists(ref SystemState state)
        {
            var em = state.EntityManager;
            if (_requestIdGeneratorEntity != Entity.Null && em.Exists(_requestIdGeneratorEntity))
            {
                return;
            }

            if (!_requestIdGeneratorQuery.IsEmptyIgnoreFilter)
            {
                _requestIdGeneratorEntity = _requestIdGeneratorQuery.GetSingletonEntity();
                return;
            }

            _requestIdGeneratorEntity = em.CreateEntity(ComponentType.ReadOnly<ResourceRequestIdGenerator>());
            em.SetComponentData(_requestIdGeneratorEntity, new ResourceRequestIdGenerator
            {
                NextRequestId = 1
            });
        }

        private uint GetNextRequestId(ref SystemState state)
        {
            var em = state.EntityManager;
            
            if (_requestIdGeneratorEntity == Entity.Null || !em.Exists(_requestIdGeneratorEntity))
            {
                EnsureRequestIdGeneratorExists(ref state);
            }

            var generator = em.GetComponentData<ResourceRequestIdGenerator>(_requestIdGeneratorEntity);
            var requestId = generator.NextRequestId == 0 ? 1u : generator.NextRequestId;
            generator.NextRequestId = requestId + 1;
            em.SetComponentData(_requestIdGeneratorEntity, generator);
            return requestId;
        }
    }
}

