using System;
using Unity.Collections;
using Unity.Mathematics;

namespace PureDOTS.Runtime.WorldGen
{
    [Flags]
    public enum GalaxySystemTraitMask : uint
    {
        None = 0u,
        All = 0xFFFFFFFFu
    }

    [Flags]
    public enum GalaxyPoiMask : uint
    {
        None = 0u,
        All = 0xFFFFFFFFu
    }

    public enum GalaxySystemTraitKind : byte
    {
        None = 0,
        Binary = 1,
        Nebula = 2,
        Neutron = 3,
        BlackHole = 4,
        AncientCore = 5
    }

    public enum GalaxyPoiKind : byte
    {
        None = 0,
        AncientRuins = 1,
        AlienShrine = 2,
        GateFragment = 3,
        SuperResource = 4,
        Scenic = 5,
        HazardZone = 6
    }

    public struct GalaxySystemTraitDefinition
    {
        public GalaxySystemTraitKind Kind;
        public float Weight;
        public byte MinRing;
        public byte MaxRing;
    }

    public struct GalaxyPoiDefinition
    {
        public GalaxyPoiKind Kind;
        public float Weight;
        public byte MinRing;
        public byte MaxRing;
    }

    public struct GalaxyGenerationCatalog
    {
        public NativeArray<GalaxySystemTraitDefinition> SystemTraits;
        public NativeArray<GalaxyPoiDefinition> Pois;
    }

    public struct GalaxyGenerationConfig
    {
        public uint Seed;
        public ushort FactionCount;
        public ushort SystemsPerFaction;
        public ushort ResourcesPerSystem;
        public float StartRadius;
        public float SystemSpacing;
        public float ResourceBaseUnits;
        public float ResourceRichnessGradient;
        public ushort ResourceKindCount;
        public GalaxySystemTraitMask TraitMask;
        public GalaxyPoiMask PoiMask;
        public byte MaxTraitsPerSystem;
        public byte MaxPoisPerSystem;
        public float TraitChanceBase;
        public float TraitChancePerRing;
        public float TraitChanceMax;
        public float PoiChanceBase;
        public float PoiChancePerRing;
        public float PoiChanceMax;
        public float PoiOffsetMin;
        public float PoiOffsetMax;

        public static GalaxyGenerationConfig FromBase(
            uint seed,
            ushort factionCount,
            ushort systemsPerFaction,
            ushort resourcesPerSystem,
            float startRadius,
            float systemSpacing,
            float resourceBaseUnits,
            float resourceRichnessGradient,
            ushort resourceKindCount)
        {
            return new GalaxyGenerationConfig
            {
                Seed = seed,
                FactionCount = factionCount,
                SystemsPerFaction = systemsPerFaction,
                ResourcesPerSystem = resourcesPerSystem,
                StartRadius = startRadius,
                SystemSpacing = systemSpacing,
                ResourceBaseUnits = resourceBaseUnits,
                ResourceRichnessGradient = resourceRichnessGradient,
                ResourceKindCount = resourceKindCount,
                TraitMask = GalaxySystemTraitMask.All,
                PoiMask = GalaxyPoiMask.All,
                MaxTraitsPerSystem = 0,
                MaxPoisPerSystem = 0,
                TraitChanceBase = 0f,
                TraitChancePerRing = 0f,
                TraitChanceMax = 0f,
                PoiChanceBase = 0f,
                PoiChancePerRing = 0f,
                PoiChanceMax = 0f,
                PoiOffsetMin = 0f,
                PoiOffsetMax = 0f
            };
        }
    }

    public struct GalaxyFactionSeed
    {
        public ushort FactionId;
    }

    public struct GalaxySystemSeed
    {
        public ushort SystemId;
        public ushort HomeFactionId;
        public byte RingIndex;
        public float3 Position;
    }

    public struct GalaxyResourceSeed
    {
        public ushort SystemId;
        public ushort KindIndex;
        public byte RingIndex;
        public byte LocalIndex;
        public ushort RandomSuffix;
        public float3 Position;
        public float Units;
    }

    public struct GalaxyAnomalySeed
    {
        public float3 Position;
        public byte RingIndex;
    }

    public struct GalaxySystemTraitSeed
    {
        public ushort SystemId;
        public GalaxySystemTraitKind Kind;
        public float Intensity;
        public byte RingIndex;
    }

    public struct GalaxyPoiSeed
    {
        public ushort SystemId;
        public byte RingIndex;
        public GalaxyPoiKind Kind;
        public float Reward;
        public float Risk;
        public float3 Position;
    }

    public static class GalaxyGeneration
    {
        public static void Generate(
            in GalaxyGenerationConfig config,
            in GalaxyGenerationCatalog catalog,
            ref NativeList<GalaxyFactionSeed> factionSeeds,
            ref NativeList<GalaxySystemSeed> systemSeeds,
            ref NativeList<GalaxyResourceSeed> resourceSeeds,
            ref NativeList<GalaxyAnomalySeed> anomalySeeds,
            ref NativeList<GalaxySystemTraitSeed> traitSeeds,
            ref NativeList<GalaxyPoiSeed> poiSeeds)
        {
            factionSeeds.Clear();
            systemSeeds.Clear();
            resourceSeeds.Clear();
            anomalySeeds.Clear();
            traitSeeds.Clear();
            poiSeeds.Clear();

            var rng = new Unity.Mathematics.Random(math.max(1u, config.Seed));
            ushort systemId = 1;

            for (int f = 0; f < config.FactionCount; f++)
            {
                var factionId = (ushort)(f + 1);
                factionSeeds.Add(new GalaxyFactionSeed { FactionId = factionId });

                for (int s = 0; s < config.SystemsPerFaction; s++)
                {
                    var ringIndex = (byte)math.min(255, s);
                    float angle = rng.NextFloat(0f, math.PI * 2f);
                    float radius = config.StartRadius + ringIndex * config.SystemSpacing;
                    var position = new float3(math.cos(angle), 0f, math.sin(angle)) * radius;

                    systemSeeds.Add(new GalaxySystemSeed
                    {
                        SystemId = systemId,
                        HomeFactionId = factionId,
                        RingIndex = ringIndex,
                        Position = position
                    });

                    for (int r = 0; r < config.ResourcesPerSystem; r++)
                    {
                        var offset = new float3(rng.NextFloat(-120f, 120f), 0f, rng.NextFloat(-120f, 120f));
                        var units = math.max(1f, config.ResourceBaseUnits * rng.NextFloat(0.5f, 1.5f));
                        resourceSeeds.Add(new GalaxyResourceSeed
                        {
                            SystemId = systemId,
                            KindIndex = (ushort)rng.NextInt(0, math.max(1, config.ResourceKindCount)),
                            RingIndex = ringIndex,
                            LocalIndex = (byte)r,
                            RandomSuffix = (ushort)rng.NextInt(0, 10000),
                            Position = position + offset,
                            Units = units
                        });
                    }

                    systemId++;
                }
            }
        }
    }
}
