using NUnit.Framework;
using PureDOTS.Runtime.WorldGen;
using PureDOTS.Runtime.WorldGen.Domain;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Tests.EditMode
{
    public class WorldGenSurfaceFieldsTests
    {
        [Test]
        public void SurfaceFields_Determinism_SameInputsSameHash()
        {
            var recipe = BuildRecipe(seaLevel: 0.25f);
            var definitions = BuildDefinitions();

            var domain = new PlanarXZDomainProvider
            {
                CellsPerChunk = new int2(16, 16),
                CellSize = 1f,
                WorldOriginXZ = float2.zero,
                LatitudeOriginZ = 0f,
                LatitudeInvRange = 1f / 512f
            };

            var chunkCoord = new int3(0, 0, 0);
            ref var recipeBlob = ref recipe.Value;
            ref var stage = ref recipeBlob.Stages[0];

            var a = SurfaceFieldsGenerator.GenerateChunk(
                in recipeBlob,
                in stage,
                0,
                in definitions.Value,
                in domain,
                chunkCoord,
                Allocator.Persistent);

            var b = SurfaceFieldsGenerator.GenerateChunk(
                in recipeBlob,
                in stage,
                0,
                in definitions.Value,
                in domain,
                chunkCoord,
                Allocator.Persistent);

            Assert.That(a.Value.QuantizedHash, Is.EqualTo(b.Value.QuantizedHash));
            Assert.That(a.Value.Summary.WaterCellCount, Is.EqualTo(b.Value.Summary.WaterCellCount));

            a.Dispose();
            b.Dispose();
            recipe.Dispose();
            definitions.Dispose();
        }

        [Test]
        public void SurfaceFields_Seams_SharedBorderVerticesMatch()
        {
            var recipe = BuildRecipe(seaLevel: 0.25f);
            var definitions = BuildDefinitions();

            var domain = new PlanarXZDomainProvider
            {
                CellsPerChunk = new int2(16, 16),
                CellSize = 1f,
                WorldOriginXZ = float2.zero,
                LatitudeOriginZ = 0f,
                LatitudeInvRange = 1f / 512f
            };

            ref var recipeBlob = ref recipe.Value;
            ref var stage = ref recipeBlob.Stages[0];

            var left = SurfaceFieldsGenerator.GenerateChunk(
                in recipeBlob,
                in stage,
                0,
                in definitions.Value,
                in domain,
                new int3(0, 0, 0),
                Allocator.Persistent);

            var right = SurfaceFieldsGenerator.GenerateChunk(
                in recipeBlob,
                in stage,
                0,
                in definitions.Value,
                in domain,
                new int3(1, 0, 0),
                Allocator.Persistent);

            ref var leftChunk = ref left.Value;
            ref var rightChunk = ref right.Value;

            var cells = leftChunk.CellsPerChunk;
            var stride = cells.x + 1;

            for (int z = 0; z <= cells.y; z++)
            {
                var aIndex = cells.x + z * stride; // right edge of left
                var bIndex = 0 + z * stride;       // left edge of right

                Assert.That(leftChunk.HeightQ[aIndex], Is.EqualTo(rightChunk.HeightQ[bIndex]));
                Assert.That(leftChunk.TempQ[aIndex], Is.EqualTo(rightChunk.TempQ[bIndex]));
                Assert.That(leftChunk.MoistureQ[aIndex], Is.EqualTo(rightChunk.MoistureQ[bIndex]));
            }

            left.Dispose();
            right.Dispose();
            recipe.Dispose();
            definitions.Dispose();
        }

        [Test]
        public void SurfaceFields_Constraints_OceanMaskIncreasesWater()
        {
            var recipe = BuildRecipe(seaLevel: 0.2f, constraintOceanStrength: 0.8f);
            var definitions = BuildDefinitions();

            var domain = new PlanarXZDomainProvider
            {
                CellsPerChunk = new int2(16, 16),
                CellSize = 1f,
                WorldOriginXZ = float2.zero,
                LatitudeOriginZ = 0f,
                LatitudeInvRange = 1f / 512f
            };

            ref var recipeBlob = ref recipe.Value;
            ref var stage = ref recipeBlob.Stages[0];

            var baseline = SurfaceFieldsGenerator.GenerateChunk(
                in recipeBlob,
                in stage,
                0,
                in definitions.Value,
                in domain,
                new int3(0, 0, 0),
                Allocator.Persistent);

            var constraints = BuildOceanConstraintMap(worldMinXZ: float2.zero, worldMaxXZ: new float2(16f, 16f), resolution: new int2(4, 4));
            var constrained = SurfaceFieldsGenerator.GenerateChunk(
                in recipeBlob,
                in stage,
                0,
                in definitions.Value,
                in domain,
                new SurfaceConstraintMapSampler(constraints),
                new int3(0, 0, 0),
                Allocator.Persistent);

            Assert.That(constrained.Value.Summary.WaterCellCount, Is.GreaterThan(baseline.Value.Summary.WaterCellCount));

            baseline.Dispose();
            constrained.Dispose();
            constraints.Dispose();
            recipe.Dispose();
            definitions.Dispose();
        }

        [Test]
        public void WorldGenRng_StageChunkRandom_IsStablePerChunk()
        {
            var recipe = BuildRecipe(seaLevel: 0.25f);
            ref var recipeBlob = ref recipe.Value;
            ref var stage = ref recipeBlob.Stages[0];

            var a1 = WorldGenRng.CreateStageChunkRandom(in recipeBlob, in stage, 0, new int3(3, 0, 7), stream: 0);
            var a2 = WorldGenRng.CreateStageChunkRandom(in recipeBlob, in stage, 0, new int3(3, 0, 7), stream: 0);
            var b = WorldGenRng.CreateStageChunkRandom(in recipeBlob, in stage, 0, new int3(4, 0, 7), stream: 0);

            var a1First = a1.NextUInt();
            var a2First = a2.NextUInt();
            var bFirst = b.NextUInt();

            Assert.That(a1First, Is.EqualTo(a2First));
            Assert.That(a1First, Is.Not.EqualTo(bFirst));

            recipe.Dispose();
        }

        private static BlobAssetReference<WorldRecipeBlob> BuildRecipe(float seaLevel, float constraintOceanStrength = 0f)
        {
            using var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<WorldRecipeBlob>();
            root.SchemaVersion = WorldGenSchema.WorldRecipeSchemaVersion;
            root.WorldSeed = 12345u;
            root.DefinitionsHash = default;

            var stages = builder.Allocate(ref root.Stages, 1);
            stages[0].Kind = WorldGenStageKind.SurfaceFields;
            stages[0].SeedSalt = 999u;

            var paramCount = constraintOceanStrength > 0f ? 2 : 1;
            var parameters = builder.Allocate(ref stages[0].Parameters, paramCount);

            parameters[0] = new WorldGenParamBlob
            {
                Key = new FixedString64Bytes("sea_level"),
                Type = WorldGenParamType.Float,
                FloatValue = seaLevel
            };

            if (constraintOceanStrength > 0f)
            {
                parameters[1] = new WorldGenParamBlob
                {
                    Key = new FixedString64Bytes("constraint_ocean_strength"),
                    Type = WorldGenParamType.Float,
                    FloatValue = constraintOceanStrength
                };
            }

            return builder.CreateBlobAssetReference<WorldRecipeBlob>(Allocator.Persistent);
        }

        private static BlobAssetReference<WorldGenDefinitionsBlob> BuildDefinitions()
        {
            using var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<WorldGenDefinitionsBlob>();
            root.SchemaVersion = WorldGenSchema.WorldGenDefinitionsSchemaVersion;

            var biomes = builder.Allocate(ref root.Biomes, 4);
            biomes[0] = new WorldGenBiomeDefinitionBlob
            {
                Id = new FixedString64Bytes("ocean"),
                Weight = 1f,
                TemperatureMin = 0f,
                TemperatureMax = 1f,
                MoistureMin = 0f,
                MoistureMax = 1f
            };
            biomes[1] = new WorldGenBiomeDefinitionBlob
            {
                Id = new FixedString64Bytes("tundra"),
                Weight = 1f,
                TemperatureMin = 0f,
                TemperatureMax = 0.3f,
                MoistureMin = 0f,
                MoistureMax = 1f
            };
            biomes[2] = new WorldGenBiomeDefinitionBlob
            {
                Id = new FixedString64Bytes("desert"),
                Weight = 1f,
                TemperatureMin = 0.5f,
                TemperatureMax = 1f,
                MoistureMin = 0f,
                MoistureMax = 0.3f
            };
            biomes[3] = new WorldGenBiomeDefinitionBlob
            {
                Id = new FixedString64Bytes("forest"),
                Weight = 1f,
                TemperatureMin = 0.3f,
                TemperatureMax = 0.8f,
                MoistureMin = 0.3f,
                MoistureMax = 1f
            };

            builder.Allocate(ref root.Resources, 0);
            builder.Allocate(ref root.RuinSets, 0);

            return builder.CreateBlobAssetReference<WorldGenDefinitionsBlob>(Allocator.Persistent);
        }

        private static BlobAssetReference<SurfaceConstraintMapBlob> BuildOceanConstraintMap(float2 worldMinXZ, float2 worldMaxXZ, int2 resolution)
        {
            using var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<SurfaceConstraintMapBlob>();
            root.SchemaVersion = 1;
            root.WorldMinXZ = worldMinXZ;
            root.WorldMaxXZ = worldMaxXZ;
            root.Resolution = resolution;

            builder.Allocate(ref root.HeightBiasQ, 0);
            builder.Allocate(ref root.RidgeMaskQ, 0);

            var ocean = builder.Allocate(ref root.OceanMaskQ, resolution.x * resolution.y);
            for (int i = 0; i < ocean.Length; i++)
            {
                ocean[i] = 255;
            }

            return builder.CreateBlobAssetReference<SurfaceConstraintMapBlob>(Allocator.Persistent);
        }
    }
}

