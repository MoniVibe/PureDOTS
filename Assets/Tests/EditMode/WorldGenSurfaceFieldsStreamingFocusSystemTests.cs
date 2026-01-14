using NUnit.Framework;
using PureDOTS.Runtime.Streaming;
using PureDOTS.Runtime.WorldGen;
using PureDOTS.Runtime.WorldGen.Systems;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Tests.EditMode
{
    public class WorldGenSurfaceFieldsStreamingFocusSystemTests
    {
        private World _world;
        private EntityManager _entityManager;
        private BlobAssetReference<WorldRecipeBlob> _recipe;
        private BlobAssetReference<WorldGenDefinitionsBlob> _definitions;
        private SystemHandle _bootstrapHandle;
        private SystemHandle _requestHandle;
        private SystemHandle _generateHandle;
        private SystemHandle _evictHandle;
        private SystemHandle _disposeHandle;
        private Entity _focusEntity;

        [SetUp]
        public void SetUp()
        {
            _world = new World("WorldGenSurfaceFieldsStreamingFocusSystemTests", WorldFlags.Game);
            _entityManager = _world.EntityManager;

            _bootstrapHandle = _world.GetOrCreateSystem<SurfaceFieldsChunkBootstrapSystem>();
            _requestHandle = _world.GetOrCreateSystem<SurfaceFieldsChunkRequestFromStreamingFocusSystem>();
            _generateHandle = _world.GetOrCreateSystem<SurfaceFieldsChunkGenerateSystem>();
            _evictHandle = _world.GetOrCreateSystem<SurfaceFieldsChunkEvictSystem>();
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

            _entityManager.SetComponentData(queueEntity, new SurfaceFieldsStreamingConfig
            {
                LoadRadiusChunks = 1,
                KeepRadiusChunks = 2,
                MaxNewChunksPerTick = 0,
                EnableEviction = 0
            });

            _focusEntity = _entityManager.CreateEntity();
            _entityManager.AddComponentData(_focusEntity, new StreamingFocus
            {
                Position = float3.zero,
                Velocity = float3.zero,
                RadiusScale = 1f,
                LoadRadiusOffset = 0f,
                UnloadRadiusOffset = 0f
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
        public void SurfaceFields_StreamingFocus_RequestsSquareAndGeneratesChunks()
        {
            var queueEntity = GetQueueEntity();
            var requests = _entityManager.GetBuffer<SurfaceFieldsChunkRequest>(queueEntity);
            Assert.That(requests.Length, Is.EqualTo(0));

            _requestHandle.Update(_world.Unmanaged);

            Assert.That(requests.Length, Is.EqualTo(9));

            _generateHandle.Update(_world.Unmanaged);

            Assert.That(requests.Length, Is.EqualTo(0));

            using var chunkQuery = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<SurfaceFieldsChunkComponent>());
            Assert.That(chunkQuery.CalculateEntityCount(), Is.EqualTo(9));
        }

        [Test]
        public void SurfaceFields_StreamingFocus_RespectsMaxNewChunksPerTick()
        {
            var queueEntity = GetQueueEntity();
            _entityManager.SetComponentData(queueEntity, new SurfaceFieldsStreamingConfig
            {
                LoadRadiusChunks = 1,
                KeepRadiusChunks = 2,
                MaxNewChunksPerTick = 4,
                EnableEviction = 0
            });

            var requests = _entityManager.GetBuffer<SurfaceFieldsChunkRequest>(queueEntity);
            Assert.That(requests.Length, Is.EqualTo(0));

            _requestHandle.Update(_world.Unmanaged);
            _generateHandle.Update(_world.Unmanaged);

            using var chunkQuery = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<SurfaceFieldsChunkComponent>());
            Assert.That(chunkQuery.CalculateEntityCount(), Is.EqualTo(4));
            Assert.That(requests.Length, Is.EqualTo(5));
        }

        [Test]
        public void SurfaceFields_StreamingFocus_Eviction_RemovesDistantChunks()
        {
            var queueEntity = GetQueueEntity();
            _entityManager.SetComponentData(queueEntity, new SurfaceFieldsStreamingConfig
            {
                LoadRadiusChunks = 1,
                KeepRadiusChunks = 1,
                MaxNewChunksPerTick = 0,
                EnableEviction = 1
            });

            _requestHandle.Update(_world.Unmanaged);
            _generateHandle.Update(_world.Unmanaged);

            _entityManager.SetComponentData(_focusEntity, new StreamingFocus
            {
                Position = new float3(80f, 0f, 0f),
                Velocity = float3.zero,
                RadiusScale = 1f,
                LoadRadiusOffset = 0f,
                UnloadRadiusOffset = 0f
            });

            _requestHandle.Update(_world.Unmanaged);
            _generateHandle.Update(_world.Unmanaged);
            _evictHandle.Update(_world.Unmanaged);

            using var chunkQuery = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<SurfaceFieldsChunkComponent>());
            Assert.That(chunkQuery.CalculateEntityCount(), Is.EqualTo(9));

            _disposeHandle.Update(_world.Unmanaged);

            using var cleanupQuery = _entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<SurfaceFieldsChunkCleanup>(),
                ComponentType.Exclude<SurfaceFieldsChunkComponent>());
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
