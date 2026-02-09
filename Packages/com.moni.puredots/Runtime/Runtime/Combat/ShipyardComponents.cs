using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.Combat
{
    /// <summary>
    /// Shipyard component - enables equipping entities via install requests.
    /// </summary>
    public struct Shipyard : IComponentData
    {
        public float Range;
        public float InstallCooldownSec;
        public float LastInstallTime;
        public byte IsActive;
    }

    /// <summary>
    /// Budget for shipyard-driven installs (data-only).
    /// </summary>
    public struct ShipyardBuildBudget : IComponentData
    {
        public float Energy;
        public float Materials;
        public float Crew;
        public float LastSpendTime;
    }

    /// <summary>
    /// Request to equip an entity via a shipyard.
    /// </summary>
    [InternalBufferCapacity(4)]
    public struct ShipyardEquipRequest : IBufferElementData
    {
        public Entity InstallEntity;
        public FixedString64Bytes WeaponId;
        public FixedString32Bytes AmmoId;
        public byte UseTargetPool;
        public WeaponInstallMode InstallMode;
        public WeaponSpawnerAimMode AimMode;
        public uint TriggerTick;
        public float RequireEnergy;
        public float RequireMaterials;
        public float RequireCrew;
        public byte ConsumeBudget;
        public float ShipyardRequireEnergy;
        public float ShipyardRequireMaterials;
        public float ShipyardRequireCrew;
        public byte ConsumeShipyardBudget;
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
