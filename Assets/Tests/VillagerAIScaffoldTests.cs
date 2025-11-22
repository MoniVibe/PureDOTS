using NUnit.Framework;
using PureDOTS.Config;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Villager;
using PureDOTS.Runtime.Villagers;
using PureDOTS.Systems;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace PureDOTS.Tests
{
    /// <summary>
    /// Smoke coverage proving the villager AI scaffolding compiles and runs without managed allocations.
    /// </summary>
    public class VillagerAIScaffoldTests
    {
        private World _world;
        private EntityManager EntityManager => _world.EntityManager;
        private BlobAssetReference<VillagerArchetypeCatalogBlob> _catalog;

        [SetUp]
        public void SetUp()
        {
            _world = new World("VillagerAIScaffoldTests", WorldFlags.Game);
        }

        [TearDown]
        public void TearDownCatalog()
        {
            if (_catalog.IsCreated)
            {
                _catalog.Dispose();
            }

            if (_world != null && _world.IsCreated)
            {
                _world.Dispose();
            }
        }

        [Test]
        public void VillagerArchetypeCatalog_BuildsAndQueries()
        {
            _catalog = BuildCatalog(new FixedString64Bytes("scaffold-archetype"), 0.05f);

            Assert.IsTrue(_catalog.IsCreated);
            Assert.AreEqual(1, _catalog.Value.Archetypes.Length);

            var key = new FixedString64Bytes("scaffold-archetype");
            Assert.IsTrue(_catalog.Value.TryGetArchetype(key, out var archetype));
            Assert.AreEqual(0.05f, archetype.HungerDecayRate, 0.0001f);
        }

        [Test]
        public void VillagerAISystem_SelectsHungerGoal_WhenHungerExceedsThreshold()
        {
            CoreSingletonBootstrapSystem.EnsureSingletons(EntityManager);
            ConfigureTime();
            EnsureBehaviorConfig();

            _catalog = BuildCatalog(new FixedString64Bytes("scaffold-archetype"), 0.05f);

            var villager = EntityManager.CreateEntity(
                typeof(VillagerAIState),
                typeof(VillagerNeeds),
                typeof(VillagerJob),
                typeof(VillagerJobTicket),
                typeof(VillagerFlags));

            var needs = new VillagerNeeds { Health = 100f, MaxHealth = 100f };
            needs.SetHunger(95f);
            needs.SetEnergy(60f);
            needs.SetMorale(80f);
            EntityManager.SetComponentData(villager, needs);

            EntityManager.SetComponentData(villager, new VillagerJob
            {
                Type = VillagerJob.JobType.None,
                Phase = VillagerJob.JobPhase.Idle,
                Productivity = 1f,
                ActiveTicketId = 0,
                LastStateChangeTick = 0
            });

            var flags = new VillagerFlags();
            flags.IsDead = false;
            EntityManager.SetComponentData(villager, flags);

            var aiSystem = _world.GetOrCreateSystem<VillagerAISystem>();
            aiSystem.Update(_world.Unmanaged);

            var aiState = EntityManager.GetComponentData<VillagerAIState>(villager);
            Assert.AreEqual(VillagerAIState.Goal.SurviveHunger, aiState.CurrentGoal);
            Assert.AreEqual(VillagerAIState.State.Eating, aiState.CurrentState);
        }

        [Test]
        public void JobBehaviorStructs_AreUnmanaged()
        {
            Assert.IsTrue(UnsafeUtility.IsUnmanaged<GatherJobBehavior>());
            Assert.IsTrue(UnsafeUtility.IsUnmanaged<BuildJobBehavior>());
            Assert.IsTrue(UnsafeUtility.IsUnmanaged<CraftJobBehavior>());
            Assert.IsTrue(UnsafeUtility.IsUnmanaged<CombatJobBehavior>());
        }

        private static BlobAssetReference<VillagerArchetypeCatalogBlob> BuildCatalog(in FixedString64Bytes name, float hungerDecayRate)
        {
            using var builder = new BlobBuilder(Allocator.Temp);
            ref var catalog = ref builder.ConstructRoot<VillagerArchetypeCatalogBlob>();
            var archetypes = builder.Allocate(ref catalog.Archetypes, 1);

            archetypes[0] = new VillagerArchetypeData
            {
                ArchetypeName = name,
                BasePhysique = 50,
                BaseFinesse = 50,
                BaseWillpower = 50,
                HungerDecayRate = hungerDecayRate,
                EnergyDecayRate = 0.02f,
                MoraleDecayRate = 0.01f,
                GatherJobWeight = 50,
                BuildJobWeight = 40,
                CraftJobWeight = 30,
                CombatJobWeight = 20,
                TradeJobWeight = 10,
                MoralAxisLean = 0,
                OrderAxisLean = 0,
                PurityAxisLean = 0,
                BaseLoyalty = 50
            };

            return builder.CreateBlobAssetReference<VillagerArchetypeCatalogBlob>(Allocator.Persistent);
        }

        private void ConfigureTime()
        {
            using var timeQuery = EntityManager.CreateEntityQuery(ComponentType.ReadWrite<TimeState>());
            var timeEntity = timeQuery.GetSingletonEntity();
            var time = EntityManager.GetComponentData<TimeState>(timeEntity);
            time.IsPaused = false;
            time.FixedDeltaTime = 0.2f;
            time.Tick = 1;
            EntityManager.SetComponentData(timeEntity, time);

            using var rewindQuery = EntityManager.CreateEntityQuery(ComponentType.ReadWrite<RewindState>());
            var rewindEntity = rewindQuery.GetSingletonEntity();
            var rewind = EntityManager.GetComponentData<RewindState>(rewindEntity);
            rewind.Mode = RewindMode.Record;
            EntityManager.SetComponentData(rewindEntity, rewind);
        }

        private void EnsureBehaviorConfig()
        {
            using var query = EntityManager.CreateEntityQuery(ComponentType.ReadWrite<VillagerBehaviorConfig>());
            if (query.IsEmptyIgnoreFilter)
            {
                var entity = EntityManager.CreateEntity(typeof(VillagerBehaviorConfig));
                EntityManager.SetComponentData(entity, VillagerBehaviorConfig.CreateDefaults());
            }
        }
    }
}

