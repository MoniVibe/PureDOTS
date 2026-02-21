using System;

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
}