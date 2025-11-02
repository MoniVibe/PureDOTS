using Unity.Entities;

namespace Godgame.Registry
{
    /// <summary>
    /// Resource node component for wood, ore, and other harvestable resources.
    /// Used by harvesting systems to find and interact with resource locations.
    /// </summary>
    public struct GodgameResourceNode : IComponentData
    {
        /// <summary>
        /// Resource type index mapping to ResourceType enum (Wood=1, Ore=2, etc.)
        /// </summary>
        public ushort ResourceTypeIndex;

        /// <summary>
        /// Current remaining amount of resource in this node.
        /// </summary>
        public float RemainingAmount;

        /// <summary>
        /// Maximum capacity of this resource node.
        /// </summary>
        public float MaxAmount;

        /// <summary>
        /// Regeneration rate per second (0 = no regeneration).
        /// </summary>
        public float RegenerationRate;
    }

    /// <summary>
    /// Component indicating a villager is carrying harvested resources.
    /// Added when villager starts harvesting, removed when depositing at storehouse.
    /// </summary>
    public struct VillagerCarrying : IComponentData
    {
        /// <summary>
        /// Resource type index of what the villager is carrying.
        /// </summary>
        public ushort ResourceTypeIndex;

        /// <summary>
        /// Current amount being carried.
        /// </summary>
        public float Amount;

        /// <summary>
        /// Maximum carrying capacity for this villager.
        /// </summary>
        public float MaxCarryCapacity;
    }
}




