using System;
using PureDOTS.Runtime.Combat;
using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.Anatomy
{
    /// <summary>
    /// Stable anatomy identifier for an entity (race/species/variant driven).
    /// </summary>
    public struct AnatomyId : IComponentData
    {
        public FixedString64Bytes Value;
    }

    /// <summary>
    /// Catalog reference for anatomy definitions.
    /// </summary>
    public struct AnatomyCatalogRef : IComponentData
    {
        public BlobAssetReference<AnatomyCatalogBlob> Catalog;
    }

    /// <summary>
    /// Blob catalog for anatomy definitions.
    /// </summary>
    public struct AnatomyCatalogBlob
    {
        public BlobArray<AnatomyDefinition> Definitions;
    }

    /// <summary>
    /// One anatomy definition (humanoid, insectoid, drone, etc.).
    /// </summary>
    public struct AnatomyDefinition
    {
        public FixedString64Bytes AnatomyId;
        public BlobArray<BodyPartDefinition> Parts;
    }

    /// <summary>
    /// Body part role classification.
    /// </summary>
    public enum BodyPartKind : byte
    {
        Limb = 0,
        Organ = 1,
        External = 2,
        Internal = 3,
        Prosthetic = 4
    }

    /// <summary>
    /// Body part flags for capability and criticality.
    /// </summary>
    [Flags]
    public enum BodyPartFlags : byte
    {
        None = 0,
        Vital = 1 << 0,
        Paired = 1 << 1,
        Sensor = 1 << 2,
        Locomotion = 1 << 3,
        Manipulator = 1 << 4,
        Prosthetic = 1 << 5
    }

    /// <summary>
    /// Definition of a single body part within an anatomy.
    /// </summary>
    public struct BodyPartDefinition
    {
        public FixedString64Bytes PartId;
        public FixedString64Bytes ParentId;
        public BodyPartKind Kind;
        public BodyPartFlags Flags;
        public float MaxHealth;
        public float RegenRate;
        public float DamageMultiplier;
    }

    /// <summary>
    /// Runtime state for a body part instance.
    /// </summary>
    [InternalBufferCapacity(8)]
    public struct BodyPartState : IBufferElementData
    {
        public FixedString64Bytes PartId;
        public FixedString64Bytes ParentId;
        public BodyPartKind Kind;
        public BodyPartFlags Flags;
        public float MaxHealth;
        public float CurrentHealth;
        public float RegenRate;
        public float DamageMultiplier;
    }

    /// <summary>
    /// Damage routed to a specific body part.
    /// </summary>
    [InternalBufferCapacity(4)]
    public struct BodyPartDamageEvent : IBufferElementData
    {
        public FixedString64Bytes PartId;
        public float Damage;
        public DamageType DamageType;
        public DamageFlags DamageFlags;
        public Entity SourceEntity;
        public uint Tick;
    }
}
