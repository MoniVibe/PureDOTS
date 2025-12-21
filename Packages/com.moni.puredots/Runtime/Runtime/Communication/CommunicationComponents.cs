using Unity.Collections;
using Unity.Entities;
using PureDOTS.Runtime.Perception;

namespace PureDOTS.Runtime.Communication
{
    public enum CommunicationMethod : byte
    {
        NativeLanguage = 0,
        KnownLanguage = 1,
        GeneralSigns = 2,
        Empathy = 3,
        Telepathy = 4,
        FailedCommunication = 5
    }

    public enum CommunicationIntent : byte
    {
        Greeting = 0,
        Farewell = 1,
        Gratitude = 2,
        Apology = 3,
        Threat = 4,
        Submission = 5,
        WillingToTrade = 10,
        UnwillingToTrade = 11,
        TradeOfferSpecific = 12,
        TradeRequestSpecific = 13,
        PriceNegotiation = 14,
        AskForDirections = 20,
        ProvideDirections = 21,
        AskForKnowledge = 22,
        ShareKnowledge = 23,
        Warning = 24,
        Rumor = 25,
        PeacefulIntent = 30,
        HostileIntent = 31,
        NeutralIntent = 32,
        HiddenIntent = 33,
        RequestHelp = 40,
        OfferHelp = 41,
        RequestAlliance = 42,
        DeclineRequest = 43,
        SpellIncantation = 50,
        SpellSign = 51,
        TeachSpell = 52,
        Incomprehensible = 255
    }

    /// <summary>
    /// Marker + tuning for entities that can send/receive communications.
    /// </summary>
    public struct CommEndpoint : IComponentData
    {
        public PerceptionChannel SupportedChannels;
        public float BaseClarity;
        public float NoiseFloor;

        public static CommEndpoint Default => new CommEndpoint
        {
            SupportedChannels = PerceptionChannel.Hearing | PerceptionChannel.Vision | PerceptionChannel.EM,
            BaseClarity = 1f,
            NoiseFloor = 0f
        };
    }

    [InternalBufferCapacity(8)]
    public struct CommAttempt : IBufferElementData
    {
        public Entity Sender;
        public Entity Receiver;
        public PerceptionChannel TransportMask;
        public CommunicationMethod Method;
        public CommunicationIntent Intent;
        public FixedString64Bytes PayloadId;
        public float Clarity;
        public float DeceptionStrength;
        public uint Timestamp;
    }

    [InternalBufferCapacity(8)]
    public struct CommReceipt : IBufferElementData
    {
        public Entity Sender;
        public PerceptionChannel Channel;
        public CommunicationMethod Method;
        public CommunicationIntent Intent;
        public FixedString64Bytes PayloadId;
        public float Integrity;
        public byte WasDeceptive;
        public uint Timestamp;
    }
}
