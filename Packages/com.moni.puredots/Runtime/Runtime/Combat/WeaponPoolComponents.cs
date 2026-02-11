using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.Combat
{
    /// <summary>
    /// Role tags for weapon pools - used for biasing selection.
    /// </summary>
    public enum WeaponPoolRole : byte
    {
        Primary = 0,
        Secondary = 1,
        PointDefense = 2,
        Support = 3,
        Experimental = 4
    }

    /// <summary>
    /// Selection mode for weapon pool picks.
    /// </summary>
    public enum WeaponPoolSelectionMode : byte
    {
        WeightedRandom = 0,
        RoundRobin = 1,
        FirstValid = 2
    }

    /// <summary>
    /// Configuration for weapon pool selection (data-only).
    /// </summary>
    public struct WeaponPoolConfig : IComponentData
    {
        public WeaponPoolSelectionMode SelectionMode;
        public float MinIntervalSec;
        public float LastSelectTime;
        public int MaxSelections;
        public int SelectionsMade;
        public int RoundRobinIndex;
        public byte AutoInstall;
        public byte RequireNoWeapon;
        public byte ReplaceExisting;
        public byte ConsumeBudget;
        public float PrimaryBias;
        public float SecondaryBias;
        public float PointDefenseBias;
        public float SupportBias;
        public float ExperimentalBias;
    }

    /// <summary>
    /// Weapon pool entry describing a selectable weapon install payload.
    /// </summary>
    [InternalBufferCapacity(8)]
    public struct WeaponPoolEntry : IBufferElementData
    {
        public FixedString64Bytes WeaponId;
        public FixedString32Bytes AmmoId;
        public WeaponPoolRole Role;
        public WeaponInstallMode InstallMode;
        public WeaponSpawnerAimMode AimMode;
        public float Weight;
        public float RequireEnergy;
        public float RequireMaterials;
        public float RequireCrew;
        public byte ConsumeBudget;
        public byte ReplaceExisting;
        public float InitialEnergy;
        public float InitialHeat;
        public int MagazineCapacity;
        public int MagazineCurrent;
        public int AmmoPerShot;
        public float ReloadSec;
        public int StockpileCapacity;
        public int StockpileCurrent;
    }
}
