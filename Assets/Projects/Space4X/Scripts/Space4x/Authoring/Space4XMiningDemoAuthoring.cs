using System;
using PureDOTS.Authoring;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Knowledge;
using PureDOTS.Runtime.Resource;
using PureDOTS.Runtime.Skills;
using PureDOTS.Runtime.Spatial;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Space4X.Registry
{
    /// <summary>
    /// Authoring component for creating carriers, mining vessels, and asteroids in the mining demo scene.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PureDotsConfigAuthoring))]
    [RequireComponent(typeof(SpatialPartitionAuthoring))]
    public sealed class Space4XMiningDemoAuthoring : MonoBehaviour
    {
        [SerializeField]
        private CarrierDefinition[] carriers = new CarrierDefinition[]
        {
            new CarrierDefinition
            {
                CarrierId = "CARRIER-1",
                Speed = 5f,
                PatrolCenter = new float3(0f, 0f, 0f),
                PatrolRadius = 50f,
                WaitTime = 2f,
                Position = new float3(0f, 0f, 0f)
            }
        };

        [SerializeField]
        private MiningVesselDefinition[] miningVessels = new MiningVesselDefinition[]
        {
            new MiningVesselDefinition
            {
                VesselId = "MINER-1",
                Speed = 10f,
                MiningEfficiency = 0.8f,
                CargoCapacity = 100f,
                Position = new float3(5f, 0f, 0f),
                CarrierId = "CARRIER-1"
            },
            new MiningVesselDefinition
            {
                VesselId = "MINER-2",
                Speed = 10f,
                MiningEfficiency = 0.8f,
                CargoCapacity = 100f,
                Position = new float3(-5f, 0f, 0f),
                CarrierId = "CARRIER-1"
            }
        };

        [SerializeField]
        private AsteroidDefinition[] asteroids = new AsteroidDefinition[]
        {
            new AsteroidDefinition
            {
                AsteroidId = "ASTEROID-1",
                ResourceType = ResourceType.Minerals,
                ResourceAmount = 500f,
                MaxResourceAmount = 500f,
                MiningRate = 10f,
                Position = new float3(20f, 0f, 0f)
            },
            new AsteroidDefinition
            {
                AsteroidId = "ASTEROID-2",
                ResourceType = ResourceType.Minerals,
                ResourceAmount = 500f,
                MaxResourceAmount = 500f,
                MiningRate = 10f,
                Position = new float3(-20f, 0f, 0f)
            }
        };

        [SerializeField]
        private MiningVisualSettings visuals = MiningVisualSettings.CreateDefault();

        public CarrierDefinition[] Carriers => carriers;
        public MiningVesselDefinition[] MiningVessels => miningVessels;
        public AsteroidDefinition[] Asteroids => asteroids;
        public MiningVisualSettings Visuals => visuals;

        [Serializable]
        public struct CarrierDefinition
        {
            [Tooltip("Unique identifier for this carrier")]
            public string CarrierId;
            
            [Tooltip("Movement speed of the carrier")]
            [Min(0.1f)]
            public float Speed;
            
            [Tooltip("Center point of the patrol area")]
            public float3 PatrolCenter;
            
            [Tooltip("Radius of the patrol area")]
            [Min(1f)]
            public float PatrolRadius;
            
            [Tooltip("How long to wait at each waypoint (seconds)")]
            [Min(0f)]
            public float WaitTime;
            
            [Tooltip("Starting position of the carrier")]
            public float3 Position;

            [Header("Strike Group Skills & Knowledge")]
            [Range(0f, 200f)] public float strikeGroupSkillLevel;
            public bool strikeGroupKnowsLegendary;
            public bool strikeGroupKnowsRelic;
            public string[] lessonIds;
        }

        [Serializable]
        public struct MiningVesselDefinition
        {
            [Tooltip("Unique identifier for this mining vessel")]
            public string VesselId;
            
            [Tooltip("Movement speed of the vessel")]
            [Min(0.1f)]
            public float Speed;
            
            [Tooltip("Mining efficiency multiplier (0-1)")]
            [Range(0f, 1f)]
            public float MiningEfficiency;
            
            [Tooltip("Maximum cargo capacity")]
            [Min(1f)]
            public float CargoCapacity;
            
            [Tooltip("Starting position of the vessel")]
            public float3 Position;
            
            [Tooltip("Carrier ID that this vessel belongs to (must match a CarrierId)")]
            public string CarrierId;

            [Header("Pilot Skills & Knowledge")]
            [Range(0f, 200f)] public float pilotSkillLevel;
            public bool pilotKnowsLegendary;
            public bool pilotKnowsRelic;
            public string[] lessonIds;
        }

        [Serializable]
        public struct AsteroidDefinition
        {
            [Tooltip("Unique identifier for this asteroid")]
            public string AsteroidId;
            
            [Tooltip("Type of resource in this asteroid")]
            public ResourceType ResourceType;
            
            [Tooltip("Current resource amount")]
            [Min(0f)]
            public float ResourceAmount;
            
            [Tooltip("Maximum resource amount (used for regeneration if needed)")]
            [Min(0f)]
            public float MaxResourceAmount;
            
            [Tooltip("Rate at which resources can be mined per second")]
            [Min(0.1f)]
            public float MiningRate;
            
            [Tooltip("Position of the asteroid")]
            public float3 Position;

            [Header("Quality")]
            public ResourceQualityTier qualityTier;
            [Range(1, 600)] public int baseQuality;
            [Range(0, 200)] public int qualityVariance;
        }

        [Serializable]
        public struct MiningVisualSettings
        {
            public Space4XMiningPrimitive CarrierPrimitive;
            [Min(0.05f)] public float CarrierScale;
            public Color CarrierColor;

            public Space4XMiningPrimitive MiningVesselPrimitive;
            [Min(0.05f)] public float MiningVesselScale;
            public Color MiningVesselColor;

            public Space4XMiningPrimitive AsteroidPrimitive;
            [Min(0.05f)] public float AsteroidScale;
            public Color AsteroidColor;

            public static MiningVisualSettings CreateDefault()
            {
                return new MiningVisualSettings
                {
                    CarrierPrimitive = Space4XMiningPrimitive.Capsule,
                    CarrierScale = 3f,
                    CarrierColor = new Color(0.35f, 0.4f, 0.62f, 1f),
                    MiningVesselPrimitive = Space4XMiningPrimitive.Cylinder,
                    MiningVesselScale = 1.2f,
                    MiningVesselColor = new Color(0.25f, 0.52f, 0.84f, 1f),
                    AsteroidPrimitive = Space4XMiningPrimitive.Sphere,
                    AsteroidScale = 2.25f,
                    AsteroidColor = new Color(0.52f, 0.43f, 0.34f, 1f)
                };
            }
        }

        private sealed class Baker : Unity.Entities.Baker<Space4XMiningDemoAuthoring>
        {
            private NativeHashMap<FixedString64Bytes, Entity> _carrierEntityMap;

            public override void Bake(Space4XMiningDemoAuthoring authoring)
            {
                AddVisualConfig(authoring);

                // Build carrier entity map first
                _carrierEntityMap = new NativeHashMap<FixedString64Bytes, Entity>(
                    authoring.Carriers?.Length ?? 0, 
                    Allocator.Temp);

                // Bake carriers first and store their entities
                BakeCarriers(authoring);
                
                // Then bake vessels (which reference carriers)
                BakeMiningVessels(authoring);
                
                // Finally bake asteroids
                BakeAsteroids(authoring);

                _carrierEntityMap.Dispose();
            }

            private void AddVisualConfig(Space4XMiningDemoAuthoring authoring)
            {
                var configEntity = GetEntity(TransformUsageFlags.None);
                var visuals = authoring.visuals;

                var config = new Space4XMiningVisualConfig
                {
                    CarrierPrimitive = visuals.CarrierPrimitive,
                    MiningVesselPrimitive = visuals.MiningVesselPrimitive,
                    AsteroidPrimitive = visuals.AsteroidPrimitive,
                    CarrierScale = math.max(0.05f, visuals.CarrierScale),
                    MiningVesselScale = math.max(0.05f, visuals.MiningVesselScale),
                    AsteroidScale = math.max(0.05f, visuals.AsteroidScale),
                    CarrierColor = ToFloat4(visuals.CarrierColor),
                    MiningVesselColor = ToFloat4(visuals.MiningVesselColor),
                    AsteroidColor = ToFloat4(visuals.AsteroidColor)
                };

                SetComponent(configEntity, config);
            }

            private void BakeCarriers(Space4XMiningDemoAuthoring authoring)
            {
                if (authoring.Carriers == null || authoring.Carriers.Length == 0)
                {
                    return;
                }

                foreach (var carrier in authoring.Carriers)
                {
                    if (string.IsNullOrWhiteSpace(carrier.CarrierId))
                    {
                        Debug.LogWarning($"Carrier definition has empty CarrierId, skipping.");
                        continue;
                    }

                    var entity = CreateAdditionalEntity(TransformUsageFlags.Dynamic);
                    AddComponent(entity, LocalTransform.FromPositionRotationScale(carrier.Position, quaternion.identity, 1f));
                    AddComponent<SpatialIndexedTag>(entity);
                    
                    var carrierIdBytes = new FixedString64Bytes(carrier.CarrierId);
                    
                    AddComponent(entity, new Carrier
                    {
                        CarrierId = carrierIdBytes,
                        AffiliationEntity = Entity.Null, // Can be set later if affiliations are used
                        Speed = math.max(0.1f, carrier.Speed),
                        PatrolCenter = carrier.PatrolCenter,
                        PatrolRadius = math.max(1f, carrier.PatrolRadius)
                    });

                    AddComponent(entity, new PatrolBehavior
                    {
                        CurrentWaypoint = float3.zero, // Will be initialized by CarrierPatrolSystem
                        WaitTime = math.max(0f, carrier.WaitTime),
                        WaitTimer = 0f
                    });

                    AddComponent(entity, new MovementCommand
                    {
                        TargetPosition = float3.zero,
                        ArrivalThreshold = 1f
                    });

                    var skillSet = new SkillSet();
                    skillSet.SetLevel(SkillId.Mining, (byte)math.clamp(carrier.strikeGroupSkillLevel, 0f, 255f));
                    AddComponent(entity, skillSet);

                    var knowledge = CreateKnowledge(
                        carrier.strikeGroupKnowsLegendary,
                        carrier.strikeGroupKnowsRelic,
                        carrier.lessonIds);
                    AddComponent(entity, knowledge);

                    // Add ResourceStorage buffer for the carrier
                    var resourceBuffer = AddBuffer<ResourceStorage>(entity);
                    // Buffer starts empty, will be populated by mining vessels

                    // Store entity in map for vessel references
                    _carrierEntityMap.TryAdd(carrierIdBytes, entity);
                }
            }

            private void BakeMiningVessels(Space4XMiningDemoAuthoring authoring)
            {
                if (authoring.MiningVessels == null || authoring.MiningVessels.Length == 0)
                {
                    return;
                }

                foreach (var vessel in authoring.MiningVessels)
                {
                    if (string.IsNullOrWhiteSpace(vessel.VesselId))
                    {
                        Debug.LogWarning($"Mining vessel definition has empty VesselId, skipping.");
                        continue;
                    }

                    var entity = CreateAdditionalEntity(TransformUsageFlags.Dynamic);
                    AddComponent(entity, LocalTransform.FromPositionRotationScale(vessel.Position, quaternion.identity, 1f));
                    AddComponent<SpatialIndexedTag>(entity);

                    // Find the carrier entity
                    Entity carrierEntity = Entity.Null;
                    if (!string.IsNullOrWhiteSpace(vessel.CarrierId))
                    {
                        var carrierIdBytes = new FixedString64Bytes(vessel.CarrierId);
                        if (_carrierEntityMap.TryGetValue(carrierIdBytes, out var foundCarrier))
                        {
                            carrierEntity = foundCarrier;
                        }
                        else
                        {
                            Debug.LogWarning($"Mining vessel '{vessel.VesselId}' references carrier '{vessel.CarrierId}' which doesn't exist. Vessel will not function.");
                        }
                    }

                    AddComponent(entity, new MiningVessel
                    {
                        VesselId = new FixedString64Bytes(vessel.VesselId),
                        CarrierEntity = carrierEntity,
                        MiningEfficiency = math.clamp(vessel.MiningEfficiency, 0f, 1f),
                        Speed = math.max(0.1f, vessel.Speed),
                        CargoCapacity = math.max(1f, vessel.CargoCapacity),
                        CurrentCargo = 0f,
                        CargoTier = (byte)ResourceQualityTier.Unknown,
                        AverageCargoQuality = 0
                    });

                    AddComponent(entity, new MiningJob
                    {
                        State = MiningJobState.None,
                        TargetAsteroid = Entity.Null,
                        MiningProgress = 0f
                    });

                    var vesselSkillSet = new SkillSet();
                    vesselSkillSet.SetLevel(SkillId.Mining, (byte)math.clamp(vessel.pilotSkillLevel, 0f, 255f));
                    AddComponent(entity, vesselSkillSet);

                    var vesselKnowledge = CreateKnowledge(
                        vessel.pilotKnowsLegendary,
                        vessel.pilotKnowsRelic,
                        vessel.lessonIds);
                    AddComponent(entity, vesselKnowledge);
                }
            }

            private void BakeAsteroids(Space4XMiningDemoAuthoring authoring)
            {
                if (authoring.Asteroids == null || authoring.Asteroids.Length == 0)
                {
                    return;
                }

                foreach (var asteroid in authoring.Asteroids)
                {
                    if (string.IsNullOrWhiteSpace(asteroid.AsteroidId))
                    {
                        Debug.LogWarning($"Asteroid definition has empty AsteroidId, skipping.");
                        continue;
                    }

                    var entity = CreateAdditionalEntity(TransformUsageFlags.Dynamic);
                    AddComponent(entity, LocalTransform.FromPositionRotationScale(asteroid.Position, quaternion.identity, 1f));
                    AddComponent<SpatialIndexedTag>(entity);

                    AddComponent(entity, new Asteroid
                    {
                        AsteroidId = new FixedString64Bytes(asteroid.AsteroidId),
                        ResourceType = asteroid.ResourceType,
                        ResourceAmount = math.max(0f, asteroid.ResourceAmount),
                        MaxResourceAmount = math.max(asteroid.ResourceAmount, asteroid.MaxResourceAmount),
                        MiningRate = math.max(0.1f, asteroid.MiningRate),
                        QualityTier = asteroid.qualityTier,
                        BaseQuality = (ushort)math.clamp(asteroid.baseQuality, 1, 600),
                        QualityVariance = (ushort)math.clamp(asteroid.qualityVariance, 0, 200)
                    });
                }
            }

            private static float4 ToFloat4(Color color)
            {
                return new float4(color.r, color.g, color.b, color.a);
            }

            private static VillagerKnowledge CreateKnowledge(bool knowsLegendary, bool knowsRelic, string[] lessonIds)
            {
                var knowledge = new VillagerKnowledge
                {
                    Flags = 0
                };

                if (knowsLegendary)
                {
                    knowledge.Flags |= VillagerKnowledgeFlags.HarvestLegendary;
                    knowledge.TryAddLesson(ToLessonId("lesson.harvest.legendary"));
                }

                if (knowsRelic)
                {
                    knowledge.Flags |= VillagerKnowledgeFlags.HarvestRelic;
                    knowledge.TryAddLesson(ToLessonId("lesson.harvest.relic"));
                }

                if (lessonIds != null)
                {
                    for (int i = 0; i < lessonIds.Length; i++)
                    {
                        knowledge.TryAddLesson(ToLessonId(lessonIds[i]));
                    }
                }

                return knowledge;
            }

            private static FixedString64Bytes ToLessonId(string value)
            {
                FixedString64Bytes str = default;
                if (!string.IsNullOrWhiteSpace(value))
                {
                    str = new FixedString64Bytes(value.Trim());
                }

                return str;
            }
        }
    }
}

