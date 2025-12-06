using Unity.Entities;

namespace PureDOTS.Runtime.Components.Orbital
{
    /// <summary>
    /// Tag component marking entities needing orbital recomputation.
    /// Applied when:
    /// - Body enters/leaves another's sphere of influence
    /// - Player interaction/miracle applies delta-v
    /// Otherwise, entities propagate analytically.
    /// </summary>
    public struct OrbitalDirtyTag : IComponentData { }
}

