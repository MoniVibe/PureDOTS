using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Components
{
    /// <summary>
    /// Types of miracles available in the game.
    /// </summary>
    public enum MiracleType : byte
    {
        None = 0,
        BlessRegion = 1,
        CurseRegion = 2,
        RestoreBiome = 3,
        Fireball = 4,
        Heal = 5,
        Shield = 6,
        Lightning = 7,
        Earthquake = 8,
        Forest = 9,
        Freeze = 10,
        Food = 11,
        Meteor = 12,
        Rain = 13, // Legacy/placeholder
    }

    /// <summary>
    /// How a miracle is cast (instant, sustained, thrown, etc.).
    /// </summary>
    public enum MiracleCastingMode : byte
    {
        Instant = 0,
        Sustained = 1,
        Thrown = 2,
        Area = 3,
    }

    /// <summary>
    /// Current lifecycle state of a miracle instance.
    /// </summary>
    public enum MiracleLifecycleState : byte
    {
        Charging = 0,
        Active = 1,
        Sustaining = 2,
        Cooldown = 3,
        Expired = 4,
    }

    /// <summary>
    /// Definition of a miracle type, containing base properties.
    /// </summary>
    public struct MiracleDefinition : IComponentData
    {
        public MiracleType Type;
        public MiracleCastingMode CastingMode;
        public float BaseRadius;
        public float BaseIntensity;
        public float BaseCost;
        public float SustainedCostPerSecond;
    }

    /// <summary>
    /// Runtime state of an active miracle instance.
    /// </summary>
    public struct MiracleRuntimeState : IComponentData
    {
        public MiracleLifecycleState Lifecycle;
        public float ChargePercent;
        public float CurrentRadius;
        public float CurrentIntensity;
        public float CooldownSecondsRemaining;
        public uint LastCastTick;
        public byte AlignmentDelta;
    }

    /// <summary>
    /// Token representing an active miracle instance.
    /// Used to track and identify miracles in the system.
    /// </summary>
    public struct MiracleToken : IComponentData
    {
        public int Id;
        public MiracleType Type;
        public Entity CasterEntity;
        public Entity ConfigEntity;
    }

    /// <summary>
    /// Identifies the entity that cast a miracle and the hand that performed the cast.
    /// </summary>
    public struct MiracleCaster : IComponentData
    {
        public Entity CasterEntity;
        public Entity HandEntity;
    }

    /// <summary>
    /// Effect component applied by a miracle to a target entity.
    /// </summary>
    public struct MiracleEffect : IComponentData
    {
        public MiracleToken Token;
        public float Magnitude;
        public float Duration;
        public float RemainingDuration;
    }

    /// <summary>
    /// Component for region-scale miracle effects.
    /// Applies to biome/ground tile regions rather than individual entities.
    /// </summary>
    public struct RegionMiracleEffect : IComponentData
    {
        /// <summary>Region entity (biome/ground tile region)</summary>
        public Entity RegionEntity;
        /// <summary>Center position of the effect</summary>
        public float3 CenterPosition;
        /// <summary>Radius of the effect</summary>
        public float Radius;
        /// <summary>Miracle type</summary>
        public MiracleType Type;
        /// <summary>Intensity (0-1)</summary>
        public float Intensity;
        /// <summary>Total duration</summary>
        public float Duration;
        /// <summary>Remaining duration</summary>
        public float RemainingDuration;
    }

    /// <summary>
    /// Defines an available miracle slot on a caster, including prefab/config binding.
    /// </summary>
    [InternalBufferCapacity(4)]
    public struct MiracleSlotDefinition : IBufferElementData
    {
        public byte SlotIndex;
        public MiracleType Type;
        public Entity MiraclePrefab;
        public Entity ConfigEntity;
    }

    /// <summary>
    /// Event raised by input/presentation to trigger a miracle release.
    /// </summary>
    [InternalBufferCapacity(4)]
    public struct MiracleReleaseEvent : IBufferElementData
    {
        public MiracleType Type;
        public float3 Position;
        public float3 Direction;
        public float Impulse;
        public Entity ConfigEntity;
    }

    /// <summary>
    /// Simple request struct designers can enqueue via authoring triggers to force a miracle spawn for preview.
    /// </summary>
    [InternalBufferCapacity(4)]
    public struct MiracleDesignerTrigger : IBufferElementData
    {
        public FixedString64Bytes DescriptorKey;
        public float3 Position;
        public MiracleType Type;
    }

    /// <summary>
    /// Component used alongside the MiracleDesignerTrigger buffer to identify the source.
    /// </summary>
    public struct MiracleDesignerTriggerSource : IComponentData
    {
        public Entity ProfileEntity;
        public MiracleType Type;
        public float3 Offset;
    }
}

