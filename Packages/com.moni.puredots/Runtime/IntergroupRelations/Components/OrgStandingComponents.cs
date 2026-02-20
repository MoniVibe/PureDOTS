using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.IntergroupRelations
{
    /// <summary>
    /// Per-entity sentiment toward an organization.
    /// </summary>
    [InternalBufferCapacity(8)]
    public struct EntityOrgStanding : IBufferElementData
    {
        public Entity OrgEntity;
        public sbyte Score;          // -100..100
        public byte Trust;           // 0..100
        public byte Familiarity;     // 0..100
        public uint LastInteractionTick;
    }

    /// <summary>
    /// Explicit organizational corruption state.
    /// </summary>
    public struct OrgCorruption : IComponentData
    {
        public float Level;            // 0..1
        public float RecentPressure;   // 0..1 delta applied on last update
        public uint LastUpdateTick;
    }
}
