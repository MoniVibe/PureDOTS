using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.Combat
{
    /// <summary>
    /// Ammo stockpile for an entity (shared across weapons).
    /// </summary>
    public struct AmmoStockpile : IComponentData
    {
        public FixedString32Bytes AmmoType;
        public int Current;
        public int Capacity;
    }

    /// <summary>
    /// Per-weapon magazine state for ammo-based weapons.
    /// </summary>
    public struct WeaponMagazine : IComponentData
    {
        public FixedString32Bytes AmmoType;
        public int Current;
        public int Capacity;
        public int AmmoPerShot;
        public float ReloadSec;
        public float LastReloadTime;
    }

    /// <summary>
    /// Ammo consumption request emitted by weapon systems.
    /// </summary>
    [InternalBufferCapacity(4)]
    public struct AmmoConsumptionRequest : IBufferElementData
    {
        public FixedString32Bytes AmmoType;
        public int Amount;
        public Entity WeaponEntity;
        public uint Tick;
    }
}
