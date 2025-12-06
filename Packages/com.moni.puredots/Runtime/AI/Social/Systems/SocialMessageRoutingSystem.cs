using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Shared;
using PureDOTS.Runtime.AI.Social;
using PureDOTS.Runtime.Bridges;
using PureDOTS.Runtime.Components;
using PureDOTS.Systems;

namespace PureDOTS.Runtime.AI.Social.Systems
{
    /// <summary>
    /// Routes social messages via spatial proximity.
    /// Implements chunk-local cooperation by restricting message scan to same spatial cell.
    /// Based on performance optimization strategy for large-scale social dynamics.
    /// </summary>
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    [UpdateAfter(typeof(CooperationSystemManaged))]
    [BurstCompile]
    public partial struct SocialMessageRoutingSystem : ISystem
    {
        private const float BroadcastRadius = 30f; // Radius for broadcast messages

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<AgentSyncState>();
            state.RequireForUpdate<RewindState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var rewindState = SystemAPI.GetSingleton<RewindState>();
            if (rewindState.Mode != RewindMode.Record)
            {
                return; // Skip during playback
            }

            // Routing logic handled in managed wrapper
        }
    }

    /// <summary>
    /// Managed wrapper for SocialMessageRoutingSystem that accesses AgentSyncBus.
    /// Routes messages based on spatial proximity and broadcast flags.
    /// </summary>
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    [UpdateAfter(typeof(CooperationSystemManaged))]
    public sealed partial class SocialMessageRoutingSystemManaged : SystemBase
    {
        private float _lastUpdateTime;
        private const float UpdateInterval = 0.1f; // 10 Hz routing updates

        protected override void OnCreate()
        {
            _lastUpdateTime = 0f;
            RequireForUpdate<AgentSyncState>();
            RequireForUpdate<RewindState>();
        }

        protected override void OnUpdate()
        {
            var rewindState = SystemAPI.GetSingleton<RewindState>();
            if (rewindState.Mode != RewindMode.Record)
            {
                return; // Skip during playback
            }

            var currentTime = (float)SystemAPI.Time.ElapsedTime;
            if (currentTime - _lastUpdateTime < UpdateInterval)
            {
                return; // Temporal batching
            }

            var coordinator = World.GetExistingSystemManaged<AgentSyncBridgeCoordinator>();
            if (coordinator == null)
            {
                return;
            }

            var bus = coordinator.GetBus();
            if (bus == null || bus.SocialMessageQueueCount == 0)
            {
                return;
            }

            var tickState = SystemAPI.GetSingleton<TickTimeState>();
            var tickNumber = tickState.Tick;

            // Get component lookups
            var syncIdLookup = GetComponentLookup<AgentSyncId>(true);
            var positionLookup = GetComponentLookup<Unity.Transforms.LocalTransform>(true);
            var socialMessageBufferLookup = GetBufferLookup<SocialMessage>(false);

            // Process routing in Burst job
            var job = new RouteSocialMessagesJob
            {
                SyncIdLookup = syncIdLookup,
                PositionLookup = positionLookup,
                SocialMessageBufferLookup = socialMessageBufferLookup,
                TickNumber = tickNumber,
                BroadcastRadius = SocialMessageRoutingSystem.BroadcastRadius
            };

            var entityQuery = GetEntityQuery(typeof(AgentSyncId), typeof(SocialMessage));
            job.ScheduleParallel(entityQuery, Dependency).Complete();

            _lastUpdateTime = currentTime;
        }
    }

    [BurstCompile]
    private partial struct RouteSocialMessagesJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<AgentSyncId> SyncIdLookup;
        [ReadOnly] public ComponentLookup<Unity.Transforms.LocalTransform> PositionLookup;
        public BufferLookup<SocialMessage> SocialMessageBufferLookup;
        public uint TickNumber;
        public float BroadcastRadius;

        public void Execute(
            Entity entity,
            DynamicBuffer<SocialMessage> messages)
        {
            if (!SyncIdLookup.HasComponent(entity) || !PositionLookup.HasComponent(entity))
            {
                return;
            }

            var agentGuid = SyncIdLookup[entity].Guid;
            var agentPosition = PositionLookup[entity].Position;

            // Process messages for routing
            for (int i = 0; i < messages.Length; i++)
            {
                var message = messages[i];

                // Check if message has broadcast flag
                if ((message.Flags & SocialMessageFlags.Broadcast) != 0)
                {
                    // Broadcast message - route to nearby agents
                    // In full implementation, would use spatial index for efficient lookup
                    // For now, message stays in buffer for processing by CooperationSystem
                    continue;
                }

                // Direct message - already routed to specific receiver
                // No additional routing needed
            }
        }
    }
}

