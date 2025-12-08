using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Components
{
    /// <summary>
    /// Cold data components for presentation/UI/lore.
    /// These are stored separately from hot simulation components for cache efficiency.
    /// </summary>

    /// <summary>
    /// Presentation data for villagers (names, tooltips, UI state).
    /// Stored on companion entity or separate archetype.
    /// </summary>
    public struct VillagerPresentation : IComponentData
    {
        /// <summary>Display name for UI.</summary>
        public FixedString64Bytes DisplayName;
        
        /// <summary>Tooltip text shown on hover.</summary>
        public FixedString128Bytes Tooltip;
        
        /// <summary>UI state flags (selected, highlighted, etc.).</summary>
        public byte UIStateFlags;
        
        /// <summary>Last tick when presentation was updated.</summary>
        public uint LastUpdateTick;
    }

    /// <summary>
    /// Lore/flavor data for villagers (biography, personality traits).
    /// Stored on companion entity.
    /// </summary>
    public struct VillagerLore : IComponentData
    {
        /// <summary>Biography text.</summary>
        public FixedString512Bytes Biography;
        
        /// <summary>Personality traits (comma-separated).</summary>
        public FixedString128Bytes PersonalityTraits;
        
        /// <summary>Backstory/flavor text.</summary>
        public FixedString512Bytes Backstory;
    }

    /// <summary>
    /// Presentation data for villages (names, tooltips, UI state).
    /// </summary>
    public struct VillagePresentation : IComponentData
    {
        /// <summary>Display name for UI.</summary>
        public FixedString64Bytes DisplayName;
        
        /// <summary>Tooltip text shown on hover.</summary>
        public FixedString128Bytes Tooltip;
        
        /// <summary>UI state flags.</summary>
        public byte UIStateFlags;
        
        /// <summary>Last tick when presentation was updated.</summary>
        public uint LastUpdateTick;
    }

    /// <summary>
    /// Lore/flavor data for villages (history, culture, flavor text).
    /// </summary>
    public struct VillageLore : IComponentData
    {
        /// <summary>Village history text.</summary>
        public FixedString512Bytes History;
        
        /// <summary>Cultural description.</summary>
        public FixedString128Bytes Culture;
        
        /// <summary>Flavor text for events.</summary>
        public FixedString512Bytes FlavorText;
    }

    /// <summary>
    /// Reference from hot entity to cold presentation companion entity.
    /// </summary>
    public struct PresentationCompanionRef : IComponentData
    {
        /// <summary>Entity containing presentation components.</summary>
        public Entity CompanionEntity;
    }

    /// <summary>
    /// Reference from hot entity to cold lore companion entity.
    /// </summary>
    public struct LoreCompanionRef : IComponentData
    {
        /// <summary>Entity containing lore components.</summary>
        public Entity CompanionEntity;
    }

    /// <summary>
    /// Message buffer for sim-to-presentation communication.
    /// Written by simulation systems, read by presentation systems.
    /// </summary>
    public struct SimToPresentationMessage : IBufferElementData
    {
        public enum MessageType : byte
        {
            PositionUpdate = 0,
            StateUpdate = 1,
            AnimationUpdate = 2,
            HealthUpdate = 3,
            NeedsUpdate = 4
        }

        public MessageType Type;
        public Entity SourceEntity;
        public float3 Position;
        public float3 Velocity;
        public byte State;
        public float HealthPercent;
        public float HungerPercent;
        public float EnergyPercent;
        public uint Tick;
    }
}

