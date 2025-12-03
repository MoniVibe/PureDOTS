using Unity.Entities;

namespace Godgame.Presentation
{
    /// <summary>
    /// Component for entities that can swap their presentation prefab at runtime.
    /// This is a minimal stub to satisfy compile errors - refine semantics later.
    /// </summary>
    public struct SwappablePresentationBinding : IComponentData
    {
        /// <summary>
        /// The prefab entity to use for presentation.
        /// </summary>
        public Entity Prefab;

        /// <summary>
        /// Optional variant index for prefab variations.
        /// </summary>
        public int VariantIndex;
    }

    /// <summary>
    /// Tag component that marks an entity's presentation binding as dirty and needs refresh.
    /// </summary>
    public struct SwappablePresentationDirtyTag : IComponentData { }
}

