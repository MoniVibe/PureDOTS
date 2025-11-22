using NUnit.Framework;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Presentation;
using PureDOTS.Systems;
using Unity.Collections;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace PureDOTS.Tests.Integration
{
    public class PresentationBridgePlaybackTests
    {
        private World _world;
        private EntityManager _entityManager;
        private PresentationBridge _bridge;
        private BlobAssetReference<PresentationBindingBlob> _bindingBlob;
        private Entity _requestHub;
        private Entity _rewindEntity;

        [SetUp]
        public void SetUp()
        {
            _world = new World("PresentationBridgePlaybackTests");
            _entityManager = _world.EntityManager;

            _world.GetOrCreateSystemManaged<InitializationSystemGroup>();
            _world.GetOrCreateSystemManaged<SimulationSystemGroup>();
            _world.GetOrCreateSystemManaged<PresentationSystemGroup>();
            _world.GetOrCreateSystemManaged<BeginPresentationECBSystem>();
            _world.CreateSystem<PresentationBridgePlaybackSystem>();
            _world.CreateSystem<CompanionPresentationSyncSystem>();
            _world.CreateSystem<PresentationCleanupSystem>();
            _world.GetOrCreateSystemManaged<EndPresentationECBSystem>();
            _world.GetExistingSystemManaged<PresentationSystemGroup>()?.SortSystems();

            _requestHub = _entityManager.CreateEntity(
                typeof(PresentationRequestHub),
                typeof(PresentationRequestFailures));
            _entityManager.AddBuffer<PlayEffectRequest>(_requestHub);
            _entityManager.AddBuffer<SpawnCompanionRequest>(_requestHub);
            _entityManager.AddBuffer<DespawnCompanionRequest>(_requestHub);

            var bindingRef = CreateBindingBlob();
            _bindingBlob = bindingRef.Binding;
            var bindingEntity = _entityManager.CreateEntity(typeof(PresentationBindingReference));
            _entityManager.SetComponentData(bindingEntity, bindingRef);

            _rewindEntity = _entityManager.CreateEntity(typeof(RewindState));
            _entityManager.SetComponentData(_rewindEntity, new RewindState { Mode = RewindMode.Record });

            _bridge = new GameObject("PresentationBridge").AddComponent<PresentationBridge>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_bindingBlob.IsCreated)
            {
                _bindingBlob.Dispose();
            }

            if (_bridge != null)
            {
                Object.DestroyImmediate(_bridge.gameObject);
            }

            if (_world.IsCreated)
            {
                _world.Dispose();
            }
        }

        [Test]
        public void PlayEffectRequests_ArePlayedAndCleanedUp()
        {
            _world.SetTime(new TimeData(0.1f, 0.1f));
            var effectBuffer = _entityManager.GetBuffer<PlayEffectRequest>(_requestHub);
            effectBuffer.Add(new PlayEffectRequest
            {
                EffectId = 1,
                Target = Entity.Null,
                Position = float3.zero,
                Rotation = quaternion.identity,
                DurationSeconds = 0.2f
            });

            _world.Update();

            using (var query = _entityManager.CreateEntityQuery(typeof(PresentationEffect), typeof(PresentationCleanupTag)))
            {
                Assert.AreEqual(1, query.CalculateEntityCount());
            }

            Assert.AreEqual(1, _bridge.Stats.EffectsPlayed);

            _world.Update();
            _world.Update();

            using (var query = _entityManager.CreateEntityQuery(typeof(PresentationEffect), typeof(PresentationCleanupTag)))
            {
                Assert.AreEqual(0, query.CalculateEntityCount());
            }

            Assert.AreEqual(1, _bridge.Stats.Released);
        }

        [Test]
        public void CompanionRequests_SpawnDespawn_AndReusePool()
        {
            var target = _entityManager.CreateEntity();
            var spawnBuffer = _entityManager.GetBuffer<SpawnCompanionRequest>(_requestHub);
            spawnBuffer.Add(new SpawnCompanionRequest
            {
                CompanionId = 7,
                Target = target,
                Position = float3.zero,
                Rotation = quaternion.identity
            });

            _world.Update();
            Assert.IsTrue(_entityManager.HasComponent<CompanionPresentation>(target));
            var handle = _entityManager.GetComponentData<CompanionPresentation>(target).Handle;
            Assert.Greater(handle, 0);

            var despawnBuffer = _entityManager.GetBuffer<DespawnCompanionRequest>(_requestHub);
            despawnBuffer.Add(new DespawnCompanionRequest { Target = target });
            _world.Update();

            Assert.IsFalse(_entityManager.HasComponent<CompanionPresentation>(target));
            Assert.AreEqual(1, _bridge.Stats.Released);

            spawnBuffer.Add(new SpawnCompanionRequest
            {
                CompanionId = 7,
                Target = target,
                Position = float3.zero,
                Rotation = quaternion.identity
            });

            _world.Update();
            Assert.IsTrue(_entityManager.HasComponent<CompanionPresentation>(target));
            Assert.GreaterOrEqual(_bridge.Stats.ReusedFromPool, 1);
        }

        [Test]
        public void MissingBinding_RecordsFailureAndClearsBuffers()
        {
            var bindingEntity = _entityManager.CreateEntityQuery(typeof(PresentationBindingReference)).GetSingletonEntity();
            if (_bindingBlob.IsCreated)
            {
                _bindingBlob.Dispose();
                _bindingBlob = default;
            }

            _entityManager.SetComponentData(bindingEntity, new PresentationBindingReference());

            var effectBuffer = _entityManager.GetBuffer<PlayEffectRequest>(_requestHub);
            effectBuffer.Add(new PlayEffectRequest { EffectId = 99, DurationSeconds = 0.1f, Rotation = quaternion.identity });

            _world.Update();

            var failures = _entityManager.GetComponentData<PresentationRequestFailures>(_requestHub);
            Assert.GreaterOrEqual(failures.MissingBindings, 1);
            Assert.AreEqual(0, _entityManager.GetBuffer<PlayEffectRequest>(_requestHub).Length);
            Assert.AreEqual(0, _bridge.Stats.EffectsPlayed);
        }

        [Test]
        public void Requests_AreSkippedDuringPlayback()
        {
            _entityManager.SetComponentData(_rewindEntity, new RewindState { Mode = RewindMode.Playback });

            var effectBuffer = _entityManager.GetBuffer<PlayEffectRequest>(_requestHub);
            var spawnBuffer = _entityManager.GetBuffer<SpawnCompanionRequest>(_requestHub);

            effectBuffer.Add(new PlayEffectRequest { EffectId = 1, DurationSeconds = 0.1f, Rotation = quaternion.identity });
            spawnBuffer.Add(new SpawnCompanionRequest { CompanionId = 7, Target = _entityManager.CreateEntity(), Rotation = quaternion.identity });

            _world.Update();

            Assert.AreEqual(1, _entityManager.GetBuffer<PlayEffectRequest>(_requestHub).Length);
            Assert.AreEqual(1, _entityManager.GetBuffer<SpawnCompanionRequest>(_requestHub).Length);

            var failures = _entityManager.GetComponentData<PresentationRequestFailures>(_requestHub);
            Assert.AreEqual(0, failures.MissingBridge + failures.MissingBindings + failures.FailedPlayback);
            Assert.AreEqual(0, _bridge.Stats.EffectsPlayed);
            Assert.AreEqual(0, _bridge.Stats.CompanionsSpawned);
        }

        [Test]
        public void ResolveStyleOverride_MergesPaletteSizeAndSpeed()
        {
            var bindingStyle = new PresentationStyleBlock
            {
                Style = (FixedString64Bytes)"base",
                PaletteIndex = 1,
                Size = 1f,
                Speed = 1f
            };

            var overrideStyle = new PresentationStyleOverride
            {
                Style = (FixedString64Bytes)"override",
                PaletteIndex = 3,
                Size = 2.5f,
                Speed = 0.5f
            };

            var result = PresentationBindingUtility.ResolveStyle(bindingStyle, overrideStyle);
            Assert.AreEqual("override", result.Style.ToString());
            Assert.AreEqual(3, result.PaletteIndex);
            Assert.AreEqual(2.5f, result.Size, 0.001f);
            Assert.AreEqual(0.5f, result.Speed, 0.001f);
        }

        [Test]
        public void CompanionSync_AppliesOffsetAndLerp()
        {
            _world.SetTime(new TimeData(0.1f, 0.1f));
            var target = _entityManager.CreateEntity(typeof(LocalTransform));
            _entityManager.SetComponentData(target, LocalTransform.FromPositionRotationScale(float3.zero, quaternion.identity, 1f));

            var spawnBuffer = _entityManager.GetBuffer<SpawnCompanionRequest>(_requestHub);
            spawnBuffer.Add(new SpawnCompanionRequest
            {
                CompanionId = 7,
                Target = target,
                Position = float3.zero,
                Rotation = quaternion.identity,
                Offset = new float3(0f, 2f, 0f),
                FollowLerp = 1f
            });

            _world.Update();

            Assert.IsTrue(_entityManager.HasComponent<CompanionPresentation>(target));
            var companion = _entityManager.GetComponentData<CompanionPresentation>(target);
            Assert.IsTrue(_bridge.TryGetInstance(companion.Handle, out var instance));
            Assert.AreEqual(2f, instance.transform.position.y, 0.01f);

            companion.FollowLerp = 0.5f;
            _entityManager.SetComponentData(target, companion);
            _entityManager.SetComponentData(target, LocalTransform.FromPositionRotationScale(new float3(10f, 0f, 0f), quaternion.identity, 1f));

            _world.Update();

            var pos = instance.transform.position;
            Assert.Greater(pos.x, 4.9f);
            Assert.Less(pos.x, 10.1f);
            Assert.AreEqual(2f, pos.y, 0.1f);
        }

        private PresentationBindingReference CreateBindingBlob()
        {
            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<PresentationBindingBlob>();

            var effects = builder.Allocate(ref root.Effects, 1);
            var defaultEffectStyle = new PresentationStyleBlock
            {
                Style = (FixedString64Bytes)"fx.default",
                PaletteIndex = 0,
                Size = 1f,
                Speed = 1f
            };
            effects[0] = new PresentationEffectBinding
            {
                EffectId = 1,
                Kind = PresentationKind.Particle,
                Style = defaultEffectStyle,
                Lifetime = PresentationLifetimePolicy.Timed,
                AttachRule = PresentationAttachRule.World,
                DurationSeconds = 0.2f
            };

            var companions = builder.Allocate(ref root.Companions, 1);
            var defaultCompanionStyle = new PresentationStyleBlock
            {
                Style = (FixedString64Bytes)"comp.default",
                PaletteIndex = 0,
                Size = 1f,
                Speed = 1f
            };
            companions[0] = new PresentationCompanionBinding
            {
                CompanionId = 7,
                Kind = PresentationKind.Mesh,
                Style = defaultCompanionStyle,
                AttachRule = PresentationAttachRule.FollowTarget
            };

            var blob = builder.CreateBlobAssetReference<PresentationBindingBlob>(Allocator.Persistent);
            builder.Dispose();

            return new PresentationBindingReference { Binding = blob };
        }
    }
}

