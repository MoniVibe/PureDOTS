using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using PureDOTS.Shared;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Bridges;
using PureDOTS.Runtime.Core;

namespace PureDOTS.Runtime.Bridges
{
    /// <summary>
    /// Reads Body ECS state and writes messages to Mind ECS.
    /// Batches updates per sync interval (100ms default) with delta compression.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(AgentMappingSystem))]
    public sealed partial class BodyToMindSyncSystem : SystemBase
    {
        private float _lastSyncTime;
        private const float DefaultSyncInterval = 0.1f; // 100ms

        protected override void OnCreate()
        {
            _lastSyncTime = 0f;
            RequireForUpdate<AgentSyncState>();
        }

        protected override void OnUpdate()
        {
            var currentTime = (float)SystemAPI.Time.ElapsedTime;
            
            if (currentTime - _lastSyncTime < DefaultSyncInterval)
            {
                return;
            }

            var coordinator = World.GetExistingSystemManaged<AgentSyncBridgeCoordinator>();
            if (coordinator == null)
            {
                return;
            }

            var bus = coordinator.GetBus();
            if (bus == null)
            {
                return;
            }

            var tickState = SystemAPI.GetSingleton<TickTimeState>();
            var tickNumber = tickState.Tick;

            // Collect body state updates in Burst job
            var entityQuery = GetEntityQuery(
                ComponentType.ReadOnly<AgentBody>(),
                ComponentType.ReadOnly<AgentSyncId>(),
                ComponentType.ReadOnly<LocalTransform>());

            var updates = new NativeList<BodyToMindMessage>(entityQuery.CalculateEntityCount(), Allocator.TempJob);
            var needsLookup = GetComponentLookup<VillagerNeeds>(true);
            var membershipLookup = GetComponentLookup<AggregateMembership>(true);
            
            needsLookup.Update(this);
            membershipLookup.Update(this);

            var collectJob = new CollectBodyTelemetryJob
            {
                Updates = updates,
                TickNumber = tickNumber,
                NeedsLookup = needsLookup,
                MembershipLookup = membershipLookup
            };

            collectJob.ScheduleParallel(entityQuery, Dependency).Complete();

            // Enqueue messages to bus (managed operation)
            for (int i = 0; i < updates.Length; i++)
            {
                bus.EnqueueBodyToMind(updates[i]);
            }

            updates.Dispose();
            _lastSyncTime = currentTime;
        }

        [BurstCompile]
        private struct CollectBodyTelemetryJob : IJobEntity
        {
            public NativeList<BodyToMindMessage> Updates;
            public uint TickNumber;
            [ReadOnly] public ComponentLookup<VillagerNeeds> NeedsLookup;
            [ReadOnly] public ComponentLookup<AggregateMembership> MembershipLookup;

            public void Execute(
                [EntityIndexInQuery] int index,
                Entity entity,
                in AgentBody agentBody,
                in AgentSyncId syncId,
                in LocalTransform transform)
            {
                // Only sync if Mind ECS entity exists
                if (syncId.MindEntityIndex < 0)
                {
                    return;
                }

                // Try to get health from VillagerNeeds if available
                float health = 100f;
                float maxHealth = 100f;
                if (NeedsLookup.HasComponent(entity))
                {
                    var needs = NeedsLookup[entity];
                    health = needs.Health;
                    maxHealth = 100f; // Default max health
                }

                // Get aggregate membership if available
                AgentGuid aggregateGuid = default;
                if (MembershipLookup.HasComponent(entity))
                {
                    var membership = MembershipLookup[entity];
                    aggregateGuid = membership.AggregateGuid;
                }

                var message = new BodyToMindMessage
                {
                    AgentGuid = syncId.Guid,
                    Position = transform.Position,
                    Rotation = transform.Rotation,
                    Health = health,
                    MaxHealth = maxHealth,
                    Flags = 0, // Will be set by sync bus delta compression
                    TickNumber = TickNumber,
                    AggregateGuid = aggregateGuid
                };

                Updates.Add(message);
            }
        }
    }
}

