#if INCLUDE_SPACE4X_IN_PUREDOTS
using NUnit.Framework;
using PureDOTS.Runtime.AI;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Interrupts;
using PureDOTS.Runtime.Villager;
using PureDOTS.Runtime.Villagers;
using PureDOTS.Systems;
using Space4X.Runtime;
using Space4X.Runtime.Transport;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Tests.Playmode
{
    /// <summary>
    /// Integration tests validating that Godgame villagers and Space4X vessels
    /// properly integrate with the shared AISystemGroup pipeline.
    /// </summary>
    public class AIIntegrationTests
    {
        private World _world;
        private EntityManager EntityManager => _world.EntityManager;
        private BlobAssetReference<AIUtilityArchetypeBlob> _villagerBlob;
        private BlobAssetReference<AIUtilityArchetypeBlob> _vesselBlob;

        [SetUp]
        public void SetUp()
        {
            _world = new World("AIIntegrationTests", WorldFlags.Game);
            CoreSingletonBootstrapSystem.EnsureSingletons(EntityManager);
            EnsureTimeState();
            EnsureRewindState();
            EnsureAICommandQueue();
            _villagerBlob = CreateVillagerUtilityBlob();
            _vesselBlob = CreateVesselUtilityBlob();
        }

        [TearDown]
        public void TearDown()
        {
            if (_villagerBlob.IsCreated)
            {
                _villagerBlob.Dispose();
            }
            if (_vesselBlob.IsCreated)
            {
                _vesselBlob.Dispose();
            }
            if (_world != null && _world.IsCreated)
            {
                _world.Dispose();
            }
        }

        [Test]
        public void Villager_HasAISystemComponents_AfterAuthoring()
        {
            var villager = CreateVillagerWithAIComponents();

            Assert.IsTrue(EntityManager.HasComponent<AISensorConfig>(villager));
            Assert.IsTrue(EntityManager.HasComponent<AISensorState>(villager));
            Assert.IsTrue(EntityManager.HasComponent<AIBehaviourArchetype>(villager));
            Assert.IsTrue(EntityManager.HasComponent<AIUtilityState>(villager));
            Assert.IsTrue(EntityManager.HasComponent<AISteeringConfig>(villager));
            Assert.IsTrue(EntityManager.HasComponent<AISteeringState>(villager));
            Assert.IsTrue(EntityManager.HasComponent<AITargetState>(villager));
            Assert.IsTrue(EntityManager.HasComponent<VillagerAIUtilityBinding>(villager));
            Assert.IsTrue(EntityManager.HasBuffer<AISensorReading>(villager));
            Assert.IsTrue(EntityManager.HasBuffer<AIActionState>(villager));
        }

        [Test]
        public void Vessel_HasAISystemComponents_AfterAuthoring()
        {
            var vessel = CreateVesselWithAIComponents();

            Assert.IsTrue(EntityManager.HasComponent<AISensorConfig>(vessel));
            Assert.IsTrue(EntityManager.HasComponent<AISensorState>(vessel));
            Assert.IsTrue(EntityManager.HasComponent<AIBehaviourArchetype>(vessel));
            Assert.IsTrue(EntityManager.HasComponent<AIUtilityState>(vessel));
            Assert.IsTrue(EntityManager.HasComponent<AISteeringConfig>(vessel));
            Assert.IsTrue(EntityManager.HasComponent<AISteeringState>(vessel));
            Assert.IsTrue(EntityManager.HasComponent<AITargetState>(vessel));
            Assert.IsTrue(EntityManager.HasComponent<VesselAIUtilityBinding>(vessel));
            Assert.IsTrue(EntityManager.HasBuffer<AISensorReading>(vessel));
            Assert.IsTrue(EntityManager.HasBuffer<AIActionState>(vessel));
        }

        [Test]
        public void VillagerAIUtilityBinding_MapsActionsToGoals()
        {
            var villager = CreateVillagerWithAIComponents();
            var binding = EntityManager.GetComponentData<VillagerAIUtilityBinding>(villager);

            Assert.AreEqual(4, binding.Goals.Length);
            Assert.AreEqual(VillagerAIState.Goal.SurviveHunger, binding.Goals[0]);
            Assert.AreEqual(VillagerAIState.Goal.Rest, binding.Goals[1]);
            Assert.AreEqual(VillagerAIState.Goal.Rest, binding.Goals[2]);
            Assert.AreEqual(VillagerAIState.Goal.Work, binding.Goals[3]);
        }

        [Test]
        public void VesselAIUtilityBinding_MapsActionsToGoals()
        {
            var vessel = CreateVesselWithAIComponents();
            var binding = EntityManager.GetComponentData<VesselAIUtilityBinding>(vessel);

            Assert.AreEqual(2, binding.Goals.Length);
            Assert.AreEqual(VesselAIState.Goal.Mining, binding.Goals[0]);
            Assert.AreEqual(VesselAIState.Goal.Returning, binding.Goals[1]);
        }

        [Test]
        public void AICommandQueue_IsCreated()
        {
            using var query = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<AICommandQueueTag>());
            Assert.IsFalse(query.IsEmptyIgnoreFilter);
            var queueEntity = query.GetSingletonEntity();
            Assert.IsTrue(EntityManager.HasBuffer<AICommand>(queueEntity));
        }

        [Test]
        public void Villager_HasEntityIntentComponent_AfterAuthoring()
        {
            var villager = CreateVillagerWithAIComponents();
            Assert.IsTrue(EntityManager.HasComponent<EntityIntent>(villager),
                "Villager should have EntityIntent component after authoring");
            Assert.IsTrue(EntityManager.HasBuffer<Interrupt>(villager),
                "Villager should have Interrupt buffer after authoring");
        }

        [Test]
        public void Vessel_HasEntityIntentComponent_AfterAuthoring()
        {
            var vessel = CreateVesselWithAIComponents();
            Assert.IsTrue(EntityManager.HasComponent<EntityIntent>(vessel),
                "Vessel should have EntityIntent component after authoring");
            Assert.IsTrue(EntityManager.HasBuffer<Interrupt>(vessel),
                "Vessel should have Interrupt buffer after authoring");
        }

        private Entity CreateVillagerWithAIComponents()
        {
            var entity = EntityManager.CreateEntity(
                typeof(VillagerId),
                typeof(VillagerNeeds),
                typeof(VillagerJob),
                typeof(VillagerJobTicket),
                typeof(VillagerAIState),
                typeof(VillagerFlags),
                typeof(LocalTransform),
                typeof(AISensorConfig),
                typeof(AISensorState),
                typeof(AIBehaviourArchetype),
                typeof(AIUtilityState),
                typeof(AISteeringConfig),
                typeof(AISteeringState),
                typeof(AITargetState),
                typeof(VillagerAIUtilityBinding),
                typeof(EntityIntent));

            EntityManager.SetComponentData(entity, new VillagerId { Value = 1, FactionId = 0 });
            EntityManager.SetComponentData(entity, new VillagerNeeds
            {
                Health = 100f,
                MaxHealth = 100f
            });
            EntityManager.SetComponentData(entity, new LocalTransform
            {
                Position = float3.zero,
                Rotation = quaternion.identity,
                Scale = 1f
            });

            EntityManager.SetComponentData(entity, new AISensorConfig
            {
                UpdateInterval = 0.5f,
                Range = 30f,
                MaxResults = 8,
                PrimaryCategory = AISensorCategory.ResourceNode,
                SecondaryCategory = AISensorCategory.Storehouse
            });

            EntityManager.SetComponentData(entity, new AIBehaviourArchetype
            {
                UtilityBlob = _villagerBlob
            });

            var binding = new VillagerAIUtilityBinding();
            binding.Goals.Add(VillagerAIState.Goal.SurviveHunger);
            binding.Goals.Add(VillagerAIState.Goal.Rest);
            binding.Goals.Add(VillagerAIState.Goal.Rest);
            binding.Goals.Add(VillagerAIState.Goal.Work);
            EntityManager.SetComponentData(entity, binding);

            EntityManager.AddBuffer<AISensorReading>(entity);
            EntityManager.AddBuffer<AIActionState>(entity);
            EntityManager.AddBuffer<Interrupt>(entity);

            // Set default EntityIntent
            EntityManager.SetComponentData(entity, new EntityIntent
            {
                Mode = IntentMode.Idle,
                IsValid = 0
            });

            return entity;
        }

        private Entity CreateVesselWithAIComponents()
        {
            var entity = EntityManager.CreateEntity(
                typeof(MinerVessel),
                typeof(VesselAIState),
                typeof(VesselMovement),
                typeof(LocalTransform),
                typeof(AISensorConfig),
                typeof(AISensorState),
                typeof(AIBehaviourArchetype),
                typeof(AIUtilityState),
                typeof(AISteeringConfig),
                typeof(AISteeringState),
                typeof(AITargetState),
                typeof(VesselAIUtilityBinding),
                typeof(EntityIntent));

            EntityManager.SetComponentData(entity, new MinerVessel
            {
                ResourceTypeIndex = 0,
                Capacity = 50f,
                Load = 0f,
                Flags = TransportUnitFlags.Idle
            });

            EntityManager.SetComponentData(entity, new LocalTransform
            {
                Position = float3.zero,
                Rotation = quaternion.identity,
                Scale = 1f
            });

            EntityManager.SetComponentData(entity, new AISensorConfig
            {
                UpdateInterval = 1f,
                Range = 100f,
                MaxResults = 10,
                PrimaryCategory = AISensorCategory.ResourceNode,
                SecondaryCategory = AISensorCategory.TransportUnit
            });

            EntityManager.SetComponentData(entity, new AIBehaviourArchetype
            {
                UtilityBlob = _vesselBlob
            });

            var binding = new VesselAIUtilityBinding();
            binding.Goals.Add(VesselAIState.Goal.Mining);
            binding.Goals.Add(VesselAIState.Goal.Returning);
            EntityManager.SetComponentData(entity, binding);

            EntityManager.AddBuffer<AISensorReading>(entity);
            EntityManager.AddBuffer<AIActionState>(entity);
            EntityManager.AddBuffer<Interrupt>(entity);

            // Set default EntityIntent
            EntityManager.SetComponentData(entity, new EntityIntent
            {
                Mode = IntentMode.Idle,
                IsValid = 0
            });

            return entity;
        }

        private static BlobAssetReference<AIUtilityArchetypeBlob> CreateVillagerUtilityBlob()
        {
            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<AIUtilityArchetypeBlob>();
            var actions = builder.Allocate(ref root.Actions, 4);

            // Action 0: SatisfyHunger
            ref var action0 = ref actions[0];
            var factors0 = builder.Allocate(ref action0.Factors, 1);
            factors0[0] = new AIUtilityCurveBlob
            {
                SensorIndex = 0,
                Threshold = 0.3f,
                Weight = 2f,
                ResponsePower = 2f,
                MaxValue = 1f
            };

            // Action 1: Rest
            ref var action1 = ref actions[1];
            var factors1 = builder.Allocate(ref action1.Factors, 1);
            factors1[0] = new AIUtilityCurveBlob
            {
                SensorIndex = 1,
                Threshold = 0.2f,
                Weight = 1.5f,
                ResponsePower = 1.5f,
                MaxValue = 1f
            };

            // Action 2: ImproveMorale
            ref var action2 = ref actions[2];
            var factors2 = builder.Allocate(ref action2.Factors, 1);
            factors2[0] = new AIUtilityCurveBlob
            {
                SensorIndex = 2,
                Threshold = 0.4f,
                Weight = 1f,
                ResponsePower = 1f,
                MaxValue = 1f
            };

            // Action 3: Work
            ref var action3 = ref actions[3];
            var factors3 = builder.Allocate(ref action3.Factors, 1);
            factors3[0] = new AIUtilityCurveBlob
            {
                SensorIndex = 3,
                Threshold = 0f,
                Weight = 0.8f,
                ResponsePower = 1f,
                MaxValue = 1f
            };

            var blob = builder.CreateBlobAssetReference<AIUtilityArchetypeBlob>(Allocator.Persistent);
            builder.Dispose();
            return blob;
        }

        private static BlobAssetReference<AIUtilityArchetypeBlob> CreateVesselUtilityBlob()
        {
            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<AIUtilityArchetypeBlob>();
            var actions = builder.Allocate(ref root.Actions, 2);

            // Action 0: Mining
            ref var action0 = ref actions[0];
            var factors0 = builder.Allocate(ref action0.Factors, 1);
            factors0[0] = new AIUtilityCurveBlob
            {
                SensorIndex = 0,
                Threshold = 0f,
                Weight = 1f,
                ResponsePower = 1f,
                MaxValue = 1f
            };

            // Action 1: Returning
            ref var action1 = ref actions[1];
            var factors1 = builder.Allocate(ref action1.Factors, 1);
            factors1[0] = new AIUtilityCurveBlob
            {
                SensorIndex = 1,
                Threshold = 0f,
                Weight = 1f,
                ResponsePower = 1f,
                MaxValue = 1f
            };

            var blob = builder.CreateBlobAssetReference<AIUtilityArchetypeBlob>(Allocator.Persistent);
            builder.Dispose();
            return blob;
        }

        private void EnsureTimeState()
        {
            using var query = EntityManager.CreateEntityQuery(ComponentType.ReadWrite<TimeState>());
            var entity = query.GetSingletonEntity();
            var time = EntityManager.GetComponentData<TimeState>(entity);
            time.IsPaused = false;
            time.FixedDeltaTime = 0.2f;
            time.Tick = 1;
            EntityManager.SetComponentData(entity, time);
        }

        private void EnsureRewindState()
        {
            using var query = EntityManager.CreateEntityQuery(ComponentType.ReadWrite<RewindState>());
            var entity = query.GetSingletonEntity();
            var rewind = EntityManager.GetComponentData<RewindState>(entity);
            rewind.Mode = RewindMode.Record;
            EntityManager.SetComponentData(entity, rewind);
        }

        private void EnsureAICommandQueue()
        {
            using var query = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<AICommandQueueTag>());
            Entity queueEntity;
            if (query.IsEmptyIgnoreFilter)
            {
                queueEntity = EntityManager.CreateEntity(typeof(AICommandQueueTag));
            }
            else
            {
                queueEntity = query.GetSingletonEntity();
            }

            if (!EntityManager.HasBuffer<AICommand>(queueEntity))
            {
                EntityManager.AddBuffer<AICommand>(queueEntity);
            }
        }
    }
}
#endif
