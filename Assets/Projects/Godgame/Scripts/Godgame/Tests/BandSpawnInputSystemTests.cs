using Godgame.Interaction;
using Godgame.Interaction.Input;
using Godgame.Presentation;
using NUnit.Framework;
using PureDOTS.Runtime.Bands;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Presentation;
using PureDOTS.Systems;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Godgame.Tests.Interaction
{
    public class BandSpawnInputSystemTests
    {
        private World _world;
        private EntityManager _entityManager;
        private Entity _inputEntity;

        private SystemHandle _bandSpawnHandle;
        private SystemHandle _endSimEcbHandle;
        private SystemHandle _bandRegistryHandle;
        private SystemHandle _presentationBootstrapHandle;
        private SystemHandle _presentationBindingBootstrapHandle;

        [SetUp]
        public void SetUp()
        {
            _world = new World("BandSpawnInputSystemTests");
            _entityManager = _world.EntityManager;

            CoreSingletonBootstrapSystem.EnsureSingletons(_entityManager);

            _inputEntity = _entityManager.CreateEntity(typeof(InputState));
            _entityManager.SetComponentData(_inputEntity, new InputState
            {
                PointerWorld = new float3(3f, 0f, 5f),
                PointerWorldValid = true,
                PrimaryClicked = true,
                PrimaryHeld = true
            });

            _bandSpawnHandle = _world.GetOrCreateSystem<BandSpawnInputSystem>();
            _endSimEcbHandle = _world.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            _bandRegistryHandle = _world.GetOrCreateSystem<BandRegistrySystem>();
            _presentationBootstrapHandle = _world.GetOrCreateSystem<PresentationBootstrapSystem>();
            _presentationBindingBootstrapHandle = _world.GetOrCreateSystem<GodgamePresentationBindingBootstrapSystem>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_world.IsCreated)
            {
                if (_entityManager.CreateEntityQuery(typeof(PresentationBindingReference)).TryGetSingleton(out PresentationBindingReference binding) &&
                    binding.Binding.IsCreated)
                {
                    binding.Binding.Dispose();
                }
            }

            if (_world.IsCreated)
            {
                _world.Dispose();
            }
        }

        [Test]
        public void SpawnAndEffectRequestFlow()
        {
            UpdateSystem(_presentationBootstrapHandle);
            UpdateSystem(_presentationBindingBootstrapHandle);

            // Frame 1: click to spawn a band at the pointer world position.
            UpdateSystem(_bandSpawnHandle);
            UpdateSystem(_endSimEcbHandle);

            var bandQuery = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<BandId>(), ComponentType.ReadOnly<LocalTransform>());
            var bandEntity = bandQuery.GetSingletonEntity();
            var bandId = _entityManager.GetComponentData<BandId>(bandEntity);
            Assert.AreEqual(1, bandId.Value);

            var selection = _entityManager.CreateEntityQuery(typeof(BandSelectionState)).GetSingleton<BandSelectionState>();
            Assert.AreEqual(bandEntity, selection.SelectedBand);

            UpdateSystem(_bandRegistryHandle);
            using var registryQuery = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<BandRegistry>());
            var registryEntity = registryQuery.GetSingletonEntity();
            var entries = _entityManager.GetBuffer<BandRegistryEntry>(registryEntity);
            Assert.AreEqual(1, entries.Length);
            Assert.AreEqual(bandEntity, entries[0].BandEntity);

            // Frame 2: press Q to enqueue an effect on the selected target.
            var input = _entityManager.GetComponentData<InputState>(_inputEntity);
            input.PrimaryClicked = false;
            input.PrimaryHeld = false;
            input.EffectTriggered = true;
            input.PointerWorld = new float3(8f, 0f, -2f);
            _entityManager.SetComponentData(_inputEntity, input);

            UpdateSystem(_bandSpawnHandle);

            using var queueQuery = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<PresentationCommandQueue>());
            var queueEntity = queueQuery.GetSingletonEntity();
            var effects = _entityManager.GetBuffer<PlayEffectRequest>(queueEntity);
            Assert.AreEqual(1, effects.Length);
            Assert.AreEqual(GodgamePresentationIds.MiraclePingEffectId, effects[0].EffectId);
            Assert.AreEqual(selection.SelectedBand, effects[0].Target);
            Assert.AreEqual(input.PointerWorld, effects[0].Position);
        }

        private void UpdateSystem(SystemHandle handle)
        {
            handle.Update(_world.Unmanaged);
        }
    }
}
