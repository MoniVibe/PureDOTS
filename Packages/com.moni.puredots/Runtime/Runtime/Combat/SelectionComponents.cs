using Unity.Entities;

namespace PureDOTS.Runtime.Combat
{
    /// <summary>
    /// Handle for selecting and commanding formations.
    /// Used by player input and AI strategic systems.
    /// </summary>
    public struct SelectionHandle : IComponentData
    {
        /// <summary>Selection identifier (unique per selection).</summary>
        public ulong Id;

        /// <summary>Formation entity reference.</summary>
        public Entity Aggregate;
    }
}

