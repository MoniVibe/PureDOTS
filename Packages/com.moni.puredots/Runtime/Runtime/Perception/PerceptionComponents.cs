using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Perception
{
    /// <summary>
    /// Perception channels for cross-game reuse.
    /// Uses [Flags] pattern to allow flexible combinations.
    /// Games can extend with custom channels via bit positions 8-31.
    /// </summary>
    [System.Flags]
    public enum PerceptionChannel : uint
    {
        None = 0,
        
        // Standard channels (bits 0-7)
        Vision = 1 << 0,        // Visual detection (line of sight)
        Hearing = 1 << 1,      // Auditory detection (sound)
        Smell = 1 << 2,        // Olfactory detection
        EM = 1 << 3,           // Electromagnetic (radar/optical)
        Gravitic = 1 << 4,     // Gravitational detection
        Exotic = 1 << 5,       // Exotic physics detection
        Paranormal = 1 << 6,   // Magical/psychic detection
        Proximity = 1 << 7,    // Generic proximity detection
        
        // Custom channels (bits 8-31) reserved for game-specific extensions
        Custom0 = 1 << 8,
        Custom1 = 1 << 9,
        Custom2 = 1 << 10,
        Custom3 = 1 << 11,
        Custom4 = 1 << 12,
        Custom5 = 1 << 13,
        Custom6 = 1 << 14,
        Custom7 = 1 << 15,
        
        All = 0xFFFFFFFF
    }

    /// <summary>
    /// Per-entity sensor capability configuration.
    /// Extends SensorConfig with channel-based detection.
    /// Phase 1: Basic channel ranges and FOV.
    /// </summary>
    public struct SenseCapability : IComponentData
    {
        /// <summary>
        /// Bitmask of enabled perception channels.
        /// </summary>
        public PerceptionChannel EnabledChannels;

        /// <summary>
        /// Detection range per channel (indexed by channel bit position).
        /// For simplicity, Phase 1 uses a single range value.
        /// Phase 2: Per-channel ranges via blob asset or separate fields.
        /// </summary>
        public float Range;

        /// <summary>
        /// Field of view in degrees (360 = omnidirectional).
        /// Applies to channels that require LOS (Vision, EM).
        /// </summary>
        public float FieldOfView;

        /// <summary>
        /// Sensor acuity (0-1, affects confidence calculations).
        /// Higher = better detection quality.
        /// </summary>
        public float Acuity;

        /// <summary>
        /// Minimum time between sensor updates (seconds).
        /// </summary>
        public float UpdateInterval;

        /// <summary>
        /// Maximum entities to track simultaneously per channel.
        /// </summary>
        public byte MaxTrackedTargets;

        /// <summary>
        /// Capability flags (reuses SensorCapabilityFlags from SensorConfig).
        /// </summary>
        public byte Flags;

        /// <summary>
        /// Creates default sense capability.
        /// </summary>
        public static SenseCapability Default => new SenseCapability
        {
            EnabledChannels = PerceptionChannel.Vision | PerceptionChannel.Hearing,
            Range = 50f,
            FieldOfView = 120f,
            Acuity = 1f,
            UpdateInterval = 0.5f,
            MaxTrackedTargets = 8,
            Flags = 0
        };
    }

    /// <summary>
    /// Per-entity sensor signature (how detectable this entity is per channel).
    /// Extends Detectable with channel-specific signatures.
    /// Phase 1: Simple per-channel detectability values.
    /// </summary>
    public struct SensorSignature : IComponentData
    {
        /// <summary>
        /// Visual signature (0 = invisible, 1 = normal, >1 = conspicuous).
        /// </summary>
        public float VisualSignature;

        /// <summary>
        /// Auditory signature (0 = silent, 1 = normal, >1 = noisy).
        /// </summary>
        public float AuditorySignature;

        /// <summary>
        /// Olfactory signature (0 = odorless, 1 = normal, >1 = strong smell).
        /// </summary>
        public float OlfactorySignature;

        /// <summary>
        /// EM signature (0 = stealth, 1 = normal, >1 = high emissions).
        /// </summary>
        public float EMSignature;

        /// <summary>
        /// Gravitic signature (0 = no gravity, 1 = normal, >1 = massive).
        /// </summary>
        public float GraviticSignature;

        /// <summary>
        /// Exotic signature (0 = undetectable, 1 = normal, >1 = exotic).
        /// </summary>
        public float ExoticSignature;

        /// <summary>
        /// Paranormal signature (0 = mundane, 1 = normal, >1 = magical).
        /// </summary>
        public float ParanormalSignature;

        /// <summary>
        /// Gets signature for a specific channel.
        /// </summary>
        public float GetSignature(PerceptionChannel channel)
        {
            return channel switch
            {
                PerceptionChannel.Vision => VisualSignature,
                PerceptionChannel.Hearing => AuditorySignature,
                PerceptionChannel.Smell => OlfactorySignature,
                PerceptionChannel.EM => EMSignature,
                PerceptionChannel.Gravitic => GraviticSignature,
                PerceptionChannel.Exotic => ExoticSignature,
                PerceptionChannel.Paranormal => ParanormalSignature,
                PerceptionChannel.Proximity => 1f, // Always detectable via proximity
                _ => 1f // Default for unknown channels
            };
        }

        /// <summary>
        /// Creates default signature (normal detectability).
        /// </summary>
        public static SensorSignature Default => new SensorSignature
        {
            VisualSignature = 1f,
            AuditorySignature = 1f,
            OlfactorySignature = 1f,
            EMSignature = 1f,
            GraviticSignature = 1f,
            ExoticSignature = 1f,
            ParanormalSignature = 1f
        };
    }

    /// <summary>
    /// Perceived entity entry in PerceptionState buffer.
    /// Stores what an entity currently perceives about another entity.
    /// </summary>
    [InternalBufferCapacity(16)]
    public struct PerceivedEntity : IBufferElementData
    {
        /// <summary>
        /// The perceived entity.
        /// </summary>
        public Entity TargetEntity;

        /// <summary>
        /// Channel(s) this entity was detected on (bitmask).
        /// </summary>
        public PerceptionChannel DetectedChannels;

        /// <summary>
        /// Detection confidence (0-1, 1 = certain).
        /// </summary>
        public float Confidence;

        /// <summary>
        /// Distance to target.
        /// </summary>
        public float Distance;

        /// <summary>
        /// Direction to target (normalized).
        /// </summary>
        public float3 Direction;

        /// <summary>
        /// Tick when first detected.
        /// </summary>
        public uint FirstDetectedTick;

        /// <summary>
        /// Tick when last seen/updated.
        /// </summary>
        public uint LastSeenTick;

        /// <summary>
        /// Target's threat level (0-255).
        /// </summary>
        public byte ThreatLevel;

        /// <summary>
        /// Target's relationship (-128 = enemy, 0 = neutral, +127 = ally).
        /// </summary>
        public sbyte Relationship;
    }

    /// <summary>
    /// Perception state tracking component.
    /// Tracks what an entity currently perceives.
    /// </summary>
    public struct PerceptionState : IComponentData
    {
        /// <summary>
        /// Last tick perception was updated.
        /// </summary>
        public uint LastUpdateTick;

        /// <summary>
        /// Number of entities currently perceived.
        /// </summary>
        public byte PerceivedCount;

        /// <summary>
        /// Highest threat level among perceived entities.
        /// </summary>
        public byte HighestThreat;

        /// <summary>
        /// Entity with highest threat (for quick access).
        /// </summary>
        public Entity HighestThreatEntity;

        /// <summary>
        /// Nearest perceived entity.
        /// </summary>
        public Entity NearestEntity;

        /// <summary>
        /// Distance to nearest entity.
        /// </summary>
        public float NearestDistance;
    }

    /// <summary>
    /// Channel detection rule configuration (pluggable rules).
    /// Phase 1: Simple data structure.
    /// Phase 2: Blob asset with complex rules, LOS queries, etc.
    /// </summary>
    public struct ChannelDetectionRule : IComponentData
    {
        /// <summary>
        /// Channel this rule applies to.
        /// </summary>
        public PerceptionChannel Channel;

        /// <summary>
        /// Whether this channel requires line of sight.
        /// </summary>
        public byte RequiresLOS;

        /// <summary>
        /// Range decay factor (0 = no decay, 1 = linear decay).
        /// </summary>
        public float RangeDecayFactor;

        /// <summary>
        /// Whether this channel can detect through obstacles.
        /// </summary>
        public byte CanDetectThroughObstacles;

        /// <summary>
        /// Creates default rule for a channel.
        /// </summary>
        public static ChannelDetectionRule Default(PerceptionChannel channel)
        {
            return new ChannelDetectionRule
            {
                Channel = channel,
                RequiresLOS = (byte)((channel == PerceptionChannel.Vision || channel == PerceptionChannel.EM) ? 1 : 0),
                RangeDecayFactor = 1f,
                CanDetectThroughObstacles = 0
            };
        }
    }
}

