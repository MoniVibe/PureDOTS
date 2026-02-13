using Unity.Entities;

namespace PureDOTS.Systems.Combat
{
    /// <summary>
    /// Collective combat doctrine weights shared by a squad/fleet.
    /// Values are intended to be normalized to [0,1], but are clamped at use sites.
    /// </summary>
    public struct CombatDoctrineProfile : IComponentData
    {
        public float ThreatWeight;
        public float RangeWeight;
        public float HealthWeight;
        public float ObjectiveWeight;
        public float FocusFireWeight;

        /// <summary>
        /// Minimum adjusted score required to engage.
        /// </summary>
        public float EngageScoreThreshold;

        /// <summary>
        /// Normalized hull value where retreat pressure starts to rise.
        /// </summary>
        public float RetreatHullThreshold;

        /// <summary>
        /// How strongly low hull pushes retreat.
        /// </summary>
        public float RetreatRiskMultiplier;
    }

    /// <summary>
    /// Individual combat biases applied on top of doctrine.
    /// Values are expected in [-1,1].
    /// </summary>
    public struct CombatIndividualProfile : IComponentData
    {
        public float AggressionBias;
        public float ObjectiveBias;
        public float FinishOffBias;
        public float RangeBias;

        /// <summary>
        /// 0 = risk averse, 1 = risk tolerant.
        /// </summary>
        public float RiskTolerance;
    }
}

