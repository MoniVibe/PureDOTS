using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Physics
{
    /// <summary>
    /// Q-energy value computed from collision.
    /// Q = 0.5 * m_projectile * v^2 / m_target (J/kg).
    /// </summary>
    public struct QEnergy : IComponentData
    {
        /// <summary>
        /// Q value in J/kg.
        /// </summary>
        public float Value;

        /// <summary>
        /// Tick when Q was computed.
        /// </summary>
        public uint ComputationTick;
    }

    /// <summary>
    /// Macro energy field voxel element for energy density storage.
    /// Used for planetary-scale collision energy diffusion.
    /// </summary>
    public struct MacroEnergyFieldElement : IBufferElementData
    {
        /// <summary>
        /// Energy density in this voxel cell (J/m³).
        /// </summary>
        public float EnergyDensity;

        /// <summary>
        /// Voxel cell index (0-1023 for 1024 cell grid).
        /// </summary>
        public int CellIndex;
    }

    /// <summary>
    /// Macro thermal state for planetary-scale entities.
    /// Aggregates energy field data into visual/thermal parameters.
    /// </summary>
    public struct MacroThermoState : IComponentData
    {
        /// <summary>
        /// Average temperature across energy field in Kelvin.
        /// </summary>
        public float Temperature;

        /// <summary>
        /// Crust melt percentage (0.0 to 1.0).
        /// </summary>
        public float MeltPercentage;

        /// <summary>
        /// Atmosphere loss percentage (0.0 to 1.0).
        /// </summary>
        public float AtmosphereLossPercentage;

        /// <summary>
        /// Total energy in field (J).
        /// </summary>
        public float TotalEnergy;

        /// <summary>
        /// Last update tick.
        /// </summary>
        public uint LastUpdateTick;
    }

    /// <summary>
    /// Configuration for macro energy field diffusion.
    /// </summary>
    public struct MacroEnergyFieldConfig : IComponentData
    {
        /// <summary>
        /// Number of voxel cells per dimension (typically 512-1024).
        /// </summary>
        public int VoxelResolution;

        /// <summary>
        /// Diffusion coefficient k in dE/dt = k * ∇²E - loss * E.
        /// </summary>
        public float DiffusionCoefficient;

        /// <summary>
        /// Energy loss coefficient (loss term in diffusion equation).
        /// </summary>
        public float EnergyLossCoefficient;

        /// <summary>
        /// Q-to-temperature conversion factor (K per J/kg).
        /// </summary>
        public float QToTemperatureFactor;

        /// <summary>
        /// Energy-to-melt conversion factor (% melt per J/m³).
        /// </summary>
        public float EnergyToMeltFactor;

        /// <summary>
        /// Energy-to-atmosphere-loss conversion factor (% loss per J).
        /// </summary>
        public float EnergyToAtmosphereLossFactor;
    }
}

