using System;
using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.Genetics
{
    [Flags]
    public enum GeneticMutationSourceFlags : byte
    {
        None = 0,
        Natural = 1 << 0,
        ResearchFacility = 1 << 1,
        Event = 1 << 2,
        Miracle = 1 << 3,
        Forced = 1 << 4
    }

    public struct GenomeId : IEquatable<GenomeId>
    {
        public FixedString64Bytes Value;

        public static GenomeId FromString(string raw)
        {
            var normalized = string.IsNullOrWhiteSpace(raw) ? string.Empty : raw.Trim().ToLowerInvariant();
            return new GenomeId { Value = new FixedString64Bytes(normalized) };
        }

        public bool Equals(GenomeId other) => Value.Equals(other.Value);
        public override bool Equals(object obj) => obj is GenomeId other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Value.ToString();
    }

    public struct GeneticProfile : IComponentData
    {
        public GenomeId Genome;
        public FixedString64Bytes SpeciesId;
        public int Generation;
        public float Quality01;
        public float Stability01;
        public float Drift01;
        public float MutationVariance01;
        public float Mutability01;
        public GeneticMutationSourceFlags AllowedMutationSources;
    }

    public struct GeneticInclination : IComponentData
    {
        public float ViolenceDiplomacyAxis;
        public float MightMagicAxis;
    }

    public struct GeneticHabitatPreference : IComponentData
    {
        public FixedString32Bytes PrimaryHabitatId;
        public FixedString32Bytes SecondaryHabitatId;
        public FixedString32Bytes PreferredBiomeId;
        public float PreferredGravity;
        public float GravityTolerancePercent;
        public byte ToleratesExtremeEnvironments;
    }

    public struct CultureProfile : IComponentData
    {
        public float SpiritualMaterialAxis;
        public float LawfulChaoticAxis;
        public float CorruptPureAxis;
        public float XenophileXenophobeAxis;
        public float Mutability01;
        public float DriftRate01;
        public float Cohesion01;
    }

    [Serializable]
    public struct GeneticAxisDefinition
    {
        public string Id;
        public string DisplayName;
        public float MinValue;
        public float MaxValue;
        public float DefaultValue;
        public string NegativePoleLabel;
        public string NeutralLabel;
        public string PositivePoleLabel;
        public string[] Tags;
    }

    [Serializable]
    public struct CultureAxisDefinition
    {
        public string Id;
        public string DisplayName;
        public float MinValue;
        public float MaxValue;
        public float DefaultValue;
        public string NegativePoleLabel;
        public string NeutralLabel;
        public string PositivePoleLabel;
        public string[] Tags;
    }

    [Serializable]
    public struct GeneticProfileDefinition
    {
        public string Id;
        public float ViolenceDiplomacyAxis;
        public float MightMagicAxis;
        public string PrimaryHabitatId;
        public string SecondaryHabitatId;
        public string PreferredBiomeId;
        public float PreferredGravity;
        public float GravityTolerancePercent;
        public bool ToleratesExtremeEnvironments;
        public float MutationVariance01;
        public float Mutability01;
        public GeneticMutationSourceFlags AllowedMutationSources;
        public string[] Tags;
    }

    [Serializable]
    public struct CultureProfileDefinition
    {
        public string Id;
        public float SpiritualMaterialAxis;
        public float LawfulChaoticAxis;
        public float CorruptPureAxis;
        public float XenophileXenophobeAxis;
        public float Mutability01;
        public float DriftRate01;
        public float Cohesion01;
        public string[] Tags;
    }
}