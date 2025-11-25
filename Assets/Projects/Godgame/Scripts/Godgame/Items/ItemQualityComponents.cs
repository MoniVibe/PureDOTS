using PureDOTS.Runtime.Shared;
using Unity.Collections;
using Unity.Entities;

namespace Godgame.Items
{
    /// <summary>
    /// Material quality component. Stores the quality value (0-100) for a material entity.
    /// </summary>
    public struct MaterialQuality : IComponentData
    {
        public float Value; // 0-100
    }

    /// <summary>
    /// Material purity component. Stores the purity value (0-100) for extracted materials.
    /// </summary>
    public struct MaterialPurity : IComponentData
    {
        public float Value; // 0-100
    }

    /// <summary>
    /// Material rarity component. Stores the rarity tier for a material entity.
    /// </summary>
    public struct MaterialRarity : IComponentData
    {
        public Rarity Value;
    }

    /// <summary>
    /// Material tech tier component. Stores the tech tier (0-10) required to extract/use this material.
    /// </summary>
    public struct MaterialTechTier : IComponentData
    {
        public byte Value; // 0-10
    }

    /// <summary>
    /// [DEPRECATED] Use InstanceQuality component instead.
    /// Equipment quality component. Stores the quality value (0-100) derived from materials.
    /// </summary>
    [System.Obsolete("Use InstanceQuality component instead")]
    public struct EquipmentQuality : IComponentData
    {
        public float Value; // 0-100
    }

    /// <summary>
    /// Equipment rarity component. Stores the rarity tier derived from materials + craftsman.
    /// </summary>
    public struct EquipmentRarity : IComponentData
    {
        public Rarity Value;
    }

    /// <summary>
    /// Equipment tech tier component. Stores the tech tier (0-10) required to craft/use this equipment.
    /// </summary>
    public struct EquipmentTechTier : IComponentData
    {
        public byte Value; // 0-10
    }

    /// <summary>
    /// [DEPRECATED] Use InstanceQuality component instead.
    /// Tool quality component. Stores the quality value (0-100) derived from quality formula.
    /// </summary>
    [System.Obsolete("Use InstanceQuality component instead")]
    public struct ToolQuality : IComponentData
    {
        public float Value; // 0-100
    }

    /// <summary>
    /// Tool rarity component. Stores the rarity tier derived from materials + craftsman.
    /// </summary>
    public struct ToolRarity : IComponentData
    {
        public Rarity Value;
    }

    /// <summary>
    /// Tool tech tier component. Stores the tech tier (0-10) required to craft/use this tool.
    /// </summary>
    public struct ToolTechTier : IComponentData
    {
        public byte Value; // 0-10
    }

    /// <summary>
    /// Tool ID component. Identifies a tool entity.
    /// </summary>
    public struct ToolId : IComponentData
    {
        public FixedString64Bytes Value;
    }

    /// <summary>
    /// Tool specification reference. Points to the tool catalog blob.
    /// </summary>
    public struct ToolSpecRef : IComponentData
    {
        public BlobAssetReference<ToolSpecBlob> Blob;
    }

    /// <summary>
    /// Production input buffer element. Defines a material input required for tool/equipment production.
    /// </summary>
    public struct ProductionInput : IBufferElementData
    {
        public FixedString64Bytes MaterialId;
        public float Quantity;
        public float MinPurity;
        public float MinQuality;
        public byte MinTechTier; // Minimum tech tier required to use this input
    }

    /// <summary>
    /// Material attribute buffer element. Stores attributes that skilled craftsmen can add to items.
    /// </summary>
    public struct MaterialAttribute : IBufferElementData
    {
        public FixedString64Bytes AttributeId; // e.g., "IncreasedDurability", "SharpEdge"
        public float Value;
        public bool IsPercentage;
        public byte MinCraftsmanSkill; // Minimum skill (0-100) to add this attribute
        public float ChanceToAdd; // Probability (0-1) of adding this attribute
    }
}

