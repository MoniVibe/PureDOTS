using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Combat
{
    /// <summary>
    /// Tracks control or disruption applied to a projectile.
    /// </summary>
    public struct ProjectileControlState : IComponentData
    {
        public Entity Controller;
        public ProjectileControlMode Mode;
        public float ControlStrength; // 0..1
        public float GuidanceJitter;  // Noise to apply to steering
        public float MaxTurnRateDeg;  // Optional override
        public uint ControlTick;
    }

    /// <summary>
    /// Request to control, redirect, or disrupt a projectile.
    /// </summary>
    [InternalBufferCapacity(4)]
    public struct ProjectileControlRequest : IBufferElementData
    {
        public Entity Controller;
        public Entity Projectile;
        public ProjectileControlMode Mode;
        public float ControlStrength;
        public float DurationSec;
        public float3 TargetPosition;
        public uint RequestTick;
    }

    /// <summary>
    /// Projectile control modes.
    /// </summary>
    public enum ProjectileControlMode : byte
    {
        None = 0,
        Redirect = 1,
        Hijack = 2,
        Disrupt = 3
    }

    /// <summary>
    /// Signature for projectile identification and countermeasures.
    /// </summary>
    public struct ProjectileSignature : IComponentData
    {
        public uint Signature;
        public float Emission;
        public float DecoyResistance;
        public float ECCMStrength;
    }
}
