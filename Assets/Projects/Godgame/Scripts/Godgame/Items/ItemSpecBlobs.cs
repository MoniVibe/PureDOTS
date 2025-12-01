using PureDOTS.Runtime.Shared;
using Unity.Collections;
using Unity.Entities;

namespace Godgame.Items
{
    /// <summary>
    /// Material specification blob. Defines material properties, quality, rarity, and tech tier.
    /// </summary>
    public struct MaterialSpecBlob
    {
        public FixedString64Bytes Id;
        public float BaseQuality; // 0-100
        public float Purity; // 0-100, for extracted materials
        public Rarity Rarity;
        public byte TechTier; // 0-10
        public BlobArray<MaterialAttributeEntry> PossibleAttributes;
    }

    /// <summary>
    /// Material attribute entry in blob.
    /// </summary>
    public struct MaterialAttributeEntry
    {
        public FixedString64Bytes AttributeId;
        public float Value;
        public bool IsPercentage;
        public byte MinCraftsmanSkill;
        public float ChanceToAdd;
    }

    /// <summary>
    /// Equipment specification blob. Defines equipment properties and requirements.
    /// </summary>
    public struct EquipmentSpecBlob
    {
        public FixedString64Bytes Id;
        public float BaseDurability;
        public byte RequiredTechTier; // 0-10
        // Material requirements would be in a separate component/buffer
    }

    /// <summary>
    /// Tool specification blob. Defines tool properties and production inputs.
    /// Quality is computed via shared QualityFormulaBlob, not stored here.
    /// </summary>
    public struct ToolSpecBlob
    {
        public FixedString64Bytes Id;
        public FixedString64Bytes ProducedFrom; // Parent material/tool name
        public byte RequiredTechTier; // 0-10
        public BlobArray<ProductionInputEntry> ProductionInputs;
        public BlobArray<MaterialAttributeEntry> PossibleAttributes;
    }

    /// <summary>
    /// Production input entry in blob.
    /// </summary>
    public struct ProductionInputEntry
    {
        public FixedString64Bytes MaterialId;
        public float Quantity;
        public float MinPurity;
        public float MinQuality;
        public byte MinTechTier;
    }
}

