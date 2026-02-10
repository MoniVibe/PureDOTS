using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Combat
{
    /// <summary>
    /// Budget for building or installing weapons (data-only).
    /// </summary>
    public struct WeaponBuildBudget : IComponentData
    {
        public float Energy;
        public float Materials;
        public float Crew;
        public float LastSpendTime;
    }

    /// <summary>
    /// Weapon install mode.
    /// </summary>
    public enum WeaponInstallMode : byte
    {
        Mount = 0,
        Spawner = 1
    }

    /// <summary>
    /// Request to install a weapon mount or spawner onto an entity.
    /// </summary>
    [InternalBufferCapacity(4)]
    public struct WeaponInstallRequest : IBufferElementData
    {
        public FixedString64Bytes WeaponId;
        public FixedString32Bytes AmmoId;
        public Entity TargetEntity;
        public float3 TargetPosition;
        public float3 FireDirection;
        public WeaponInstallMode Mode;
        public WeaponSpawnerAimMode AimMode;
        public uint TriggerTick;
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
