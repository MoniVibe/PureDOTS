using Unity.Mathematics;

namespace PureDOTS.Runtime.Spatial
{
    /// <summary>
    /// Simple axis-aligned bounding box used by spatial helpers.
    /// </summary>
    public struct AABB
    {
        public float3 Min;
        public float3 Max;

        public float3 Center => (Min + Max) * 0.5f;

        public float3 Extents => (Max - Min) * 0.5f;

        public bool Contains(float3 point)
        {
            return math.all(point >= Min) && math.all(point <= Max);
        }

        public static AABB FromCenterExtents(float3 center, float3 extents)
        {
            return new AABB
            {
                Min = center - extents,
                Max = center + extents
            };
        }
    }
}
