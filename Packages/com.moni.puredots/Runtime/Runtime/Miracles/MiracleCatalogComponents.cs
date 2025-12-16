using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.Miracles
{
    /// <summary>
    /// Miracle identifier enum (ushort for more IDs than byte).
    /// </summary>
    public enum MiracleId : ushort
    {
        None = 0,
        Rain = 1,
        TemporalVeil = 2,
        Fire = 3,
        Heal = 4,
        // Future: Verdant, Earthquake, etc.
    }
    
    /// <summary>
    /// Dispensation mode for miracles (how they are cast).
    /// </summary>
    public enum DispenseMode : byte
    {
        Sustained = 1,
        Throw = 2
    }
    
    /// <summary>
    /// Targeting mode for miracles.
    /// </summary>
    public enum TargetingMode : byte
    {
        Point = 0,
        Area = 1,
        Actor = 2,
        Self = 3
    }
    
    /// <summary>
    /// Miracle category for organization and filtering.
    /// </summary>
    public enum MiracleCategory : byte
    {
        Weather = 0,
        Offensive = 1,
        Support = 2,
        Control = 3,
        Epic = 4
    }
    
    /// <summary>
    /// Specification for a miracle type in the catalog.
    /// </summary>
    public struct MiracleSpec
    {
        public MiracleId Id;
        public float BaseCooldownSeconds;
        public float BasePrayerCost;   // Not enforced in MVP, but present
        public float BaseRadius;
        public float MaxRadius;
        public byte MaxCharges;        // 1 for MVP
        public byte Tier;              // 1=small, 2=medium, 3=epic
        public byte AllowedDispenseModes; // bitmask: Sustained, Throw
        public TargetingMode TargetingMode;
        public MiracleCategory Category;
    }
    
    /// <summary>
    /// Catalog blob containing all miracle specifications.
    /// </summary>
    public struct MiracleCatalogBlob
    {
        public BlobArray<MiracleSpec> Specs;
    }
    
    /// <summary>
    /// Singleton component providing access to the miracle catalog.
    /// </summary>
    public struct MiracleConfigState : IComponentData
    {
        public BlobAssetReference<MiracleCatalogBlob> Catalog;
        public float GlobalCooldownScale; // For tuning/difficulty
    }
}
























