using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Scenario
{
    /// <summary>
    /// Interface for building deterministic scenarios from editor actions.
    /// Mirrors scenario JSON loader structure for seamless serialization.
    /// </summary>
    public interface IScenarioBuilder
    {
        /// <summary>
        /// Add an entity instance from a prefab at the specified position.
        /// </summary>
        void AddEntity(Entity prefab, float3 pos);

        /// <summary>
        /// Add or modify a component on an entity.
        /// </summary>
        void AddComponent<T>(Entity e, in T component) where T : unmanaged, IComponentData;

        /// <summary>
        /// Save the current scenario state to a JSON file.
        /// </summary>
        void SaveScenario(string path);
    }
}

