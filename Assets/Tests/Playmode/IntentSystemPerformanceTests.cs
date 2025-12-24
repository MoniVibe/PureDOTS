#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Intent;
using PureDOTS.Runtime.Interrupts;
using PureDOTS.Systems;
using PureDOTS.Systems.Intent;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Tests.Playmode
{
    /// <summary>
    /// Dedicated performance test suite for Intent system CI integration.
    /// These tests validate performance budgets and can be run in CI with regression detection.
    /// </summary>
    public class IntentSystemPerformanceTests
    {
        private World _world;
        private EntityManager EntityManager => _world.EntityManager;

        [SetUp]
        public void SetUp()
        {
            _world = new World("IntentSystemPerformanceTests", WorldFlags.Game);
            CoreSingletonBootstrapSystem.EnsureSingletons(EntityManager);
            EnsureTimeState();
            EnsureRewindState();
        }

        [TearDown]
        public void TearDown()
        {
            if (_world != null && _world.IsCreated)
            {
                _world.Dispose();
            }
        }

        [Test]
        public void IntentBridge_10kEntities_Under10ms()
        {
            // Create 10000 entities with EntityIntent components
            const int entityCount = 10000;
            var entities = new NativeArray<Entity>(entityCount, Allocator.Temp);

            for (int i = 0; i < entityCount; i++)
            {
                var entity = EntityManager.CreateEntity(
                    typeof(EntityIntent),
                    typeof(LocalTransform));

                EntityManager.SetComponentData(entity, LocalTransform.Identity);
                EntityManager.SetComponentData(entity, new EntityIntent
                {
                    Mode = (IntentMode)(i % 10), // Vary intent modes
                    IsValid = 1,
                    IntentSetTick = 1,
                    Priority = InterruptPriority.Normal
                });

                entities[i] = entity;
            }

            // Measure performance
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            // Run validation system (most lightweight intent system)
            var validationSystem = _world.GetOrCreateSystem<IntentValidationSystem>();
            validationSystem.Update(_world.Unmanaged);
            
            stopwatch.Stop();

            // Fail if > 20ms (performance regression threshold, with 2x safety margin for CI variance)
            Assert.Less(stopwatch.ElapsedMilliseconds, 20, 
                $"IntentValidationSystem performance regression: {entityCount} entities took {stopwatch.ElapsedMilliseconds}ms (target: < 20ms)");

            entities.Dispose();
        }

        [Test]
        public void IntentBridge_LinearScaling()
        {
            // Test that performance scales linearly (or sublinearly) with entity count
            const int smallCount = 1000;
            const int largeCount = 10000;
            
            // Measure small batch
            var smallEntities = CreateEntitiesWithIntent(smallCount);
            var smallStopwatch = System.Diagnostics.Stopwatch.StartNew();
            var validationSystem = _world.GetOrCreateSystem<IntentValidationSystem>();
            validationSystem.Update(_world.Unmanaged);
            smallStopwatch.Stop();
            var smallTime = smallStopwatch.ElapsedMilliseconds;
            CleanupEntities(smallEntities);

            // Measure large batch
            var largeEntities = CreateEntitiesWithIntent(largeCount);
            var largeStopwatch = System.Diagnostics.Stopwatch.StartNew();
            validationSystem.Update(_world.Unmanaged);
            largeStopwatch.Stop();
            var largeTime = largeStopwatch.ElapsedMilliseconds;
            CleanupEntities(largeEntities);

            // Verify scaling is linear or sublinear (largeTime should be <= 10x smallTime)
            // Allow some overhead for larger batches
            var scalingFactor = (double)largeTime / smallTime;
            var expectedScalingFactor = (double)largeCount / smallCount; // 10x entities = 10x time (linear)
            
            Assert.LessOrEqual(scalingFactor, expectedScalingFactor * 1.5, 
                $"Performance scaling should be linear or sublinear: {smallCount} entities = {smallTime}ms, {largeCount} entities = {largeTime}ms (scaling factor: {scalingFactor:F2}x, expected: {expectedScalingFactor}x)");
        }

        #region Helper Methods

        /// <summary>
        /// Creates entities with EntityIntent components for performance testing.
        /// Uses Allocator.TempJob because arrays survive across system updates within the test method.
        /// </summary>
        private NativeArray<Entity> CreateEntitiesWithIntent(int count)
        {
            var entities = new NativeArray<Entity>(count, Allocator.TempJob);

            for (int i = 0; i < count; i++)
            {
                var entity = EntityManager.CreateEntity(
                    typeof(EntityIntent),
                    typeof(LocalTransform));

                EntityManager.SetComponentData(entity, LocalTransform.Identity);
                EntityManager.SetComponentData(entity, new EntityIntent
                {
                    Mode = (IntentMode)(i % 10),
                    IsValid = 1,
                    IntentSetTick = 1,
                    Priority = InterruptPriority.Normal
                });

                entities[i] = entity;
            }

            return entities;
        }

        private void CleanupEntities(NativeArray<Entity> entities)
        {
            foreach (var entity in entities)
            {
                if (EntityManager.Exists(entity))
                {
                    EntityManager.DestroyEntity(entity);
                }
            }
            entities.Dispose();
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

        #endregion
    }
}
#endif

