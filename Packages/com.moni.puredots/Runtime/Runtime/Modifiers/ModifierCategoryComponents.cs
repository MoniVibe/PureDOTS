using Unity.Entities;

namespace PureDOTS.Runtime.Modifiers
{
    /// <summary>
    /// Economic modifiers singleton (income, upkeep, etc.).
    /// Aggregated from all entities with economic modifiers.
    /// </summary>
    public struct EconomicModifiers : IComponentData
    {
        public float IncomeAdd;
        public float IncomeMul;
        public float UpkeepAdd;
        public float UpkeepMul;
    }

    /// <summary>
    /// Combat modifiers singleton (morale, damage, etc.).
    /// Aggregated from all entities with military modifiers.
    /// </summary>
    public struct CombatModifiers : IComponentData
    {
        public float MoraleAdd;
        public float MoraleMul;
        public float DamageAdd;
        public float DamageMul;
    }

    /// <summary>
    /// World/environment modifiers component (temperature, fertility, etc.).
    /// Attached to world/region entities.
    /// </summary>
    public struct WorldModifiers : IComponentData
    {
        public float TemperatureAdd;
        public float TemperatureMul;
        public float FertilityAdd;
        public float FertilityMul;
    }
}

