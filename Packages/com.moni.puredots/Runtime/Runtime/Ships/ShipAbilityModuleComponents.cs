using Unity.Entities;

namespace PureDOTS.Runtime.Ships
{
    /// <summary>
    /// Selected ship ability module (boost, skip, time core).
    /// </summary>
    public struct ShipAbilityModule : IComponentData
    {
        public ShipAbilityKind Kind;
    }

    public enum ShipAbilityKind : byte
    {
        None = 0,
        BoostDrive = 1,
        SkipDrive = 2,
        TimeCore = 3
    }

    /// <summary>
    /// Game-agnostic boost drive tuning.
    /// </summary>
    public struct BoostDriveModuleConfig : IComponentData
    {
        public float BoostMultiplier;
        public float BoostDurationSeconds;
        public float CooldownSeconds;
        public float EnergyCost;
        public byte DisablesBaseBoost;
    }

    /// <summary>
    /// Game-agnostic spatial skip tuning.
    /// </summary>
    public struct SkipDriveModuleConfig : IComponentData
    {
        public float MinRange;
        public float MaxRange;
        public float CooldownSeconds;
        public float ChargeTimeSeconds;
        public float OriginDamageRadius;
        public float OriginDamage;
        public float DestinationDamageRadius;
        public float DestinationDamage;
        public byte AllowPhaseShift;
        public float PhaseDurationSeconds;
        public byte AllowCloak;
        public float CloakDurationSeconds;
    }

    /// <summary>
    /// Game-agnostic temporal core tuning.
    /// </summary>
    public struct TimeCoreModuleConfig : IComponentData
    {
        public float StopDurationSeconds;
        public float SlowDurationSeconds;
        public float SlowTimeScale;
        public float CooldownSeconds;
        public byte AllowSlowFallback;
        public byte GlobalStop;
    }
}
