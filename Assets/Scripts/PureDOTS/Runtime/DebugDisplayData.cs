using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.Components
{
    public struct DebugDisplayData : IComponentData
    {
        public uint CurrentTick;
        public bool IsPaused;
        public FixedString128Bytes TimeStateText;
        public FixedString128Bytes RewindStateText;
        public int VillagerCount;
        public float TotalResourcesStored;
    }
}


