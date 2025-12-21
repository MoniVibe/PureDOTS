using NUnit.Framework;
using PureDOTS.Runtime.WorldGen;
using PureDOTS.Runtime.WorldGen.Systems;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Tests.EditMode
{
    public class WorldGenSurfaceFieldsRequestSystemTests
    {
        private World _world;
        private EntityManager _entityManager;
        private BlobAssetReference<WorldRecipeBlob> _recipe;
        private BlobAssetReference<WorldGenDefinitionsBlob> _definitions;
        private SystemHandle _bootstrapHandle;
        private SystemHandle _generateHandle;
        private SystemHandle _disposeHandle;

        [SetUp]
        public void SetUp()
        {
            _world = new World("WorldGenSurfaceFieldsRequestSystemTests", WorldFlags.Game);
            _entityManager = _world.EntityManager;

            _bootstrapHandle = _world.GetOrCreateSystem<SurfaceFieldsChunkBootstrapSystem>();
            _generateHandle = _world.GetOrCreateSystem<SurfaceFieldsChunkGenerateSystem>();
            _disposeHandle = _world.GetOrCreateSystem<SurfaceFieldsChunkDisposeSystem>();

            _recipe = BuildRecipe();
            _definitions = BuildDefinitions();

            var recipeEntity = _entityManager.CreateEntity();
            _entityManager.AddComponentData(recipeEntity, new WorldRecipeComponent
            {
                RecipeHash = default,
                Recipe = _recipe
            });

            var defsEntity = _entityManager.CreateEntity();
            _entityManager.AddComponentData(defsEntity, new WorldGenDefinitionsComponent
            {
                DefinitionsHash = default,
                Definitions = _definitions
            });

            _bootstrapHandle.Update(_world.Unmanaged);

            var queueEntity = GetQueueEntity();
            _entityManager.SetComponentData(queueEntity, new SurfaceFieldsDomainConfig
            {
                CellsPerChunk = new int2(8, 8),
                CellSize = 1f,
                WorldOriginXZ = float2.zero,
                LatitudeOriginZ = 0f,
                LatitudeInvRange = 0f
            });
        }

        [TearDown]
        public void TearDown()
        {
            if (_definitions.IsCreated)
            {
                _definitions.Dispose();
            }

            if (_recipe.IsCreated)
            {
                _recipe.Dispose();
            }

            if (_world != null && _world.IsCreated)
            {
                _world.Dispose();
            }
        }

        [Test]
        public void SurfaceFields_RequestQueue_GeneratesChunkEntityAndClearsQueue()
        {
            var queueEntity = GetQueueEntity();
            var requests = _entityManager.GetBuffer<SurfaceFieldsChunkRequest>(queueEntity);
            requests.Add(new SurfaceFieldsChunkRequest { ChunkCoord = new int3(0, 0, 0) });

            _generateHandle.Update(_world.Unmanaged);

            Assert.That(requests.Length, Is.EqualTo(0));

            using var chunkQuery = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<SurfaceFieldsChunkComponent>());
            Assert.That(chunkQuery.CalculateEntityCount(), Is.EqualTo(1));

            var chunkEntity = chunkQuery.GetSingletonEntity();
            var chunk = _entityManager.GetComponentData<SurfaceFieldsChunkComponent>(chunkEntity);
            Assert.That(chunk.ChunkCoord, Is.EqualTo(new int3(0, 0, 0)));
            Assert.That(chunk.Chunk.IsCreated, Is.True);
            Assert.That(chunk.QuantizedHash, Is.EqualTo(chunk.Chunk.Value.QuantizedHash));

            _entityManager.DestroyEntity(chunkEntity);
            _disposeHandle.Update(_world.Unmanaged);

            using var cleanupQuery = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<SurfaceFieldsChunkCleanup>());
            Assert.That(cleanupQuery.CalculateEntityCount(), Is.EqualTo(0));
        }

        private Entity GetQueueEntity()
        {
            using var query = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<SurfaceFieldsChunkRequestQueue>());
            return query.GetSingletonEntity();
        }

        private static BlobAssetReference<WorldRecipeBlob> BuildRecipe()
        {
            using var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<WorldRecipeBlob>();
            root.SchemaVersion = WorldGenSchema.WorldRecipeSchemaVersion;
            root.WorldSeed = 12345u;
            root.DefinitionsHash = default;

            var stages = builder.Allocate(ref root.Stages, 1);
            stages[0].Kind = WorldGenStageKind.SurfaceFields;
            stages[0].SeedSalt = 999u;

            var parameters = builder.Allocate(ref stages[0].Parameters, 1);
            parameters[0] = new WorldGenParamBlob
            {
                Key = new FixedString64Bytes("sea_level"),
                Type = WorldGenParamType.Float,
                FloatValue = 0.25f
            };

            return builder.CreateBlobAssetReference<WorldRecipeBlob>(Allocator.Persistent);
        }

        private static BlobAssetReference<WorldGenDefinitionsBlob> BuildDefinitions()
        {
            using var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<WorldGenDefinitionsBlob>();
            root.SchemaVersion = WorldGenSchema.WorldGenDefinitionsSchemaVersion;

            var biomes = builder.Allocate(ref root.Biomes, 1);
            biomes[0] = new WorldGenBiomeDefinitionBlob
            {
                Id = new FixedString64Bytes("ocean"),
                Weight = 1f,
                TemperatureMin = 0f,
                TemperatureMax = 1f,
                MoistureMin = 0f,
                MoistureMax = 1f
            };

            builder.Allocate(ref root.Resources, 0);
            builder.Allocate(ref root.RuinSets, 0);

            return builder.CreateBlobAssetReference<WorldGenDefinitionsBlob>(Allocator.Persistent);
        }
    }
}

