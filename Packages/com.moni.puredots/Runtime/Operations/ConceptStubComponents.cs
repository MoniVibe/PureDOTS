using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Operations
{
    // STUB: shared operational contracts to unblock system wiring ahead of full specs.

    public struct ExplorationOrder : IComponentData
    {
        public int OrderId;
        public float3 TargetPosition;
        public byte Depth; // quick/standard/deep
    }

    public struct ThreatSignature : IComponentData
    {
        public float Strength;
        public byte Category; // fauna/pirate/empire/anomaly
    }

    public struct IntelSample : IComponentData
    {
        public int SampleId;
        public uint Timestamp;
    }
}
