using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Combat
{
    /// <summary>
    /// Simple weapon spawner that emits projectile spawn requests.
    /// Use for scenario testing or data-only weapons without full mount/turret setup.
    /// </summary>
    public struct WeaponSpawner : IComponentData
    {
        public FixedString64Bytes WeaponId;
        public FixedString32Bytes AmmoId;
        public Entity TargetEntity;
        public float3 TargetPosition;
        public float3 FireDirection;
        public WeaponSpawnerAimMode AimMode;
        public float LastFireTime;
        public float EnergyReserve;
        public float HeatLevel;
        public int ShotSequence;
        public byte IsActive;
    }

    public enum WeaponSpawnerAimMode : byte
    {
        TargetEntity = 0,
        TargetPosition = 1,
        FixedDirection = 2
    }
}
