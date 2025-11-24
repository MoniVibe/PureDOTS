using NUnit.Framework;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Presentation;
using PureDOTS.Systems;
using PureDOTS.Tests.Playmode;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.TestTools;

namespace PureDOTS.Tests.Playmode
{
    /// <summary>
    /// Tests for presentation bridge contract: ECB-only mutations, optionality, and failure paths.
    /// </summary>
    public class Presentation_Bridge_Contract_Tests : EcsTestFixture
    {
        private GameObject _bridgeGameObject;
        private PresentationBridge _bridge;
        private BlobAssetReference<PresentationBindingBlob> _bindingBlob;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            // Bootstrap presentation systems
            RunSystem<PresentationBootstrapSystem>();

            // Create binding blob
            _bindingBlob = CreateTestBindingBlob();
            var bindingEntity = EntityManager.CreateEntity();
            EntityManager.AddComponentData(bindingEntity, new PresentationBindingReference { Binding = _bindingBlob });

            // Create bridge GameObject
            _bridgeGameObject = new GameObject("TestPresentationBridge");
            _bridge = _bridgeGameObject.AddComponent<PresentationBridge>();
            PresentationBridgeLocator.Register(_bridge);

        }

        [TearDown]
        public override void TearDown()
        {
            if (_bridge != null)
            {
                PresentationBridgeLocator.Unregister(_bridge);
            }

            if (_bridgeGameObject != null)
            {
                Object.DestroyImmediate(_bridgeGameObject);
            }

            if (_bindingBlob.IsCreated)
            {
                _bindingBlob.Dispose();
            }

            base.TearDown();
        }

        [UnityTest]
        public System.Collections.IEnumerator ECB_Order_Verification()
        {
            // Create target entities
            var target1 = EntityManager.CreateEntity();
            EntityManager.AddComponentData(target1, LocalTransform.FromPositionRotationScale(
                new float3(1, 0, 0), quaternion.identity, 1f));

            var target2 = EntityManager.CreateEntity();
            EntityManager.AddComponentData(target2, LocalTransform.FromPositionRotationScale(
                new float3(2, 0, 0), quaternion.identity, 1f));

            // Get request hub
            var hubEntity = RequireSingletonEntity<PresentationRequestHub>();
            var spawnRequests = EntityManager.GetBuffer<SpawnCompanionRequest>(hubEntity);

            // Enqueue spawn requests in Simulation
            spawnRequests.Add(new SpawnCompanionRequest
            {
                CompanionId = 7, // From test binding
                Target = target1,
                Position = new float3(1, 0, 0),
                Rotation = quaternion.identity,
                AttachRule = PresentationAttachRule.FollowTarget
            });

            spawnRequests.Add(new SpawnCompanionRequest
            {
                CompanionId = 7,
                Target = target2,
                Position = new float3(2, 0, 0),
                Rotation = quaternion.identity,
                AttachRule = PresentationAttachRule.FollowTarget
            });

            // Run presentation systems (this should process requests and play back ECBs)
            RunManagedSystem<BeginPresentationECBSystem>();
            RunSystem<PresentationBridgePlaybackSystem>();
            RunSystem<PresentationCleanupSystem>();
            RunManagedSystem<EndPresentationECBSystem>();

            yield return null; // Allow one frame for cleanup

            // Verify companions were created
            Assert.IsTrue(EntityManager.HasComponent<CompanionPresentation>(target1),
                "Target1 should have CompanionPresentation component");
            Assert.IsTrue(EntityManager.HasComponent<CompanionPresentation>(target2),
                "Target2 should have CompanionPresentation component");
            Assert.IsTrue(EntityManager.HasComponent<Presentable>(target1),
                "Target1 should have Presentable tag");
            Assert.IsTrue(EntityManager.HasComponent<Presentable>(target2),
                "Target2 should have Presentable tag");

            // Verify requests were cleared
            Assert.AreEqual(0, spawnRequests.Length, "Spawn requests should be cleared after processing");
        }

        [UnityTest]
        public System.Collections.IEnumerator Optionality_WithoutBridge()
        {
            // Unregister bridge to simulate missing bridge scenario
            PresentationBridgeLocator.Unregister(_bridge);
            Object.DestroyImmediate(_bridgeGameObject);
            _bridgeGameObject = null;
            _bridge = null;

            // Create target entity
            var target = EntityManager.CreateEntity();
            EntityManager.AddComponentData(target, LocalTransform.FromPositionRotationScale(
                new float3(1, 0, 0), quaternion.identity, 1f));

            // Get request hub
            var hubEntity = RequireSingletonEntity<PresentationRequestHub>();
            var spawnRequests = EntityManager.GetBuffer<SpawnCompanionRequest>(hubEntity);
            var failureCounts = EntityManager.GetComponentData<PresentationRequestFailures>(hubEntity);

            int initialMissingBridge = failureCounts.MissingBridge;

            // Enqueue spawn request
            spawnRequests.Add(new SpawnCompanionRequest
            {
                CompanionId = 7,
                Target = target,
                Position = new float3(1, 0, 0),
                Rotation = quaternion.identity
            });

            // Run presentation systems - should not throw exceptions
            Assert.DoesNotThrow(() =>
            {
                RunManagedSystem<BeginPresentationECBSystem>();
                RunSystem<PresentationBridgePlaybackSystem>();
                RunSystem<PresentationCleanupSystem>();
                RunManagedSystem<EndPresentationECBSystem>();
            }, "Systems should run without exceptions even when bridge is missing");

            yield return null;

            // Verify requests were cleared
            Assert.AreEqual(0, spawnRequests.Length,
                "Requests should be cleared even when bridge is missing");

            // Verify failure counter incremented
            failureCounts = EntityManager.GetComponentData<PresentationRequestFailures>(hubEntity);
            Assert.Greater(failureCounts.MissingBridge, initialMissingBridge,
                "MissingBridge counter should increment when bridge is absent");

            // Verify simulation continues (target entity still exists)
            Assert.IsTrue(EntityManager.Exists(target),
                "Simulation should continue even when bridge is missing");
        }

        [UnityTest]
        public System.Collections.IEnumerator IdLookup_FailurePath()
        {
            // Create target entity
            var target = EntityManager.CreateEntity();
            EntityManager.AddComponentData(target, LocalTransform.FromPositionRotationScale(
                new float3(1, 0, 0), quaternion.identity, 1f));

            // Get request hub
            var hubEntity = RequireSingletonEntity<PresentationRequestHub>();
            var spawnRequests = EntityManager.GetBuffer<SpawnCompanionRequest>(hubEntity);
            var effectRequests = EntityManager.GetBuffer<PlayEffectRequest>(hubEntity);
            var failureCounts = EntityManager.GetComponentData<PresentationRequestFailures>(hubEntity);

            int initialMissingBindings = failureCounts.MissingBindings;

            // Enqueue request with unknown CompanionId
            spawnRequests.Add(new SpawnCompanionRequest
            {
                CompanionId = 999, // Unknown ID
                Target = target,
                Position = new float3(1, 0, 0),
                Rotation = quaternion.identity
            });

            // Enqueue request with unknown EffectId
            effectRequests.Add(new PlayEffectRequest
            {
                EffectId = 999, // Unknown ID
                Target = target,
                Position = new float3(1, 0, 0),
                Rotation = quaternion.identity,
                DurationSeconds = 1f
            });

            // Run presentation systems - should not throw exceptions
            Assert.DoesNotThrow(() =>
            {
                RunManagedSystem<BeginPresentationECBSystem>();
                RunSystem<PresentationBridgePlaybackSystem>();
                RunSystem<PresentationCleanupSystem>();
                RunManagedSystem<EndPresentationECBSystem>();
            }, "Systems should handle unknown IDs gracefully");

            yield return null;

            // Verify no spawn happened
            Assert.IsFalse(EntityManager.HasComponent<CompanionPresentation>(target),
                "Companion should not be spawned for unknown ID");

            // Verify failure counter incremented
            failureCounts = EntityManager.GetComponentData<PresentationRequestFailures>(hubEntity);
            Assert.Greater(failureCounts.MissingBindings, initialMissingBindings,
                "MissingBindings counter should increment for unknown IDs");

            // Verify requests were cleared
            Assert.AreEqual(0, spawnRequests.Length, "Spawn requests should be cleared");
            Assert.AreEqual(0, effectRequests.Length, "Effect requests should be cleared");

            // Verify simulation continues
            Assert.IsTrue(EntityManager.Exists(target),
                "Simulation should continue after ID lookup failure");
        }

        private static BlobAssetReference<PresentationBindingBlob> CreateTestBindingBlob()
        {
            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<PresentationBindingBlob>();

            // Create one companion binding
            var companions = builder.Allocate(ref root.Companions, 1);
            companions[0] = new PresentationCompanionBinding
            {
                CompanionId = 7,
                Kind = PresentationKind.Mesh,
                Style = new PresentationStyleBlock
                {
                    Style = (FixedString64Bytes)"test.companion",
                    PaletteIndex = 0,
                    Size = 1f,
                    Speed = 1f
                },
                AttachRule = PresentationAttachRule.FollowTarget
            };

            // Create one effect binding
            var effects = builder.Allocate(ref root.Effects, 1);
            effects[0] = new PresentationEffectBinding
            {
                EffectId = 1,
                Kind = PresentationKind.Particle,
                Style = new PresentationStyleBlock
                {
                    Style = (FixedString64Bytes)"test.effect",
                    PaletteIndex = 0,
                    Size = 1f,
                    Speed = 1f
                },
                Lifetime = PresentationLifetimePolicy.Timed,
                AttachRule = PresentationAttachRule.World,
                DurationSeconds = 1f
            };

            var blob = builder.CreateBlobAssetReference<PresentationBindingBlob>(Allocator.Persistent);
            builder.Dispose();
            return blob;
        }
        private void RunManagedSystem<T>() where T : ComponentSystemBase
        {
            var system = World.GetOrCreateSystemManaged<T>();
            system.Update();
        }
    }
}

