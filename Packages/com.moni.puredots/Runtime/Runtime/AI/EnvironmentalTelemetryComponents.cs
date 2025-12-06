using PureDOTS.Environment;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.AI
{
    /// <summary>
    /// Environmental telemetry read by agents to evaluate comfort and trigger goals.
    /// Links biomes/climate to agent desires (seek temperate region, perform miracle, etc.).
    /// </summary>
    public struct EnvironmentalTelemetry : IComponentData
    {
        public BiomeType CurrentBiome;
        public float Temperature;        // Celsius
        public float Moisture;           // 0-100
        public float Comfort;            // 0-1 (computed comfort score)
        public float Light;              // 0-100
        public float Chemical;           // Pollutants (0-100)
        public uint LastUpdateTick;
    }

    /// <summary>
    /// Agent desire triggered by environmental conditions.
    /// </summary>
    public struct EnvironmentalDesire : IComponentData
    {
        public FixedString64Bytes DesireId; // e.g., "SeekTemperateRegion", "PerformMiracle"
        public float Urgency;                // 0-1 (how urgent this desire is)
        public float3 TargetPosition;        // Optional target location
    }
}

