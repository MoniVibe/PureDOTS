#if PUREDOTS_SCENARIO

using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Demo.Orbit
{
    /// <summary>Tag for our orbit demo cubes.</summary>
    public struct OrbitCubeTag : IComponentData { }

    /// <summary>Simple orbital motion parameters.</summary>
    public struct OrbitCube : IComponentData
    {
        public float3 Center;       // Center point to orbit around
        public float Radius;        // Distance from center
        public float AngularSpeed;  // radians per second
        public float Angle;         // current angle in radians
    }
}

#endif

