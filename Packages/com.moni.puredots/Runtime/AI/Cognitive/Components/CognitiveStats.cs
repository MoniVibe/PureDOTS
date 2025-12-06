using Unity.Entities;

namespace PureDOTS.Runtime.AI.Cognitive
{
    /// <summary>
    /// Cognitive statistics component regulating procedural learning speed, reasoning depth, and behavioral flexibility.
    /// Intelligence: computational efficiency, problem-solving rate (abstract thinking, planning, hypothesis testing).
    /// Wisdom: integrative, experience-based reasoning (pattern recognition, lessons learned, intuition).
    /// Curiosity: exploration weight in procedural learning loop.
    /// Focus: cognitive stamina - how long agent can reason before mental fatigue.
    /// 
    /// See Docs/Guides/CognitiveStatsIntegrationGuide.md for usage examples and integration patterns.
    /// </summary>
    public struct CognitiveStats : IComponentData
    {
        /// <summary>
        /// Intelligence stat (0-10 range). Higher values = faster learning, more recursive planning, better affordance discovery.
        /// Dominates cognitive layer (planning, hypothesis testing).
        /// </summary>
        public float Intelligence;

        /// <summary>
        /// Wisdom stat (0-10 range). Higher values = better retention, generalization, bias correction.
        /// Dominates limbic/emotional layer and social/observational learning.
        /// </summary>
        public float Wisdom;

        /// <summary>
        /// Curiosity stat (0-10 range). Higher values = more exploration, random affordance tests.
        /// Lower values = conservative, risk-averse behavior.
        /// </summary>
        public float Curiosity;

        /// <summary>
        /// Focus stat (0-10 range). Current cognitive stamina. Decays during heavy reasoning.
        /// When low, gates heavy planning operations.
        /// </summary>
        public float Focus;

        /// <summary>
        /// Maximum focus value (default 10.0). Used for fatigue calculations.
        /// </summary>
        public float MaxFocus;

        /// <summary>
        /// Last tick when focus was decayed. Used for fatigue tracking.
        /// </summary>
        public uint LastFocusDecayTick;

        /// <summary>
        /// Creates default cognitive stats (all at 5.0, Focus at 10.0).
        /// </summary>
        public static CognitiveStats CreateDefaults()
        {
            return new CognitiveStats
            {
                Intelligence = 5.0f,
                Wisdom = 5.0f,
                Curiosity = 5.0f,
                Focus = 10.0f,
                MaxFocus = 10.0f,
                LastFocusDecayTick = 0
            };
        }

        /// <summary>
        /// Normalizes a stat value from 0-10 range to 0-1 range for use in formulas.
        /// </summary>
        public static float Normalize(float statValue)
        {
            return statValue / 10.0f;
        }

        /// <summary>
        /// Gets normalized Intelligence (0-1 range).
        /// </summary>
        public float IntelligenceNormalized => Normalize(Intelligence);

        /// <summary>
        /// Gets normalized Wisdom (0-1 range).
        /// </summary>
        public float WisdomNormalized => Normalize(Wisdom);

        /// <summary>
        /// Gets normalized Curiosity (0-1 range).
        /// </summary>
        public float CuriosityNormalized => Normalize(Curiosity);

        /// <summary>
        /// Gets normalized Focus (0-1 range).
        /// </summary>
        public float FocusNormalized => Normalize(Focus);
    }
}

