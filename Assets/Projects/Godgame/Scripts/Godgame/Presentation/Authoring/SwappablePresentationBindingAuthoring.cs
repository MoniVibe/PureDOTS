using Unity.Entities;
using UnityEngine;

namespace Godgame.Presentation.Authoring
{
    /// <summary>
    /// Authoring component for SwappablePresentationBinding.
    /// Allows configuring presentation prefab and variant index in the editor.
    /// </summary>
    public class SwappablePresentationBindingAuthoring : MonoBehaviour
    {
        [Header("Presentation")]
        [Tooltip("The prefab to use for this entity's presentation")]
        public GameObject Prefab;

        [Tooltip("Optional variant index for prefab variations")]
        public int VariantIndex;

        public class Baker : Baker<SwappablePresentationBindingAuthoring>
        {
            public override void Bake(SwappablePresentationBindingAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Renderable);
                
                if (authoring.Prefab != null)
                {
                    var prefabEntity = GetEntity(authoring.Prefab, TransformUsageFlags.Renderable);
                    AddComponent(entity, new SwappablePresentationBinding
                    {
                        Prefab = prefabEntity,
                        VariantIndex = authoring.VariantIndex
                    });
                }
            }
        }
    }
}

