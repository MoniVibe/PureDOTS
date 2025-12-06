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
    /// Trade system for Body ECS.
    /// Handles Offer/CounterOffer messages for economic cooperation.
    /// Success if mutual profit > threshold.
    /// </summary>
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    [UpdateAfter(typeof(CooperationSystemManaged))]
    [BurstCompile]
    public partial struct TradeSystem : ISystem
    {
        private const float MutualProfitThreshold = 0.3f; // Minimum mutual profit for trade

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

            // Trade processing handled in managed wrapper
        }
    }

    /// <summary>
    /// Managed wrapper for TradeSystem that accesses AgentSyncBus.
    /// Processes trade offers and counter-offers.
    /// </summary>
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    [UpdateAfter(typeof(CooperationSystemManaged))]
    public sealed partial class TradeSystemManaged : SystemBase
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

            // Process trade messages in Burst job
            var job = new ProcessTradeMessagesJob
            {
                TickNumber = tickNumber,
                MutualProfitThreshold = TradeSystem.MutualProfitThreshold
            };

            var entityQuery = GetEntityQuery(typeof(AgentSyncId), typeof(SocialMessage));
            job.ScheduleParallel(entityQuery, Dependency).Complete();

            _lastUpdateTime = currentTime;
        }
    }

    [BurstCompile]
    private partial struct ProcessTradeMessagesJob : IJobEntity
    {
        public uint TickNumber;
        public float MutualProfitThreshold;

        public void Execute(Entity entity, DynamicBuffer<SocialMessage> messages)
        {
            // Process trade messages (Offer, CounterOffer, Accept, Reject)
            for (int i = 0; i < messages.Length; i++)
            {
                var message = messages[i];

                if (message.Type == SocialMessageType.Offer || message.Type == SocialMessageType.CounterOffer)
                {
                    // Calculate mutual profit
                    var mutualGain = message.Payload;
                    var shouldAccept = CooperationResolutionSystem.CalculateMutualUtility(
                        message.Payload,
                        message.Payload * 0.8f, // Receiver's utility estimate
                        MutualProfitThreshold,
                        out var gain);

                    if (shouldAccept)
                    {
                        // Trade successful - would update resources, trust, etc.
                        // For now, mark message as processed
                    }
                }
            }
        }
    }
}

