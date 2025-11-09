using NUnit.Framework;
using PureDOTS.Runtime.Components;
using PureDOTS.Systems;
using PureDOTS.Tests.Playmode;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Tests.Integration
{
    /// <summary>
    /// Integration coverage for the Phase 2 meta registries.
    /// </summary>
    public class MetaRegistryTests : EcsTestFixture
    {
        private EntityManager _entityManager;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            _entityManager = World.EntityManager;
            CoreSingletonBootstrapSystem.EnsureSingletons(_entityManager);
        }

        [Test]
        public void FactionRegistry_TracksFactionEntities()
        {
            var faction = _entityManager.CreateEntity();
            _entityManager.AddComponentData(faction, new LocalTransform
            {
                Position = new float3(10f, 0f, -5f),
                Rotation = quaternion.identity,
                Scale = 1f
            });
            _entityManager.AddComponentData(faction, new FactionId
            {
                Value = 1,
                Name = new FixedString64Bytes("Alpha"),
                Type = FactionType.PlayerControlled
            });
            _entityManager.AddComponentData(faction, new FactionState
            {
                ResourceStockpile = 1500f,
                PopulationCount = 120,
                TerritoryCellCount = 48,
                DiplomaticStatus = DiplomaticStatusFlags.Allied,
                TerritoryCenter = new float3(10f, 0f, -5f)
            });

            var systemHandle = CreateSystem<FactionRegistrySystem>();
            RunSystem<FactionRegistrySystem>(systemHandle);

            var registryEntity = _entityManager.CreateEntityQuery(typeof(FactionRegistry), typeof(FactionRegistryEntry)).GetSingletonEntity();
            var entries = _entityManager.GetBuffer<FactionRegistryEntry>(registryEntity);

            Assert.AreEqual(1, entries.Length, "Faction registry should contain one entry.");
            var entry = entries[0];
            Assert.AreEqual(1, entry.FactionId);
            Assert.AreEqual(DiplomaticStatusFlags.Allied, entry.DiplomaticStatus);
            Assert.AreEqual(FactionType.PlayerControlled, entry.FactionType);
        }

        [Test]
        public void ClimateHazardRegistry_IncludesActiveHazards()
        {
            var hazard = _entityManager.CreateEntity();
            _entityManager.AddComponentData(hazard, new LocalTransform
            {
                Position = new float3(0f, 0f, 0f),
                Rotation = quaternion.identity,
                Scale = 1f
            });
            _entityManager.AddComponentData(hazard, new ClimateHazardState
            {
                HazardType = ClimateHazardType.Storm,
                CurrentIntensity = 0.4f,
                Radius = 25f,
                MaxIntensity = 0.8f,
                StartTick = 0u,
                DurationTicks = 1200u,
                HazardName = new FixedString64Bytes("Storm"),
                AffectedEnvironmentChannels = EnvironmentChannelMask.Moisture | EnvironmentChannelMask.Wind
            });

            var systemHandle = CreateSystem<ClimateHazardRegistrySystem>();
            RunSystem<ClimateHazardRegistrySystem>(systemHandle);

            var registryEntity = _entityManager.CreateEntityQuery(typeof(ClimateHazardRegistry), typeof(ClimateHazardRegistryEntry)).GetSingletonEntity();
            var entries = _entityManager.GetBuffer<ClimateHazardRegistryEntry>(registryEntity);

            Assert.AreEqual(1, entries.Length, "Climate hazard registry should contain one active hazard.");
            var entry = entries[0];
            Assert.AreEqual(ClimateHazardType.Storm, entry.HazardType);
            Assert.AreEqual(EnvironmentChannelMask.Moisture | EnvironmentChannelMask.Wind, entry.AffectedEnvironmentChannels);
            Assert.Greater(entry.ExpirationTick, 0u);
        }

        [Test]
        public void AreaEffectRegistry_CollectsAreaEffects()
        {
            var owner = _entityManager.CreateEntity();
            _entityManager.AddComponentData(owner, LocalTransform.Identity);

            var effect = _entityManager.CreateEntity();
            _entityManager.AddComponentData(effect, new LocalTransform
            {
                Position = new float3(5f, 0f, 5f),
                Rotation = quaternion.identity,
                Scale = 1f
            });
            _entityManager.AddComponentData(effect, new AreaEffectState
            {
                EffectType = AreaEffectType.Buff,
                CurrentStrength = 1.25f,
                Radius = 8f,
                MaxStrength = 1.5f,
                OwnerEntity = owner,
                EffectId = 42,
                AffectedArchetypes = AreaEffectTargetMask.Villagers,
                EffectName = new FixedString64Bytes("Inspiration"),
                ExpirationTick = 400u
            });

            var systemHandle = CreateSystem<AreaEffectRegistrySystem>();
            RunSystem<AreaEffectRegistrySystem>(systemHandle);

            var registryEntity = _entityManager.CreateEntityQuery(typeof(AreaEffectRegistry), typeof(AreaEffectRegistryEntry)).GetSingletonEntity();
            var entries = _entityManager.GetBuffer<AreaEffectRegistryEntry>(registryEntity);

            Assert.AreEqual(1, entries.Length, "Area effect registry should contain one entry.");
            var entry = entries[0];
            Assert.AreEqual(AreaEffectType.Buff, entry.EffectType);
            Assert.AreEqual(AreaEffectTargetMask.Villagers, entry.AffectedArchetypes);
            Assert.AreEqual((ushort)42, entry.EffectId);
        }

        [Test]
        public void CultureRegistry_TracksAlignment()
        {
            var culture = _entityManager.CreateEntity();
            _entityManager.AddComponentData(culture, new LocalTransform
            {
                Position = new float3(-3f, 0f, 7f),
                Rotation = quaternion.identity,
                Scale = 1f
            });
            _entityManager.AddComponentData(culture, new CultureState
            {
                CultureId = 3,
                CultureName = new FixedString64Bytes("Coastals"),
                CultureType = CultureType.Technological,
                MemberCount = 540,
                CurrentAlignment = 0.35f,
                AlignmentVelocity = 0.01f,
                BaseAlignment = 0.25f,
                AlignmentFlags = CultureAlignmentFlags.Stable | CultureAlignmentFlags.Ascending,
                Description = new FixedString128Bytes("Coastal tech guild.")
            });

            var systemHandle = CreateSystem<CultureAlignmentRegistrySystem>();
            RunSystem<CultureAlignmentRegistrySystem>(systemHandle);

            var registryEntity = _entityManager.CreateEntityQuery(typeof(CultureAlignmentRegistry), typeof(CultureAlignmentRegistryEntry)).GetSingletonEntity();
            var entries = _entityManager.GetBuffer<CultureAlignmentRegistryEntry>(registryEntity);

            Assert.AreEqual(1, entries.Length, "Culture registry should contain one entry.");
            var entry = entries[0];
            Assert.AreEqual(3, entry.CultureId);
            Assert.AreEqual(CultureAlignmentFlags.Stable | CultureAlignmentFlags.Ascending, entry.AlignmentFlags);
            Assert.AreEqual(CultureType.Technological, entry.CultureType);
        }

        private SystemHandle CreateSystem<T>() where T : unmanaged, ISystem
        {
            var handle = World.CreateSystem<T>();
            ref var state = ref World.Unmanaged.ResolveSystemStateRef(handle);
            ref var system = ref World.Unmanaged.GetUnsafeSystemRef<T>(handle);
            system.OnCreate(ref state);
            return handle;
        }

        private void RunSystem<T>(SystemHandle handle) where T : unmanaged, ISystem
        {
            ref var system = ref World.Unmanaged.GetUnsafeSystemRef<T>(handle);
            ref var state = ref World.Unmanaged.ResolveSystemStateRef(handle);
            system.OnUpdate(ref state);
        }
    }
}
