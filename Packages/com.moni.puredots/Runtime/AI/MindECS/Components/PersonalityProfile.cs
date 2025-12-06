using PureDOTS.Shared;

namespace PureDOTS.AI.MindECS.Components
{
    /// <summary>
    /// Personality traits and moral values for cognitive AI agents.
    /// Managed class component for DefaultEcs (Mind ECS layer).
    /// </summary>
    public class PersonalityProfile
    {
        // Core traits (0-1 normalized)
        public float RiskTolerance;
        public float SocialPreference;
        public float Aggressiveness;
        public float Curiosity;
        public float Loyalty;

        // Morality values (-1 to 1, where -1 = evil, 0 = neutral, 1 = good)
        public float MoralityAlignment;
        public float Lawfulness; // -1 = chaotic, 0 = neutral, 1 = lawful
        public float Altruism;

        // Social preferences
        public float TrustLevel; // Base trust in others (0-1)
        public float DeceptionTendency; // Likelihood to deceive (0-1)

        public PersonalityProfile()
        {
            // Default neutral personality
            RiskTolerance = 0.5f;
            SocialPreference = 0.5f;
            Aggressiveness = 0.5f;
            Curiosity = 0.5f;
            Loyalty = 0.5f;
            MoralityAlignment = 0f;
            Lawfulness = 0f;
            Altruism = 0f;
            TrustLevel = 0.5f;
            DeceptionTendency = 0.2f;
        }
    }
}

