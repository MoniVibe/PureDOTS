using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Combat
{
    /// <summary>
    /// Lightweight threat sample used by deflection planning.
    /// </summary>
    [InternalBufferCapacity(8)]
    public struct DeflectionThreat : IBufferElementData
    {
        public Entity Projectile;
        public Entity Source;
        public float ThreatScore;
        public float TimeToImpact;
        public float Distance;
        public float DodgeDifficulty;
        public float DeflectResistance;
        public float ControlResistance;
        public float3 IncomingDirection;
        public uint SampleTick;
    }

    /// <summary>
    /// Sensing settings for deflection threat evaluation.
    /// </summary>
    public struct DeflectionSense : IComponentData
    {
        public float Range;
        public int MaxThreats;
    }
}
