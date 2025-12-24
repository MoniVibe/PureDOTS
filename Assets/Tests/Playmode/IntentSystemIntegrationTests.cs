#if UNITY_INCLUDE_TESTS
using System.Reflection;
using NUnit.Framework;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Intent;
using PureDOTS.Runtime.Interrupts;
using PureDOTS.Systems;
using PureDOTS.Systems.Intent;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Tests.Playmode
{
    /// <summary>
    /// Integration tests for Intent system - validates cross-game integration and edge cases.
    /// </summary>
    public class IntentSystemIntegrationTests
    {
        private World _world;
        private EntityManager EntityManager => _world.EntityManager;

        [SetUp]
        public void SetUp()
        {
            _world = new World("IntentSystemIntegrationTests", WorldFlags.Game);
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

        #region Suite 3: QueuedIntent Buffer Behavior

        [Test]
        public void IntentProcessingSystem_NoQueuedIntentBuffer_NoError()
        {
            // Create entity with EntityIntent but no QueuedIntent buffer
            var entity = EntityManager.CreateEntity(
                typeof(EntityIntent),
                typeof(LocalTransform));

            EntityManager.SetComponentData(entity, LocalTransform.Identity);
            EntityManager.SetComponentData(entity, new EntityIntent
            {
                Mode = IntentMode.Idle,
                IsValid = 0
            });

            // Run IntentProcessingSystem (should handle missing buffer gracefully)
            var processingSystem = _world.GetOrCreateSystem<IntentProcessingSystem>();
            
            // Should not throw exception
            Assert.DoesNotThrow(() => processingSystem.Update(_world.Unmanaged),
                "IntentProcessingSystem should handle missing QueuedIntent buffer gracefully");
        }

        [Test]
        public void IntentProcessingSystem_QueuedIntent_Promotion()
        {
            // Create entity with EntityIntent and QueuedIntent buffer
            var entity = EntityManager.CreateEntity(
                typeof(EntityIntent),
                typeof(LocalTransform));

            EntityManager.SetComponentData(entity, LocalTransform.Identity);
            
            // Set current intent to Idle (invalid)
            EntityManager.SetComponentData(entity, new EntityIntent
            {
                Mode = IntentMode.Idle,
                IsValid = 0,
                IntentSetTick = 1,
                Priority = InterruptPriority.Normal
            });

            // Add QueuedIntent buffer
            var queueBuffer = EntityManager.AddBuffer<QueuedIntent>(entity);
            var targetEntity = EntityManager.CreateEntity(typeof(LocalTransform));
            EntityManager.SetComponentData(targetEntity, LocalTransform.Identity);

            // Add queued intent to buffer
            queueBuffer.Add(new QueuedIntent
            {
                Mode = IntentMode.Gather,
                TargetEntity = targetEntity,
                TargetPosition = float3.zero,
                Priority = InterruptPriority.Normal,
                TriggeringInterrupt = InterruptType.ResourceSpotted,
                RequestedTick = 1
            });

            // Run IntentProcessingSystem
            var processingSystem = _world.GetOrCreateSystem<IntentProcessingSystem>();
            processingSystem.Update(_world.Unmanaged);

            // Verify queued intent is promoted to current intent
            var intent = EntityManager.GetComponentData<EntityIntent>(entity);
            Assert.AreEqual(IntentMode.Gather, intent.Mode, "Queued intent should be promoted to current intent");
            Assert.AreEqual(1, intent.IsValid, "Intent should be valid after promotion");
            Assert.AreEqual(targetEntity, intent.TargetEntity, "Target entity should match queued intent");

            // Verify queued intent is removed from buffer
            var updatedQueueBuffer = EntityManager.GetBuffer<QueuedIntent>(entity);
            Assert.AreEqual(0, updatedQueueBuffer.Length, "Queued intent should be removed from buffer after promotion");
        }

        [Test]
        public void IntentProcessingSystem_QueuedIntent_PriorityPromotion()
        {
            // Create entity with EntityIntent (Normal priority) and QueuedIntent buffer
            var entity = EntityManager.CreateEntity(
                typeof(EntityIntent),
                typeof(LocalTransform));

            EntityManager.SetComponentData(entity, LocalTransform.Identity);
            
            // Set current intent with Normal priority
            EntityManager.SetComponentData(entity, new EntityIntent
            {
                Mode = IntentMode.Gather,
                IsValid = 1,
                IntentSetTick = 1,
                Priority = InterruptPriority.Normal
            });

            // Add QueuedIntent buffer
            var queueBuffer = EntityManager.AddBuffer<QueuedIntent>(entity);
            var targetEntity = EntityManager.CreateEntity(typeof(LocalTransform));
            EntityManager.SetComponentData(targetEntity, LocalTransform.Identity);

            // Add queued intent with High priority
            queueBuffer.Add(new QueuedIntent
            {
                Mode = IntentMode.Flee,
                TargetEntity = Entity.Null,
                TargetPosition = float3.zero,
                Priority = InterruptPriority.High,
                TriggeringInterrupt = InterruptType.LowHealth,
                RequestedTick = 2
            });

            // Run IntentProcessingSystem
            var processingSystem = _world.GetOrCreateSystem<IntentProcessingSystem>();
            processingSystem.Update(_world.Unmanaged);

            // Verify queued intent is promoted (higher priority overrides)
            var intent = EntityManager.GetComponentData<EntityIntent>(entity);
            Assert.AreEqual(IntentMode.Flee, intent.Mode, "Higher priority queued intent should override current intent");
            Assert.AreEqual(InterruptPriority.High, intent.Priority, "Priority should be updated to High");
            Assert.AreEqual(1, intent.IsValid, "Intent should be valid");

            // Verify queued intent is removed from buffer
            var updatedQueueBuffer = EntityManager.GetBuffer<QueuedIntent>(entity);
            Assert.AreEqual(0, updatedQueueBuffer.Length, "Queued intent should be removed from buffer after promotion");
        }

        [Test]
        public void IntentProcessingSystem_QueuedIntent_MultipleQueued_FIFO()
        {
            // Create entity with EntityIntent and QueuedIntent buffer
            var entity = EntityManager.CreateEntity(
                typeof(EntityIntent),
                typeof(LocalTransform));

            EntityManager.SetComponentData(entity, LocalTransform.Identity);
            
            // Set current intent to Idle
            EntityManager.SetComponentData(entity, new EntityIntent
            {
                Mode = IntentMode.Idle,
                IsValid = 0,
                IntentSetTick = 1,
                Priority = InterruptPriority.Normal
            });

            // Add QueuedIntent buffer
            var queueBuffer = EntityManager.AddBuffer<QueuedIntent>(entity);
            var target1 = EntityManager.CreateEntity(typeof(LocalTransform));
            var target2 = EntityManager.CreateEntity(typeof(LocalTransform));
            EntityManager.SetComponentData(target1, LocalTransform.Identity);
            EntityManager.SetComponentData(target2, LocalTransform.Identity);

            // Add multiple queued intents (different priorities)
            // Current implementation uses FIFO queue. Priority-based promotion may be added in future if needed.
            queueBuffer.Add(new QueuedIntent
            {
                Mode = IntentMode.Gather,
                TargetEntity = target1,
                TargetPosition = float3.zero,
                Priority = InterruptPriority.Normal,
                TriggeringInterrupt = InterruptType.ResourceSpotted,
                RequestedTick = 1
            });

            queueBuffer.Add(new QueuedIntent
            {
                Mode = IntentMode.Flee,
                TargetEntity = Entity.Null,
                TargetPosition = float3.zero,
                Priority = InterruptPriority.High,
                TriggeringInterrupt = InterruptType.LowHealth,
                RequestedTick = 2
            });

            queueBuffer.Add(new QueuedIntent
            {
                Mode = IntentMode.MoveTo,
                TargetEntity = target2,
                TargetPosition = new float3(10f, 0f, 10f),
                Priority = InterruptPriority.Low,
                TriggeringInterrupt = InterruptType.ObjectiveSpotted,
                RequestedTick = 3
            });

            // Run IntentProcessingSystem
            var processingSystem = _world.GetOrCreateSystem<IntentProcessingSystem>();
            processingSystem.Update(_world.Unmanaged);

            // Verify FIFO promotion: first queued intent (index 0) is always promoted, regardless of priority
            var intent = EntityManager.GetComponentData<EntityIntent>(entity);
            Assert.AreEqual(IntentMode.Gather, intent.Mode, "First queued intent should be promoted (FIFO behavior)");
            Assert.AreEqual(1, intent.IsValid, "Intent should be valid");

            // Verify first queued intent is removed, others remain
            var updatedQueueBuffer = EntityManager.GetBuffer<QueuedIntent>(entity);
            Assert.AreEqual(2, updatedQueueBuffer.Length, "Two queued intents should remain");
            Assert.AreEqual(IntentMode.Flee, updatedQueueBuffer[0].Mode, "Second queued intent should be first now");
            Assert.AreEqual(IntentMode.MoveTo, updatedQueueBuffer[1].Mode, "Third queued intent should be second now");
        }

        [Test]
        public void IntentProcessingSystem_QueuedIntent_PriorityBasedPromotion_NotImplemented()
        {
            // Document that priority-based promotion is NOT currently implemented
            // Current implementation uses FIFO queue (first queued intent is always promoted)
            // If priority-based promotion is desired, this test should be updated and implementation changed

            // Create entity with EntityIntent (Idle) and QueuedIntent buffer
            var entity = EntityManager.CreateEntity(
                typeof(EntityIntent),
                typeof(LocalTransform));

            EntityManager.SetComponentData(entity, LocalTransform.Identity);
            
            EntityManager.SetComponentData(entity, new EntityIntent
            {
                Mode = IntentMode.Idle,
                IsValid = 0,
                IntentSetTick = 1,
                Priority = InterruptPriority.Normal
            });

            // Add QueuedIntent buffer
            var queueBuffer = EntityManager.AddBuffer<QueuedIntent>(entity);
            var target1 = EntityManager.CreateEntity(typeof(LocalTransform));
            var target2 = EntityManager.CreateEntity(typeof(LocalTransform));
            EntityManager.SetComponentData(target1, LocalTransform.Identity);
            EntityManager.SetComponentData(target2, LocalTransform.Identity);

            // Add queued intents: Low priority first, High priority second
            queueBuffer.Add(new QueuedIntent
            {
                Mode = IntentMode.Gather,
                TargetEntity = target1,
                TargetPosition = float3.zero,
                Priority = InterruptPriority.Low,
                TriggeringInterrupt = InterruptType.ResourceSpotted,
                RequestedTick = 1
            });

            queueBuffer.Add(new QueuedIntent
            {
                Mode = IntentMode.Flee,
                TargetEntity = Entity.Null,
                TargetPosition = float3.zero,
                Priority = InterruptPriority.High,
                TriggeringInterrupt = InterruptType.LowHealth,
                RequestedTick = 2
            });

            // Run IntentProcessingSystem
            var processingSystem = _world.GetOrCreateSystem<IntentProcessingSystem>();
            processingSystem.Update(_world.Unmanaged);

            // Verify Low priority intent is promoted (FIFO, not priority-based)
            // If priority-based promotion were implemented, High priority intent would be promoted instead
            var intent = EntityManager.GetComponentData<EntityIntent>(entity);
            Assert.AreEqual(IntentMode.Gather, intent.Mode, "Low priority intent should be promoted (FIFO behavior, not priority-based)");
            Assert.AreEqual(InterruptPriority.Low, intent.Priority, "Priority should match promoted intent");
        }

        #endregion

        #region Suite 4: Integration & Edge Cases

        [Test]
        public void IntentBridge_IntentValidation_Integration()
        {
            // Create entity with expired intent (age > maxIntentAgeTicks)
            var entity = EntityManager.CreateEntity(
                typeof(EntityIntent),
                typeof(LocalTransform));

            var timeEntity = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<TimeState>()).GetSingletonEntity();
            var timeState = EntityManager.GetComponentData<TimeState>(timeEntity);

            EntityManager.SetComponentData(entity, LocalTransform.Identity);
            EntityManager.SetComponentData(entity, new EntityIntent
            {
                Mode = IntentMode.Gather,
                IsValid = 1,
                IntentSetTick = timeState.Tick - 700, // Older than default 600 tick max age
                Priority = InterruptPriority.Normal
            });

            // Run IntentValidationSystem
            var validationSystem = _world.GetOrCreateSystem<IntentValidationSystem>();
            validationSystem.Update(_world.Unmanaged);

            // Verify intent cleared
            var intent = EntityManager.GetComponentData<EntityIntent>(entity);
            Assert.AreEqual(0, intent.IsValid, "Expired intent should be cleared by validation system");

            // Run bridge system (should handle cleared intent gracefully)
            // Note: Bridge systems are game-specific, so we test validation integration here
            Assert.DoesNotThrow(() => validationSystem.Update(_world.Unmanaged),
                "Bridge systems should handle cleared intents gracefully");
        }

        [Test]
        public void IntentBridge_100Entities_Performance()
        {
            // Create 100 entities with EntityIntent components
            const int entityCount = 100;
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

            // Verify performance is acceptable (< 1ms for 100 entities, with 2x safety margin for CI variance)
            // Note: 0.1ms target is too strict for Stopwatch precision (~1ms), so using 1ms as reasonable approximation
            Assert.Less(stopwatch.ElapsedMilliseconds, 1, 
                $"IntentValidationSystem should process {entityCount} entities in < 1ms (actual: {stopwatch.ElapsedMilliseconds}ms)");

            entities.Dispose();
        }

        [Test]
        public void IntentBridge_1000Entities_Performance()
        {
            // Create 1000 entities with EntityIntent components
            const int entityCount = 1000;
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

            // Verify performance is acceptable (< 2ms for 1000 entities, with 2x safety margin for CI variance)
            Assert.Less(stopwatch.ElapsedMilliseconds, 2, 
                $"IntentValidationSystem should process {entityCount} entities in < 2ms (actual: {stopwatch.ElapsedMilliseconds}ms)");

            entities.Dispose();
        }

        [Test]
        public void IntentBridge_10000Entities_Performance()
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

            // Verify performance is acceptable (< 20ms for 10000 entities, with 2x safety margin for CI variance)
            Assert.Less(stopwatch.ElapsedMilliseconds, 20, 
                $"IntentValidationSystem should process {entityCount} entities in < 20ms (actual: {stopwatch.ElapsedMilliseconds}ms)");

            entities.Dispose();
        }

        [Test]
        public void IntentBridge_Systems_BurstCompiled()
        {
            // Verify intent systems are marked with [BurstCompile]
            // Note: Actual Burst compilation validation requires Burst Inspector or runtime checks
            // This test documents the expected Burst compilation status

            // IntentValidationSystem should be Burst-compiled
            var validationSystemType = typeof(IntentValidationSystem);
            var burstCompileAttributes = validationSystemType.GetCustomAttributes(
                typeof(Unity.Burst.BurstCompileAttribute), false);
            Assert.Greater(burstCompileAttributes.Length, 0, 
                "IntentValidationSystem should be marked with [BurstCompile]");

            // IntentProcessingSystem should be Burst-compiled
            var processingSystemType = typeof(IntentProcessingSystem);
            var processingBurstAttributes = processingSystemType.GetCustomAttributes(
                typeof(Unity.Burst.BurstCompileAttribute), false);
            Assert.Greater(processingBurstAttributes.Length, 0, 
                "IntentProcessingSystem should be marked with [BurstCompile]");

            // EnhancedInterruptHandlerSystem should be Burst-compiled
            var enhancedInterruptSystemType = typeof(EnhancedInterruptHandlerSystem);
            var enhancedBurstAttributes = enhancedInterruptSystemType.GetCustomAttributes(
                typeof(Unity.Burst.BurstCompileAttribute), false);
            Assert.Greater(enhancedBurstAttributes.Length, 0, 
                "EnhancedInterruptHandlerSystem should be marked with [BurstCompile]");
        }

        #endregion

        #region Helper Methods

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

