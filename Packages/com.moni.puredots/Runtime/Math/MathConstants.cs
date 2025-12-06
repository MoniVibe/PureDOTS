using Unity.Mathematics;

namespace PureDOTS.Runtime.Math
{
    /// <summary>
    /// Shared mathematical constants used across PureDOTS systems.
    /// All constants are Burst-safe and deterministic.
    /// </summary>
    public static class MathConstants
    {
        // Gravitational constants
        public const float GravitationalConstant = 6.67430e-11f; // G in m^3 kg^-1 s^-2
        public const float EarthGravity = 9.80665f; // m/s^2
        public const float StandardGravity = 9.80665f; // m/s^2

        // Atmospheric constants
        public const float StandardAtmosphericPressure = 101325f; // Pa (Pascals)
        public const float AirDensitySeaLevel = 1.225f; // kg/m^3 at 15°C
        public const float GasConstant = 287.05f; // J/(kg·K) for dry air
        public const float LapseRate = 0.0065f; // K/m (temperature decrease with altitude)

        // Diffusion coefficients
        public const float ThermalDiffusivity = 2.3e-5f; // m^2/s for air
        public const float MolecularDiffusionCoefficient = 2.0e-5f; // m^2/s typical

        // Orbital mechanics
        public const float AstronomicalUnit = 1.496e11f; // meters
        public const float SolarMass = 1.989e30f; // kg
        public const float EarthMass = 5.972e24f; // kg

        // Time constants
        public const float SecondsPerDay = 86400f;
        public const float SecondsPerHour = 3600f;
        public const float SecondsPerMinute = 60f;

        // Conversion factors
        public const float RadiansToDegrees = 57.29577951308232f;
        public const float DegreesToRadians = 0.017453292519943296f;
        public const float MetersToKilometers = 0.001f;
        public const float KilometersToMeters = 1000f;
    }
}

