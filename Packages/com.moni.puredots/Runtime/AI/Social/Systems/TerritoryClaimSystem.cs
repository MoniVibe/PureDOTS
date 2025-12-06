using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Shared;
using PureDOTS.Runtime.AI.Social;
using PureDOTS.Runtime.AI.Social.Systems;
using PureDOTS.Runtime.Bridges;
using PureDOTS.Runtime.Components;
using PureDOTS.Systems;

namespace PureDOTS.Runtime.AI.Social.Systems
{
    /// <summary>
    /// Territory claim system for Body ECS.
    /// Handles Threat/Appeal messages for territorial disputes.
    /// Conflict if utilities don't converge.
    /// </summary>
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    [UpdateAfter(typeof(CooperationSystemManaged))]
    [BurstCompile]
    public partial struct TerritoryClaimSystem : ISystem
    {
        private const float UtilityConvergenceThreshold = 0.1f; // Threshold for utility convergence

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

            // Territory claim processing handled in managed wrapper
        }
    }

    /// <summary>
    /// Managed wrapper for TerritoryClaimSystem that accesses AgentSyncBus.
    /// Processes territorial threats and appeals.
    /// </summary>
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    [UpdateAfter(typeof(CooperationSystemManaged))]
    public sealed partial class TerritoryClaimSystemManaged : SystemBase
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

            // Process territory claim messages in Burst job
            var job = new ProcessTerritoryClaimMessagesJob
            {
                TickNumber = tickNumber,
                UtilityConvergenceThreshold = TerritoryClaimSystem.UtilityConvergenceThreshold
            };

            var entityQuery = GetEntityQuery(typeof(AgentSyncId), typeof(SocialMessage));
            job.ScheduleParallel(entityQuery, Dependency).Complete();

            _lastUpdateTime = currentTime;
        }
    }

    [BurstCompile]
    private partial struct ProcessTerritoryClaimMessagesJob : IJobEntity
    {
        public uint TickNumber;
        public float UtilityConvergenceThreshold;

        public void Execute(Entity entity, DynamicBuffer<SocialMessage> messages)
        {
            // Process territory claim messages (Threat, Appeal)
            for (int i = 0; i < messages.Length; i++)
            {
                var message = messages[i];

                if (message.Type == SocialMessageType.Threat || message.Type == SocialMessageType.Appeal)
                {
                    // Check if utilities converge
                    var utilityDifference = math.abs(message.Payload - (message.Payload * 0.8f)); // Simplified

                    if (utilityDifference > UtilityConvergenceThreshold)
                    {
                        // Conflict - utilities don't converge
                        // Would trigger conflict resolution or territorial dispute
                    }
                    else
                    {
                        // Utilities converge - peaceful resolution possible
                    }
                }
            }
        }
    }
}

