using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Combat
{
    /// <summary>
    /// Deflection profile - drives how an entity prefers to respond to incoming threats.
    /// Pure data, used by deflection planners.
    /// </summary>
    public struct DeflectionProfile : IComponentData
    {
        public float ReactionSec;         // Reaction delay to threats
        public float MinThreatScore;      // Ignore threats below this score
        public float MaxThreatScore;      // Cap for score normalization
        public float DodgeBias;           // Preference for dodge
        public float BlockBias;           // Preference for block or shield
        public float DeflectBias;         // Preference for deflect
        public float RedirectBias;        // Preference for redirect
        public float ControlBias;         // Preference for control/hijack
        public float MaxActionsPerSecond; // Soft cap per entity
        public float CooldownSec;         // Global deflection cooldown
    }

    /// <summary>
    /// Budget for deflection actions (resource or throughput based).
    /// </summary>
    public struct DeflectionBudget : IComponentData
    {
        public float Energy;
        public float Mana;
        public float Focus;
        public float Ammo;
        public float LastSpendTime;
    }

    /// <summary>
    /// Current deflection intent for an entity.
    /// </summary>
    public struct DeflectionIntent : IComponentData
    {
        public DeflectionMode Mode;
        public Entity TargetProjectile;
        public Entity ProtectEntity;
        public float3 DesiredDirection;
        public float EstimatedCost;
        public uint DecisionTick;
    }

    /// <summary>
    /// Deflection mode.
    /// </summary>
    public enum DeflectionMode : byte
    {
        None = 0,
        Dodge = 1,
        Block = 2,
        Deflect = 3,
        Redirect = 4,
        Control = 5,
        Shield = 6,
        Intercept = 7
    }

    /// <summary>
    /// Request for a deflection action.
    /// </summary>
    [InternalBufferCapacity(4)]
    public struct DeflectionRequest : IBufferElementData
    {
        public DeflectionMode Mode;
        public Entity Projectile;
        public Entity Source;
        public Entity Protector;
        public float3 AimDirection;
        public float CostEnergy;
        public float CostMana;
        public float CostFocus;
        public uint RequestTick;
    }

    /// <summary>
    /// Deflection result event, emitted by the deflection resolver.
    /// </summary>
    [InternalBufferCapacity(4)]
    public struct DeflectionEvent : IBufferElementData
    {
        public DeflectionMode Mode;
        public Entity Projectile;
        public Entity Actor;
        public float3 ResultDirection;
        public byte Result; // 0 = fail, 1 = success
        public uint Tick;
    }
}
