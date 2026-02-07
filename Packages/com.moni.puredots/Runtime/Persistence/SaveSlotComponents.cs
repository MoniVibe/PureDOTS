using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.Persistence
{
    /// <summary>
    /// Save slot action for persistence requests.
    /// </summary>
    public enum SaveSlotAction : byte
    {
        Save = 0,
        Load = 1,
        Delete = 2
    }

    /// <summary>
    /// Flags to control persistence behavior.
    /// </summary>
    [System.Flags]
    public enum SaveSlotRequestFlags : byte
    {
        None = 0
    }

    /// <summary>
    /// Request to save/load/delete a slot.
    /// </summary>
    public struct SaveSlotRequest : IComponentData
    {
        public int SlotIndex;
        public SaveSlotAction Action;
        public SaveSlotRequestFlags Flags;
        public uint RequestedTick;
        public FixedString64Bytes Label;
    }
}
