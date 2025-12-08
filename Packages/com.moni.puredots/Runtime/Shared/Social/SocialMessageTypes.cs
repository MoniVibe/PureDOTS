using Unity.Entities;

namespace PureDOTS.Runtime.AI.Social
{
    /// <summary>
    /// Types of social messages for communication and cooperation protocols.
    /// Based on Hoey et al. (2018) structured message passing patterns.
    /// </summary>
    public enum SocialMessageType : ushort
    {
        None = 0,
        Offer = 1,          // Trade offer, resource exchange proposal
        Request = 2,         // Request for help, resources, or cooperation
        Threat = 3,          // Territorial threat, conflict warning
        Praise = 4,         // Positive feedback, trust building
        Inquiry = 5,        // Information request, knowledge sharing inquiry
        CounterOffer = 6,    // Response to an offer
        Accept = 7,          // Acceptance of offer/request
        Reject = 8,          // Rejection of offer/request
        ShareKnowledge = 9,  // Knowledge/research sharing
        Appeal = 10          // Appeal for help, diplomatic request
    }

    /// <summary>
    /// Flags for SocialMessage indicating message properties.
    /// </summary>
    public static class SocialMessageFlags
    {
        public const ushort Urgent = 1 << 0;        // High priority message
        public const ushort Broadcast = 1 << 1;      // Broadcast to nearby agents
        public const ushort RequiresResponse = 1 << 2; // Expects a response
        public const ushort TrustedSender = 1 << 3;  // Sender is trusted
    }
}

