#if PUREDOTS_SCENARIO

using UnityEngine;
using Unity.Entities;
using Unity.Rendering;

namespace PureDOTS.Demo.Rendering
{
    // Shared component for debug render mesh array (managed).
    public struct RenderMeshArraySingleton : ISharedComponentData, System.IEquatable<RenderMeshArraySingleton>
    {
        // Must remain 'Value' to match consumers (e.g., VisualProfiles).
        public RenderMeshArray Value;

        public bool Equals(RenderMeshArraySingleton other)
        {
            return ReferenceEquals(Value, other.Value);
        }

        public override bool Equals(object obj)
        {
            return obj is RenderMeshArraySingleton other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value != null ? Value.GetHashCode() : 0;
        }
    }

    [DisallowMultipleComponent]
    public sealed class UniversalDebugRenderConfigAuthoring : MonoBehaviour
    {
        public Mesh DebugMesh;
        public Material DebugMaterial;
    }

    public struct UniversalDebugRenderConfigTag : IComponentData {}

    // Baker<T> lives in Unity.Entities namespace (assembly Unity.Entities.Hybrid).
    public sealed class UniversalDebugRenderConfigBaker : Baker<UniversalDebugRenderConfigAuthoring>
    {
        public override void Bake(UniversalDebugRenderConfigAuthoring authoring)
        {
            if (authoring.DebugMesh == null || authoring.DebugMaterial == null)
            {
                Debug.LogError("[UniversalDebugRenderConfigBaker] DebugMesh or DebugMaterial is missing.");
                return;
            }

            var entity = GetEntity(TransformUsageFlags.Renderable);

            var renderMeshArray = new RenderMeshArray(
                new[] { authoring.DebugMaterial },
                new[] { authoring.DebugMesh }
            );

            AddSharedComponentManaged(entity, new RenderMeshArraySingleton
            {
                Value = renderMeshArray
            });
        }
    }
}

#endif

