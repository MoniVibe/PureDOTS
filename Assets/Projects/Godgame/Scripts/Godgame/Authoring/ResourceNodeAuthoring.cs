using Godgame.Interaction;
using Godgame.Registry;
using PureDOTS.Runtime.Spatial;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Godgame.Authoring
{
    /// <summary>
    /// Authoring component for resource nodes (wood, ore, etc.) that villagers can harvest.
    /// Bakes DOTS components required for harvesting systems.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ResourceNodeAuthoring : MonoBehaviour
    {
        [SerializeField]
        private ResourceType resourceType = ResourceType.Wood;

        [SerializeField]
        [Tooltip("Initial amount of resource available")]
        private float initialAmount = 100f;

        [SerializeField]
        [Tooltip("Maximum capacity of this resource node")]
        private float maxAmount = 100f;

        [SerializeField]
        [Tooltip("Resource regeneration rate per second (0 = no regeneration)")]
        private float regenerationRate = 0f;

        private sealed class Baker : Unity.Entities.Baker<ResourceNodeAuthoring>
        {
            public override void Bake(ResourceNodeAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic | TransformUsageFlags.Renderable);

                // Convert ResourceType enum to index (Wood=1, Ore=2, etc.)
                ushort resourceTypeIndex = (ushort)authoring.resourceType;

                // Add resource node component
                AddComponent(entity, new GodgameResourceNode
                {
                    ResourceTypeIndex = resourceTypeIndex,
                    RemainingAmount = math.max(0f, authoring.initialAmount),
                    MaxAmount = math.max(authoring.initialAmount, authoring.maxAmount),
                    RegenerationRate = math.max(0f, authoring.regenerationRate)
                });

                // Add spatial indexing for efficient queries
                AddComponent<SpatialIndexedTag>(entity);
            }
        }
    }
}



