using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Mathematics.Geometry;

namespace PureDOTS.Runtime.Spatial
{
    /// <summary>
    /// Interface for unified spatial queries across different domains.
    /// </summary>
    public interface ISpatialQuery
    {
        /// <summary>
        /// Queries entities within the specified AABB bounds.
        /// </summary>
        /// <param name="bounds">Axis-aligned bounding box</param>
        /// <param name="results">Output list of entities</param>
        /// <returns>True if query succeeded</returns>
        bool Query(in AABB bounds, out NativeList<Entity> results);
    }
}

