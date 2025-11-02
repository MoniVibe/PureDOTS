#if UNITY_EDITOR
using Space4X.Runtime;
using Space4X.Runtime.Transport;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Space4X.Authoring
{
    /// <summary>
    /// Authoring component for mining vessels.
    /// Adds this to mining vessel GameObjects so they have movement and AI components.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MiningVesselAuthoring : MonoBehaviour
    {
        [Header("Movement")]
        [Range(0.1f, 20f)] public float baseSpeed = 5f;

        [Header("Mining")]
        [Range(1f, 100f)] public float capacity = 50f;
        public ushort resourceTypeIndex = 0; // 0 = ore (can be expanded later)
    }

    public sealed class MiningVesselBaker : Baker<MiningVesselAuthoring>
    {
        public override void Bake(MiningVesselAuthoring authoring)
        {
            var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);

            // Add miner vessel component
            AddComponent(entity, new MinerVessel
            {
                ResourceTypeIndex = authoring.resourceTypeIndex,
                Capacity = authoring.capacity,
                Load = 0f,
                Flags = TransportUnitFlags.Idle,
                LastCommandTick = 0
            });

            // Add vessel AI state
            AddComponent(entity, new VesselAIState
            {
                CurrentState = VesselAIState.State.Idle,
                CurrentGoal = VesselAIState.Goal.None,
                TargetEntity = Entity.Null,
                TargetPosition = float3.zero,
                StateTimer = 0f,
                StateStartTick = 0
            });

            // Add vessel movement
            AddComponent(entity, new VesselMovement
            {
                Velocity = float3.zero,
                BaseSpeed = authoring.baseSpeed,
                CurrentSpeed = authoring.baseSpeed,
                DesiredRotation = quaternion.identity,
                IsMoving = 0,
                LastMoveTick = 0
            });

            // LocalTransform should be added automatically with TransformUsageFlags.Dynamic.
            // It will be synced from the GameObject transform automatically.
            // No need to manually set it here - Unity handles the sync for Dynamic transforms.
        }
    }
}
#endif

