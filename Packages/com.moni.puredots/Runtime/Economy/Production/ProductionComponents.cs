using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.Economy.Production
{
    /// <summary>
    /// Production stage enum.
    /// </summary>
    public enum ProductionStage : byte
    {
        Refining = 0,
        Crafting = 1,
        Enchanting = 2
    }

    /// <summary>
    /// Business type enum.
    /// </summary>
    public enum BusinessType : byte
    {
        Blacksmith = 0,
        Sawmill = 1,
        Quarry = 2,
        Mill = 3,
        Herbalist = 4,
        Wainwright = 5,
        Builder = 6,
        Alchemist = 7
    }

    /// <summary>
    /// Business production component.
    /// Tracks recipe queue, capacity, throughput for a business.
    /// </summary>
    public struct BusinessProduction : IComponentData
    {
        public BusinessType Type;
        public float Capacity;
        public float Throughput;
        public uint LastUpdateTick;
    }

    /// <summary>
    /// Supervisor policy knobs for production outcomes.
    /// Bias values are 0-1 where 0.5 = neutral.
    /// </summary>
    public struct ProductionSupervisorPolicy : IComponentData
    {
        public float QualityBias01;
        public float ThroughputBias01;
        public float RiskTolerance01;

        public static ProductionSupervisorPolicy Neutral => new ProductionSupervisorPolicy
        {
            QualityBias01 = 0.5f,
            ThroughputBias01 = 0.5f,
            RiskTolerance01 = 0.5f
        };
    }

    /// <summary>
    /// Production job buffer element.
    /// Represents a single production job in the queue.
    /// </summary>
    public struct ProductionJob : IBufferElementData
    {
        public FixedString64Bytes RecipeId;
        public Entity Worker; // Assigned worker entity
        public float Progress; // 0-1, completion progress
        public float BaseTimeCost; // Worker-hours required
        public float RemainingTime; // Worker-hours remaining
        public uint StartTick;
        public uint EstimatedCompletionTick;
    }

    /// <summary>
    /// Reference to business inventory entity.
    /// </summary>
    public struct BusinessInventory : IComponentData
    {
        public Entity InventoryEntity;
    }
}
