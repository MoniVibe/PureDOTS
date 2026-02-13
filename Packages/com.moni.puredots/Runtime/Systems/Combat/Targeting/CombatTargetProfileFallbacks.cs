using PureDOTS.Runtime.Combat;
using PureDOTS.Runtime.Combat.Targeting;
using Unity.Mathematics;

namespace PureDOTS.Systems.Combat
{
    public static class CombatTargetProfileFallbacks
    {
        public static CombatDoctrineProfile BuildDoctrineFallback(in TargetSelectionConfig config)
        {
            return new CombatDoctrineProfile
            {
                ThreatWeight = math.max(0f, config.ThreatWeight),
                RangeWeight = math.max(0f, config.DistanceWeight),
                HealthWeight = math.max(0f, config.HealthWeight),
                ObjectiveWeight = 0.2f,
                FocusFireWeight = 0.2f,
                EngageScoreThreshold = 0f,
                RetreatHullThreshold = 0.3f,
                RetreatRiskMultiplier = 1f
            };
        }

        public static CombatIndividualProfile BuildIndividualFromCombatAI(in CombatAI combatAI)
        {
            return new CombatIndividualProfile
            {
                AggressionBias = math.clamp(combatAI.Aggression / 50f, -1f, 1f),
                ObjectiveBias = 0f,
                FinishOffBias = 0f,
                RangeBias = 0f,
                RiskTolerance = 1f - math.saturate(combatAI.FleeThresholdHP / 100f)
            };
        }

        public static CombatIndividualProfile BuildNeutralIndividualFallback()
        {
            return new CombatIndividualProfile
            {
                AggressionBias = 0f,
                ObjectiveBias = 0f,
                FinishOffBias = 0f,
                RangeBias = 0f,
                RiskTolerance = 0.5f
            };
        }
    }
}
