using NUnit.Framework;
using PureDOTS.Environment;
using PureDOTS.Runtime.Components;
using PureDOTS.Systems;
using Unity.Mathematics;
using Unity.Entities;
using PresentationSystemGroup = PureDOTS.Systems.PresentationSystemGroup;

namespace PureDOTS.Tests
{
    public class SystemIntegrationPlaymodeTests
    {
        [Test]
        public void EnvironmentGridMath_TryWorldToCell_ReturnsTrueInsideBounds()
        {
            var metadata = EnvironmentGridMetadata.Create(new float3(0f, 0f, 0f), new float3(10f, 0f, 10f), 1f, new int2(10, 10));
            var inside = new float3(2.25f, 0f, 7.75f);

            var success = EnvironmentGridMath.TryWorldToCell(metadata, inside, out var cell, out var fractional);

            Assert.IsTrue(success);
            Assert.AreEqual(new int2(2, 7), cell);
            Assert.That(fractional.x, Is.InRange(0f, 1f));
            Assert.That(fractional.y, Is.InRange(0f, 1f));
        }

        [Test]
        public void EnvironmentGridMath_TryWorldToCell_ReturnsFalseOutsideBoundsButClampsOutput()
        {
            var metadata = EnvironmentGridMetadata.Create(new float3(-5f, 0f, -5f), new float3(5f, 0f, 5f), 1f, new int2(10, 10));
            var outside = new float3(6.2f, 0f, 3.1f);

            var success = EnvironmentGridMath.TryWorldToCell(metadata, outside, out var cell, out var fractional);

            Assert.IsFalse(success);
            var max = metadata.MaxCellIndex;
            Assert.AreEqual(max.x, cell.x);
            Assert.That(cell.y, Is.InRange(0, max.y));
            Assert.That(fractional.x, Is.InRange(0f, 1f));
            Assert.That(fractional.y, Is.InRange(0f, 1f));
        }

        [Test]
        public void RewindGuards_DisableSimulationGroupsDuringPlayback()
        {
            using var world = new World("RewindGuardTest");
            var entityManager = world.EntityManager;

            var rewindEntity = entityManager.CreateEntity(typeof(RewindState), typeof(RewindLegacyState));
            entityManager.SetComponentData(rewindEntity, new RewindState
            {
                Mode = RewindMode.Record,
                TargetTick = 0,
                TickDuration = 1f / 60f,
                MaxHistoryTicks = 600,
                PendingStepTicks = 0
            });
            entityManager.SetComponentData(rewindEntity, new RewindLegacyState
            {
                PlaybackSpeed = 1f,
                CurrentTick = 0,
                StartTick = 0,
                PlaybackTick = 0,
                PlaybackTicksPerSecond = 60f,
                ScrubDirection = 0,
                ScrubSpeedMultiplier = 1f,
                RewindWindowTicks = 0,
                ActiveTrack = default
            });

            var simulationGroup = world.GetOrCreateSystemManaged<SimulationSystemGroup>();
            var environmentGroup = world.GetOrCreateSystemManaged<EnvironmentSystemGroup>();
            var spatialGroup = world.GetOrCreateSystemManaged<SpatialSystemGroup>();
            var gameplayGroup = world.GetOrCreateSystemManaged<GameplaySystemGroup>();
            var presentationGroup = world.GetOrCreateSystemManaged<PresentationSystemGroup>();

            world.EnsureSystem<EnvironmentRewindGuardSystem>();
            world.EnsureSystem<SpatialRewindGuardSystem>();
            world.EnsureSystem<GameplayRewindGuardSystem>();
            world.EnsureSystem<PresentationRewindGuardSystem>();

            world.UpdateSystem<EnvironmentRewindGuardSystem>();
            world.UpdateSystem<SpatialRewindGuardSystem>();
            world.UpdateSystem<GameplayRewindGuardSystem>();
            world.UpdateSystem<PresentationRewindGuardSystem>();

            Assert.IsTrue(environmentGroup.Enabled);
            Assert.IsTrue(spatialGroup.Enabled);
            Assert.IsTrue(gameplayGroup.Enabled);
            Assert.IsTrue(presentationGroup.Enabled);

            entityManager.SetComponentData(rewindEntity, new RewindState
            {
                Mode = RewindMode.Playback
            });

            world.UpdateSystem<EnvironmentRewindGuardSystem>();
            world.UpdateSystem<SpatialRewindGuardSystem>();
            world.UpdateSystem<GameplayRewindGuardSystem>();
            world.UpdateSystem<PresentationRewindGuardSystem>();

            Assert.IsFalse(environmentGroup.Enabled);
            Assert.IsFalse(spatialGroup.Enabled);
            Assert.IsFalse(gameplayGroup.Enabled);
            Assert.IsTrue(presentationGroup.Enabled);

            entityManager.SetComponentData(rewindEntity, new RewindState
            {
                Mode = RewindMode.CatchUp
            });

            world.UpdateSystem<EnvironmentRewindGuardSystem>();
            world.UpdateSystem<SpatialRewindGuardSystem>();
            world.UpdateSystem<GameplayRewindGuardSystem>();
            world.UpdateSystem<PresentationRewindGuardSystem>();

            Assert.IsTrue(environmentGroup.Enabled);
            Assert.IsTrue(spatialGroup.Enabled);
            Assert.IsTrue(gameplayGroup.Enabled);
            Assert.IsFalse(presentationGroup.Enabled);
        }

        [Test]
        public void HandInputRouterSystem_ResolvesHighestPriorityRequest()
        {
            using var world = new World("HandInputRouterTest");
            var entityManager = world.EntityManager;

            var hand = entityManager.CreateEntity();
            entityManager.AddComponentData(hand, HandInputRouteResult.None);
            entityManager.AddComponentData(hand, new DivineHandCommand
            {
                Type = DivineHandCommandType.None,
                TargetEntity = Entity.Null,
                TargetPosition = float3.zero,
                TargetNormal = new float3(0f, 1f, 0f),
                TimeSinceIssued = 0f
            });
            var requests = entityManager.AddBuffer<HandInputRouteRequest>(hand);

            requests.Add(HandInputRouteRequest.Create(
                HandRouteSource.AuthoringBridge,
                HandRoutePhase.Started,
                HandRoutePriority.DumpToStorehouse,
                DivineHandCommandType.Dump,
                Entity.Null,
                new float3(1f, 0f, 0f),
                new float3(0f, 1f, 0f)));

            requests.Add(HandInputRouteRequest.Create(
                HandRouteSource.ResourceSystem,
                HandRoutePhase.Started,
                HandRoutePriority.ResourceSiphon,
                DivineHandCommandType.Siphon,
                Entity.Null,
                new float3(2f, 0f, 0f),
                new float3(0f, 1f, 0f)));

            var router = world.GetOrCreateSystem<HandInputRouterSystem>();
            router.Update(world.Unmanaged);

            var command = entityManager.GetComponentData<DivineHandCommand>(hand);
            Assert.AreEqual(DivineHandCommandType.Siphon, command.Type);
            Assert.That(command.TargetPosition.x, Is.EqualTo(2f).Within(1e-5f));

            // Clear buffer automatically, then enqueue cancel to ensure command resets.
            requests.Add(HandInputRouteRequest.Create(
                HandRouteSource.ResourceSystem,
                HandRoutePhase.Canceled,
                HandRoutePriority.ResourceSiphon,
                DivineHandCommandType.Siphon,
                Entity.Null,
                float3.zero,
                new float3(0f, 1f, 0f)));

            router.Update(world.Unmanaged);

            command = entityManager.GetComponentData<DivineHandCommand>(hand);
            Assert.AreEqual(DivineHandCommandType.None, command.Type);
            Assert.AreEqual(0f, command.TimeSinceIssued);
        }
    }
}
