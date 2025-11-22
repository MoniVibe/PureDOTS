using Unity.Entities;

namespace PureDOTS.Runtime.Components
{
    /// <summary>
    /// Editor-only sentinel used to detect structural changes inside PresentationSystemGroup.
    /// </summary>
    public struct PresentationStructuralChangeSentinel : IComponentData
    {
        public int LastKnownOrderVersion;
    }
}
