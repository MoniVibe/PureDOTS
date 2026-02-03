using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.Knowledge
{
    public enum KnowledgeSourceKind : byte
    {
        Observation = 0,
        Report = 1,
        Inference = 2,
        Training = 3,
        Memory = 4
    }

    public enum KnowledgeApplyMode : byte
    {
        Add = 0,
        Max = 1,
        Override = 2
    }

    /// <summary>
    /// Discrete knowledge fact with confidence and decay.
    /// </summary>
    [InternalBufferCapacity(8)]
    public struct KnowledgeFact : IBufferElementData
    {
        public FixedString64Bytes FactId;
        public float Confidence;
        public uint LearnedTick;
        public uint LastUpdatedTick;
        public uint DecayHalfLife;
        public KnowledgeSourceKind SourceKind;
        public Entity SourceEntity;
    }

    /// <summary>
    /// Request to add or update a knowledge fact.
    /// </summary>
    [InternalBufferCapacity(4)]
    public struct KnowledgeFactRequest : IBufferElementData
    {
        public FixedString64Bytes FactId;
        public float Confidence;
        public uint DecayHalfLife;
        public KnowledgeSourceKind SourceKind;
        public KnowledgeApplyMode ApplyMode;
        public Entity SourceEntity;
    }

    /// <summary>
    /// Configuration for knowledge facts.
    /// </summary>
    public struct KnowledgeFactConfig : IComponentData
    {
        public float MinConfidence;
        public int MaxFacts;

        public static KnowledgeFactConfig Default => new KnowledgeFactConfig
        {
            MinConfidence = 0.01f,
            MaxFacts = 12
        };
    }
}
