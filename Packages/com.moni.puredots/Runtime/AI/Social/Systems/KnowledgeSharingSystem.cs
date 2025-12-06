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
    /// Knowledge sharing system for Body ECS.
    /// Handles Request/ShareKnowledge messages.
    /// Shares if trust > 0.7.
    /// </summary>
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    [UpdateAfter(typeof(CooperationSystemManaged))]
    [BurstCompile]
    public partial struct KnowledgeSharingSystem : ISystem
    {
        private const float TrustThreshold = 0.7f; // Minimum trust for knowledge sharing

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

            // Knowledge sharing processing handled in managed wrapper
        }
    }

    /// <summary>
    /// Managed wrapper for KnowledgeSharingSystem that accesses AgentSyncBus.
    /// Processes knowledge sharing requests.
    /// </summary>
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    [UpdateAfter(typeof(CooperationSystemManaged))]
    public sealed partial class KnowledgeSharingSystemManaged : SystemBase
    {
        private float _lastUpdateTime;
        private const float UpdateInterval = 0.2f; // 5 Hz updates

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

            // Process knowledge sharing messages in Burst job
            var job = new ProcessKnowledgeSharingMessagesJob
            {
                TickNumber = tickNumber,
                TrustThreshold = KnowledgeSharingSystem.TrustThreshold
            };

            var entityQuery = GetEntityQuery(typeof(AgentSyncId), typeof(SocialKnowledge), typeof(SocialRelationship));
            job.ScheduleParallel(entityQuery, Dependency).Complete();

            _lastUpdateTime = currentTime;
        }
    }

    [BurstCompile]
    private partial struct ProcessKnowledgeSharingMessagesJob : IJobEntity
    {
        public uint TickNumber;
        public float TrustThreshold;

        public void Execute(
            Entity entity,
            ref SocialKnowledge socialKnowledge,
            DynamicBuffer<SocialRelationship> relationships)
        {
            // Process knowledge sharing messages (Request, ShareKnowledge)
            // In full implementation, would:
            // 1. Check trust level for sender
            // 2. If trust > threshold, share knowledge
            // 3. Update trust/reputation based on sharing outcome

            // Simplified: check relationships for trust levels
            for (int i = 0; i < relationships.Length; i++)
            {
                var relationship = relationships[i];
                
                if (relationship.Trust > TrustThreshold)
                {
                    // Trust sufficient for knowledge sharing
                    // Would trigger knowledge transfer
                }
            }
        }
    }
}

