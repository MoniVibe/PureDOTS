using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Shared;

namespace PureDOTS.Runtime.Components
{
    /// <summary>
    /// Gene specification storing genetic traits.
    /// Burst-safe struct for deterministic inheritance.
    /// </summary>
    public struct GeneSpec : IComponentData
    {
        public AgentGuid GeneId;            // Unique gene identifier
        public float TraitValue;            // Trait value (0-1, normalized)
        public GeneType Type;                // Type of gene/trait
        public float Dominance;             // Dominance factor (0-1, 1 = fully dominant)
        public uint Generation;             // Generation number
    }

    /// <summary>
    /// Types of genetic traits.
    /// </summary>
    public enum GeneType : byte
    {
        Speed = 0,
        Strength = 1,
        Intelligence = 2,
        Morale = 3,
        Aggression = 4,
        Social = 5
    }

    /// <summary>
    /// Genetic inheritance data for offspring.
    /// Stores blended traits from parents.
    /// </summary>
    public struct InheritanceData : IBufferElementData
    {
        public AgentGuid ParentGuid;        // Parent entity GUID
        public GeneType TraitType;          // Trait type inherited
        public float InheritedValue;         // Inherited trait value
        public float Contribution;          // Contribution weight (0-1)
    }

    /// <summary>
    /// Genetic state tracking evolution.
    /// </summary>
    public struct GeneticState : IComponentData
    {
        public AgentGuid EntityGuid;        // Entity identifier
        public uint Generation;             // Current generation
        public int OffspringCount;          // Number of offspring produced
        public float Fitness;               // Fitness score (for selection)
        public uint LastEvolutionTick;      // When evolution was last evaluated
    }
}

