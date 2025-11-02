#if UNITY_EDITOR
using PureDOTS.Runtime.Spatial;
using Space4X.Runtime;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Space4X.Authoring
{
    /// <summary>
    /// Authoring component for carrier entities that receive resources from mining vessels.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CarrierAuthoring : MonoBehaviour
    {
        [Header("Carrier Identity")]
        [Tooltip("Unique identifier for this carrier")]
        public int carrierId = 0;

        [Header("Storage")]
        [Tooltip("Total storage capacity for all resources combined")]
        [Min(0f)] public float totalCapacity = 1000f;
    }

    public sealed class CarrierBaker : Baker<CarrierAuthoring>
    {
        public override void Bake(CarrierAuthoring authoring)
        {
            var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);

            // Add carrier component
            AddComponent(entity, new Carrier
            {
                CarrierId = authoring.carrierId,
                TotalCapacity = authoring.totalCapacity,
                CurrentLoad = 0f,
                LastUpdateTick = 0
            });

            // Add inventory buffer
            AddBuffer<CarrierInventoryItem>(entity);

            // Add spatial tag for spatial queries
            AddComponent<SpatialIndexedTag>(entity);
        }
    }
}
#endif

