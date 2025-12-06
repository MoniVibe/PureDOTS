using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Environment
{
    /// <summary>
    /// Climate state for a ship biodeck (mini-biome ECS).
    /// Self-contained environmental simulation running at reduced tick rate.
    /// </summary>
    public struct BiodeckClimate : IComponentData
    {
        public float Temperature;      // Celsius
        public float Humidity;         // 0-100%
        public float Light;            // 0-100 (combined direct + ambient)
        public uint LastUpdateTick;
    }

    /// <summary>
    /// Atmospheric composition for a biodeck.
    /// </summary>
    public struct BiodeckAtmosphere : IComponentData
    {
        public float Oxygen;           // O₂ percentage (0-100)
        public float CarbonDioxide;    // CO₂ percentage (0-100)
        public float Pressure;         // Atmospheric pressure (kPa)
    }

    /// <summary>
    /// Flora species composition buffer for a biodeck.
    /// </summary>
    [InternalBufferCapacity(0)]
    public struct BiodeckFloraBuffer : IBufferElementData
    {
        public int SpeciesId;
        public float Coverage;         // 0-1 (coverage fraction)
    }

    /// <summary>
    /// Link to ship modules that affect biodeck climate.
    /// </summary>
    public struct BiodeckModuleLink : IComponentData
    {
        public Entity ReactorEntity;      // Provides energy/heat
        public Entity HullEntity;         // Affects pressure (breaches → pressure drop)
        public Entity RadiationShieldEntity; // Affects heat (leaks → heat delta)
    }

    /// <summary>
    /// Configuration for biodeck simulation cadence.
    /// </summary>
    public struct BiodeckSimulationConfig : IComponentData
    {
        public uint TickDivisor;       // Update every N ticks (default: 2 = 0.5 Hz)
        public float TargetTemperature; // Desired temperature (°C)
        public float TargetHumidity;    // Desired humidity (0-100%)
        public float TargetOxygen;      // Desired O₂ (0-100%)
    }
}

