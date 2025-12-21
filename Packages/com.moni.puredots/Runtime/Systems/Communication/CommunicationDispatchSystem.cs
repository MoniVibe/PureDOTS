using PureDOTS.Runtime.Communication;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Core;
using PureDOTS.Runtime.Perception;
using PureDOTS.Systems;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Systems.Communication
{
    /// <summary>
    /// Dispatches queued comm attempts to receivers, applying medium + endpoint gating.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(PerceptionSystemGroup))]
    [UpdateAfter(typeof(CommunicationEndpointBootstrapSystem))]
    public partial struct CommunicationDispatchSystem : ISystem
    {
        private ComponentLookup<CommEndpoint> _endpointLookup;
        private ComponentLookup<MediumContext> _mediumLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
            state.RequireForUpdate<SimulationFeatureFlags>();
            state.RequireForUpdate<CommEndpoint>();

            _endpointLookup = state.GetComponentLookup<CommEndpoint>(true);
            _mediumLookup = state.GetComponentLookup<MediumContext>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var features = SystemAPI.GetSingleton<SimulationFeatureFlags>();
            if ((features.Flags & SimulationFeatureFlags.CommsEnabled) == 0)
            {
                return;
            }

            var timeState = SystemAPI.GetSingleton<TimeState>();
            if (timeState.IsPaused)
            {
                return;
            }

            if (!SystemAPI.TryGetSingleton<RewindState>(out var rewindState) || rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            _endpointLookup.Update(ref state);
            _mediumLookup.Update(ref state);

            foreach (var (endpoint, attempts, sender) in SystemAPI.Query<RefRO<CommEndpoint>, DynamicBuffer<CommAttempt>>()
                .WithEntityAccess())
            {
                if (attempts.Length == 0)
                {
                    continue;
                }

                var senderMedium = ResolveMedium(sender);
                var senderChannels = endpoint.ValueRO.SupportedChannels;

                for (int i = 0; i < attempts.Length; i++)
                {
                    var attempt = attempts[i];
                    var receiver = attempt.Receiver;
                    if (receiver == Entity.Null || !state.EntityManager.Exists(receiver))
                    {
                        continue;
                    }

                    if (!_endpointLookup.HasComponent(receiver))
                    {
                        continue;
                    }

                    var receiverEndpoint = _endpointLookup[receiver];
                    var receiverMedium = ResolveMedium(receiver);

                    var mask = attempt.TransportMask == PerceptionChannel.None
                        ? senderChannels
                        : attempt.TransportMask;

                    mask &= senderChannels;
                    mask &= receiverEndpoint.SupportedChannels;
                    mask = MediumUtilities.FilterChannels(senderMedium, mask);
                    mask = MediumUtilities.FilterChannels(receiverMedium, mask);

                    if (mask == PerceptionChannel.None)
                    {
                        continue;
                    }

                    var channel = SelectPrimaryChannel(mask);
                    var clarity = attempt.Clarity > 0f ? attempt.Clarity : endpoint.ValueRO.BaseClarity;
                    clarity = math.saturate(clarity);
                    var integrity = math.saturate(clarity - attempt.DeceptionStrength - receiverEndpoint.NoiseFloor);
                    var wasDeceptive = (byte)((attempt.DeceptionStrength > 0f && integrity < clarity) ? 1 : 0);

                    if (!state.EntityManager.HasBuffer<CommReceipt>(receiver))
                    {
                        continue;
                    }

                    var receiptBuffer = state.EntityManager.GetBuffer<CommReceipt>(receiver);
                    receiptBuffer.Add(new CommReceipt
                    {
                        Sender = sender,
                        Channel = channel,
                        Method = attempt.Method,
                        Intent = attempt.Intent,
                        PayloadId = attempt.PayloadId,
                        Integrity = integrity,
                        WasDeceptive = wasDeceptive,
                        Timestamp = attempt.Timestamp == 0 ? timeState.Tick : attempt.Timestamp
                    });
                }

                attempts.Clear();
            }
        }

        private MediumType ResolveMedium(Entity entity)
        {
            return _mediumLookup.HasComponent(entity)
                ? _mediumLookup[entity].Type
                : MediumType.Gas;
        }

        private static PerceptionChannel SelectPrimaryChannel(PerceptionChannel mask)
        {
            for (int bit = 0; bit < 32; bit++)
            {
                var channel = (PerceptionChannel)(1u << bit);
                if ((mask & channel) != 0)
                {
                    return channel;
                }
            }

            return PerceptionChannel.None;
        }
    }
}
