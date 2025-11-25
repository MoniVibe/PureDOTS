using PureDOTS.Runtime.Shared;
using Unity.Collections;
using Unity.Entities;

namespace Space4X.Modules
{
    /// <summary>
    /// [DEPRECATED] Use InstanceQuality component instead.
    /// Module quality component. Stores quality value (0-1) affecting module performance.
    /// </summary>
    [System.Obsolete("Use InstanceQuality component instead")]
    public struct ModuleQuality : IComponentData
    {
        public float Value; // 0-1
    }

    /// <summary>
    /// Module rarity component. Stores rarity tier affecting availability and stat modifiers.
    /// </summary>
    public struct ModuleRarity : IComponentData
    {
        public Rarity Value;
    }

    /// <summary>
    /// Module tier component. Stores tier level (0-255) affecting baseline performance.
    /// </summary>
    public struct ModuleTier : IComponentData
    {
        public byte Value; // 0-255
    }

    /// <summary>
    /// Module manufacturer component. Stores manufacturer ID for signature traits and legendary runs.
    /// </summary>
    public struct ModuleManufacturer : IComponentData
    {
        public FixedString64Bytes ManufacturerId;
    }
}

