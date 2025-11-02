using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.Resource
{
    public struct ResourceProcessorConfig : IComponentData
    {
        public FixedString32Bytes FacilityTag;
        public byte AutoRun;
    }

    public struct ResourceProcessorState : IComponentData
    {
        public FixedString64Bytes RecipeId;
        public FixedString64Bytes OutputResourceId;
        public ResourceRecipeKind Kind;
        public int OutputAmount;
        public float RemainingSeconds;
    }

    public struct ResourceProcessorQueue : IBufferElementData
    {
        public FixedString64Bytes RecipeId;
        public int Repeat;
    }
}

