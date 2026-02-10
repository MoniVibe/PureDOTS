using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.Spells
{
    /// <summary>
    /// Spell loadout metadata for quick selection or prepared slots.
    /// </summary>
    public struct SpellLoadout : IComponentData
    {
        public byte MaxSlots;
        public byte ActiveSlot;
    }

    /// <summary>
    /// Prepared spell slot entry.
    /// </summary>
    [InternalBufferCapacity(6)]
    public struct SpellSlot : IBufferElementData
    {
        public FixedString64Bytes SpellId;
        public byte Slot;
        public SpellSlotFlags Flags;
    }

    /// <summary>
    /// Spell slot flags for preparedness and favorites.
    /// </summary>
    public enum SpellSlotFlags : byte
    {
        None = 0,
        Prepared = 1,
        Favorite = 2,
        Reserved = 4
    }
}
