using System;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Technology
{
    public enum ResearchDisciplineKind : byte
    {
        Combat = 0,
        Production = 1,
        Extraction = 2,
        Society = 3,
        Diplomacy = 4,
        Colonization = 5,
        Exploration = 6,
        Construction = 7,
        Physics = 8
    }

    public enum ResearchUnlockKind : byte
    {
        FacilityProcess = 0,
        FacilityLimb = 1,
        TechFlag = 2,
        Blueprint = 3
    }

    public enum ResearchKnowledgeState : byte
    {
        Unknown = 0,
        Stable = 1,
        Experimental = 2,
        Deprecated = 3
    }

    public enum ResearchSharingPolicy : byte
    {
        Private = 0,
        GroupLimited = 1,
        Public = 2
    }

    [Serializable]
    public struct ResearchDisciplineDefinition
    {
        public string Id;
        public string DisplayName;
        public string Description;
        public ResearchDisciplineKind Kind;
        public float BaseCostMultiplier;
        public string[] Tags;
    }

    [Serializable]
    public struct ResearchNodeDefinition
    {
        public string Id;
        public string DisplayName;
        public string Description;
        public string DisciplineId;
        public int Tier;
        public float BaseResearchCost;
        public float BaseTimeSeconds;
        public float BaseDifficulty01;
        public string[] PrerequisiteIds;
        public string[] OptionalPrerequisiteIds;
        public float MissingPrereqPenaltyMultiplier;
        public float UnlinkedPenaltyMultiplier;
        public int MinPrerequisiteLinks;
        public string[] UnlockIds;
        public string[] Tags;
        public string[] RequiredOutlookIds;
        public string[] ForbiddenOutlookIds;
        public float RequiredOutlookMinimum01;
        public float ForbiddenOutlookMaximum01;
    }

    [Serializable]
    public struct ResearchUnlockDefinition
    {
        public string Id;
        public ResearchUnlockKind Kind;
        public string TargetId;
        public float Quantity;
        public float QualityFloor01;
        public string[] Tags;
    }

    [Serializable]
    public struct ResearchKnowledgeDefinition
    {
        public string KnowledgeId;
        public string NodeId;
        public string OwnerEntityId;
        public string SourceId;
        public ResearchKnowledgeState State;
        public float KnowledgeQuality01;
        public float Drift01;
        public float Confidence01;
        public float MutationVariance01;
        public float GeniusPotential01;
        public ResearchSharingPolicy SharingPolicy;
        public string[] Tags;
    }

    public static class ResearchTreeHelpers
    {
        public static float CalculateResearchCost(
            in ResearchNodeDefinition node,
            float missingPrerequisiteRatio01,
            float unlinkedPenaltyRatio01)
        {
            var baseCost = math.max(1f, node.BaseResearchCost);
            var tierMultiplier = 1f + math.max(0, node.Tier) * 0.35f;
            var missingPenaltyMultiplier = math.max(1f, node.MissingPrereqPenaltyMultiplier);
            var unlinkedPenaltyMultiplier = math.max(1f, node.UnlinkedPenaltyMultiplier);

            var missingPenalty = math.lerp(1f, missingPenaltyMultiplier, math.saturate(missingPrerequisiteRatio01));
            var unlinkedPenalty = math.lerp(1f, unlinkedPenaltyMultiplier, math.saturate(unlinkedPenaltyRatio01));

            return baseCost * tierMultiplier * missingPenalty * unlinkedPenalty;
        }
    }
}
