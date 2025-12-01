using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Spatial;
using PureDOTS.Runtime.Time;
using PureDOTS.Runtime.Villager;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Systems
{
    /// <summary>
    /// Computes villager morale and alignment shifts based on needs, miracles, and creature actions.
    /// Runs after VillagerNeedsSystem and VillagerStatusSystem to compute mood from wellbeing.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(VillagerSystemGroup))]
    [UpdateAfter(typeof(VillagerStatusSystem))]
    public partial struct VillagerMoodSystem : ISystem
    {
        private BufferLookup<MiracleRegistryEntry> _miracleRegistryLookup;
        private ComponentLookup<LocalTransform> _transformLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();

            _miracleRegistryLookup = state.GetBufferLookup<MiracleRegistryEntry>(true);
            _transformLookup = state.GetComponentLookup<LocalTransform>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            if (timeState.IsPaused)
            {
                return;
            }

            var rewindState = SystemAPI.GetSingleton<RewindState>();
            if (rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            // Get villager behavior config or use defaults
            var config = SystemAPI.HasSingleton<VillagerBehaviorConfig>()
                ? SystemAPI.GetSingleton<VillagerBehaviorConfig>()
                : VillagerBehaviorConfig.CreateDefaults();

            _miracleRegistryLookup.Update(ref state);
            _transformLookup.Update(ref state);

            // Find miracle registry entity
            Entity miracleRegistryEntity = Entity.Null;
            foreach (var (_, entity) in SystemAPI.Query<RefRO<MiracleRegistry>>().WithEntityAccess())
            {
                miracleRegistryEntity = entity;
                break;
            }

            var job = new UpdateMoodJob
            {
                DeltaTime = timeState.FixedDeltaTime,
                CurrentTick = timeState.Tick,
                MoodLerpRate = config.MoraleLerpRate,
                AlignmentInfluenceRate = config.AlignmentInfluenceRate,
                MiracleAlignmentBonus = config.MiracleAlignmentBonus,
                CreatureAlignmentBonus = config.CreatureAlignmentBonus,
                MiracleInfluenceRange = config.MiracleInfluenceRange,
                AlignmentDecayRate = config.AlignmentDecayRate,
                MiracleRegistryEntity = miracleRegistryEntity,
                MiracleRegistryLookup = _miracleRegistryLookup,
                TransformLookup = _transformLookup
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        public partial struct UpdateMoodJob : IJobEntity
        {
            public float DeltaTime;
            public uint CurrentTick;
            public float MoodLerpRate;
            public float AlignmentInfluenceRate;
            public float MiracleAlignmentBonus;
            public float CreatureAlignmentBonus;
            public float MiracleInfluenceRange;
            public float AlignmentDecayRate;
            public Entity MiracleRegistryEntity;
            [ReadOnly] public BufferLookup<MiracleRegistryEntry> MiracleRegistryLookup;
            [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;

            public void Execute(Entity entity, ref VillagerMood mood, in VillagerNeeds needs, in VillagerAvailability availability, in LocalTransform transform)
            {
                // Mood is primarily driven by wellbeing (computed in VillagerStatusSystem)
                // Here we handle alignment shifts and external influences

                // Lerp mood toward target (set by VillagerStatusSystem based on wellbeing)
                var adjust = math.clamp(DeltaTime * mood.MoodChangeRate, 0f, 1f);
                mood.Mood = math.lerp(mood.Mood, mood.TargetMood, adjust);

                // Calculate alignment influence from nearby miracles
                var miracleInfluence = CalculateMiracleInfluence(transform.Position);

                // Apply alignment shift
                if (miracleInfluence > 0f)
                {
                    // Positive influence from miracles - shift toward alignment (100)
                    var alignmentDelta = miracleInfluence * AlignmentInfluenceRate * DeltaTime;
                    mood.Alignment = math.min(100f, mood.Alignment + alignmentDelta);
                    mood.LastAlignmentInfluenceTick = CurrentTick;
                }
                else
                {
                    // No active influence - decay toward neutral (50)
                    var neutralAlignment = 50f;
                    var decayDelta = AlignmentDecayRate * DeltaTime;
                    
                    if (mood.Alignment > neutralAlignment)
                    {
                        mood.Alignment = math.max(neutralAlignment, mood.Alignment - decayDelta);
                    }
                    else if (mood.Alignment < neutralAlignment)
                    {
                        mood.Alignment = math.min(neutralAlignment, mood.Alignment + decayDelta);
                    }
                }

                // Clamp alignment to valid range
                mood.Alignment = math.clamp(mood.Alignment, 0f, 100f);
            }

            private float CalculateMiracleInfluence(float3 villagerPosition)
            {
                if (MiracleRegistryEntity == Entity.Null || !MiracleRegistryLookup.HasBuffer(MiracleRegistryEntity))
                {
                    return 0f;
                }

                var miracleRegistry = MiracleRegistryLookup[MiracleRegistryEntity];
                var totalInfluence = 0f;
                var influenceRangeSq = MiracleInfluenceRange * MiracleInfluenceRange;

                for (var i = 0; i < miracleRegistry.Length; i++)
                {
                    var entry = miracleRegistry[i];

                    // Only consider active miracles
                    if ((entry.Flags & MiracleRegistryFlags.Active) == 0)
                    {
                        continue;
                    }

                    // Check distance to miracle effect
                    var distSq = math.distancesq(villagerPosition, entry.TargetPosition);
                    if (distSq > influenceRangeSq)
                    {
                        continue;
                    }

                    // Calculate influence based on distance (closer = stronger)
                    var distanceRatio = 1f - math.sqrt(distSq) / MiracleInfluenceRange;
                    var miracleStrength = distanceRatio * MiracleAlignmentBonus;

                    // Modify based on miracle type (beneficial miracles have positive influence)
                    // Harmful miracles could have negative influence if desired
                    switch (entry.Type)
                    {
                        case MiracleType.Healing:
                        case MiracleType.Blessing:
                        case MiracleType.Fertility:
                            totalInfluence += miracleStrength;
                            break;
                        case MiracleType.Rain:
                        case MiracleType.Sunlight:
                            totalInfluence += miracleStrength * 0.5f; // Environmental miracles have less direct influence
                            break;
                        case MiracleType.Fire:
                        case MiracleType.Lightning:
                            // Destructive miracles can reduce alignment if villagers are nearby
                            totalInfluence -= miracleStrength * 0.25f;
                            break;
                        default:
                            totalInfluence += miracleStrength * 0.3f; // Unknown miracles have modest influence
                            break;
                    }
                }

                return totalInfluence;
            }
        }
    }
}

