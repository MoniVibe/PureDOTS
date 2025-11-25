using PureDOTS.Environment;
using Space4X.Registry;
using Unity.Collections;
using Unity.Entities;

namespace Space4X.Aggregates
{
    /// <summary>
    /// Aggregate type component. Identifies the kind of aggregate (Dynasty, Guild, Corporation, Army, Band).
    /// </summary>
    public struct AggregateType : IComponentData
    {
        public AggregateKind Kind;
    }

    /// <summary>
    /// Aggregate kind enumeration.
    /// </summary>
    public enum AggregateKind : byte
    {
        Dynasty = 0,
        Guild = 1,
        Corporation = 2,
        Army = 3,
        Band = 4
    }

    /// <summary>
    /// Aggregate outlook profile reference. Points to outlook profile blob.
    /// </summary>
    public struct AggregateOutlookProfile : IComponentData
    {
        public BlobAssetReference<OutlookProfileBlob> Profile;
    }

    /// <summary>
    /// Aggregate alignment profile reference. Points to alignment profile blob.
    /// </summary>
    public struct AggregateAlignmentProfile : IComponentData
    {
        public BlobAssetReference<AlignmentProfileBlob> Profile;
    }

    /// <summary>
    /// Aggregate policy component. Stores resolved policy fields from profile composition.
    /// </summary>
    public struct AggregatePolicy : IComponentData
    {
        public float Aggression;
        public float TradeBias;
        public float Diplomacy;
        public byte DoctrineMissile;
        public byte DoctrineLaser;
        public byte DoctrineHangar;
        public float FieldRefitMult;
        // Additional policy fields can be added as needed
    }

    /// <summary>
    /// Outlook profile blob structure (placeholder - to be defined based on catalog structure).
    /// </summary>
    public struct OutlookProfileBlob
    {
        public FixedString64Bytes Id;
        // Additional outlook fields
    }

    /// <summary>
    /// Alignment profile blob structure (placeholder - to be defined based on catalog structure).
    /// </summary>
    public struct AlignmentProfileBlob
    {
        public FixedString64Bytes Id;
        public AlignmentTriplet Alignment;
        // Additional alignment fields
    }
}

