using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Runtime.Scenario
{
    /// <summary>
    /// System for managing editor gizmos and selection in the editor world.
    /// Tracks selected entities and provides placement functionality.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct EditorGizmoSystem : ISystem
    {
        private EntityQuery _selectedEntitiesQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _selectedEntitiesQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<EditorSelectedTag>(),
                ComponentType.ReadOnly<LocalTransform>());
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Gizmo rendering happens in MonoBehaviour layer
            // This system just manages selection state
        }
    }

    /// <summary>
    /// Tag component marking entities as selected in the editor.
    /// </summary>
    public struct EditorSelectedTag : IComponentData
    {
    }

    /// <summary>
    /// Component storing placement data for entities being placed in the editor.
    /// </summary>
    public struct EditorPlacementData : IComponentData
    {
        public float3 TargetPosition;
        public Entity PrefabEntity;
        public bool IsPlacing;
    }
}

