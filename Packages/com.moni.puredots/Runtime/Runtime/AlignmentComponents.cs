using System;
using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.Alignment
{
    public enum AffiliationKind : byte
    {
        Unknown = 0,
        Faction = 1,
        Crew = 2,
        Fleet = 3,
        Colony = 4
    }

    public enum EthicAxis : byte
    {
        OrderVsChaos = 0,
        CompassionVsPragmatism = 1,
        TraditionVsInnovation = 2
    }

    public enum Outlook : byte
    {
        Neutral = 0,
        Loyalist = 1,
        Opportunist = 2,
        Fanatic = 3,
        Mutinous = 4
    }

    public struct AffiliationId : IEquatable<AffiliationId>
    {
        public FixedString64Bytes Value;

        public readonly bool Equals(AffiliationId other) => Value.Equals(other.Value);
        public override readonly bool Equals(object obj) => obj is AffiliationId other && Equals(other);
        public override readonly int GetHashCode() => Value.GetHashCode();
        public override readonly string ToString() => Value.ToString();
    }

    public struct DoctrineId : IEquatable<DoctrineId>
    {
        public FixedString64Bytes Value;

        public static DoctrineId FromString(string raw)
        {
            var trimmed = string.IsNullOrWhiteSpace(raw) ? string.Empty : raw.Trim().ToLowerInvariant();
            return new DoctrineId { Value = new FixedString64Bytes(trimmed) };
        }

        public readonly bool Equals(DoctrineId other) => Value.Equals(other.Value);
        public override readonly bool Equals(object obj) => obj is DoctrineId other && Equals(other);
        public override readonly int GetHashCode() => Value.GetHashCode();
        public override readonly string ToString() => Value.ToString();
    }

    public struct DoctrineDefinition
    {
        public DoctrineId Id;
        public AffiliationKind Kind;
        public float OrderAffinity;
        public float CompassionAffinity;
        public float InnovationAffinity;
        public float FanaticismCap;
    }

    public struct DoctrineCatalogBlob
    {
        public BlobArray<DoctrineDefinition> Definitions;
    }

    public struct DoctrineCatalog : IComponentData
    {
        public BlobAssetReference<DoctrineCatalogBlob> Catalog;
    }

    public struct DoctrineRef : IComponentData
    {
        public DoctrineId Id;
    }

    public struct CrewAlignmentSample : IBufferElementData
    {
        public AffiliationId Affiliation;
        public DoctrineId Doctrine;
        public float Loyalty;
        public float Suspicion;
        public float Obedience;
        public float Fanaticism;
        public Outlook Outlook;
    }

    public enum ComplianceStatus : byte
    {
        Nominal = 0,
        Warning = 1,
        Breach = 2
    }

    public struct CrewCompliance : IComponentData
    {
        public AffiliationId Affiliation;
        public DoctrineId Doctrine;
        public float AverageLoyalty;
        public float AverageSuspicion;
        public float AverageFanaticism;
        public float SuspicionDelta;
        public ComplianceStatus Status;
        public byte MissingData;
        public uint LastUpdateTick;
    }

    public struct ComplianceThresholds : IComponentData
    {
        public float SuspicionDeltaWarning;
        public float SuspicionDeltaBreach;
        public float LoyaltyWarning;
        public float LoyaltyBreach;

        public static ComplianceThresholds CreateDefault()
        {
            return new ComplianceThresholds
            {
                SuspicionDeltaWarning = 0.1f,
                SuspicionDeltaBreach = 0.25f,
                LoyaltyWarning = 0.35f,
                LoyaltyBreach = 0.2f
            };
        }
    }

    public struct ComplianceAlert : IBufferElementData
    {
        public AffiliationId Affiliation;
        public DoctrineId Doctrine;
        public ComplianceStatus Status;
        public float SuspicionDelta;
        public float Loyalty;
        public float Suspicion;
        public byte MissingData;
    }
}
