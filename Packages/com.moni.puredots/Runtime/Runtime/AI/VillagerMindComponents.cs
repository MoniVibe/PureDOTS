using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.AI
{
    /// <summary>
    /// Enumerates shared villager need channels.
    /// </summary>
    public enum VillagerNeed : byte
    {
        Hunger = 0,
        Rest = 1,
        Faith = 2,
        Safety = 3,
        Social = 4,
        Work = 5
    }

    /// <summary>
    /// High-level villager goals shared across games.
    /// </summary>
    public enum VillagerGoal : byte
    {
        Idle = 0,
        Work = 1,
        SeekShelter = 2,
        Eat = 3,
        Sleep = 4,
        Pray = 5,
        Socialize = 6,
        Flee = 7
    }

    /// <summary>
    /// Tracks the most recent goal selection and urgency metadata.
    /// </summary>
    public struct VillagerGoalState : IComponentData
    {
        public VillagerGoal CurrentGoal;
        public VillagerGoal PreviousGoal;
        public uint LastGoalChangeTick;
        public float CurrentGoalUrgency;
    }

    /// <summary>
    /// Per-need urgency where values are clamped to [0, 1].
    /// </summary>
    public struct VillagerNeedState : IComponentData
    {
        public float HungerUrgency;
        public float RestUrgency;
        public float FaithUrgency;
        public float SafetyUrgency;
        public float SocialUrgency;
        public float WorkUrgency;
    }

    /// <summary>
    /// Need pressure tuning applied by the mind loop.
    /// </summary>
    public struct VillagerNeedTuning : IComponentData
    {
        public float HungerDecayPerTick;
        public float RestDecayPerTick;
        public float FaithDecayPerTick;
        public float SafetyDecayPerTick;
        public float SocialDecayPerTick;
        public float WorkPressurePerTick;
        public float MaxUrgency;
    }

    /// <summary>
    /// Threat perception data populated by sensors or hazard systems.
    /// </summary>
    public struct VillagerThreatState : IComponentData
    {
        public Entity ThreatEntity;
        public float3 ThreatDirection;
        public float Urgency;
        public byte HasLineOfSight;
    }

    /// <summary>
    /// Intent emitted when a villager should flee an active threat.
    /// </summary>
    public struct VillagerFleeIntent : IComponentData
    {
        public Entity ThreatEntity;
        public float3 ExitDirection;
        public float Urgency;
        public byte RequiresLineOfSight;
    }

    /// <summary>
    /// Cadence override that allows slower mind loops per entity.
    /// </summary>
    public struct VillagerMindCadence : IComponentData
    {
        public int CadenceTicks;
        public uint LastRunTick;
    }
}
