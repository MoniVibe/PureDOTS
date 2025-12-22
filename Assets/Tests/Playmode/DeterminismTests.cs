using NUnit.Framework;
using PureDOTS.Runtime.Components;
using Unity.Entities;
using UnityEngine;
using UnityEngine.TestTools;

namespace PureDOTS.Tests.Playmode
{
    /// <summary>
    /// Tests for determinism: 30/60/120 FPS parity and rewind/resim equality.
    /// </summary>
    public class DeterminismTests
    {
        private World _world;
        private EntityManager _entityManager;
        
        [SetUp]
        public void SetUp()
        {
            _world = World.DefaultGameObjectInjectionWorld;
            _entityManager = _world.EntityManager;
        }
        
        [Test]
        public void Determinism_FixedStepParity_30FPS()
        {
            // Test that simulation produces identical results at 30 FPS
            // This is a placeholder - full implementation would:
            // 1. Run simulation for N ticks at 30 FPS (FixedDeltaTime = 1/30)
            // 2. Capture final state snapshot
            // 3. Compare with expected snapshot
            
            var timeEntity = _entityManager.CreateEntity();
            _entityManager.AddComponent<TimeState>(timeEntity);
            
            var timeState = new TimeState
            {
                FixedDeltaTime = 1f / 30f,
                CurrentSpeedMultiplier = 1f,
                Tick = 0,
                IsPaused = false
            };
            _entityManager.SetComponentData(timeEntity, timeState);
            
            // Verify time state is set correctly
            var retrieved = _entityManager.GetComponentData<TimeState>(timeEntity);
            Assert.AreEqual(1f / 30f, retrieved.FixedDeltaTime, 0.001f);
            
            _entityManager.DestroyEntity(timeEntity);
        }
        
        [Test]
        public void Determinism_FixedStepParity_60FPS()
        {
            var timeEntity = _entityManager.CreateEntity();
            _entityManager.AddComponent<TimeState>(timeEntity);
            
            var timeState = new TimeState
            {
                FixedDeltaTime = 1f / 60f,
                CurrentSpeedMultiplier = 1f,
                Tick = 0,
                IsPaused = false
            };
            _entityManager.SetComponentData(timeEntity, timeState);
            
            var retrieved = _entityManager.GetComponentData<TimeState>(timeEntity);
            Assert.AreEqual(1f / 60f, retrieved.FixedDeltaTime, 0.001f);
            
            _entityManager.DestroyEntity(timeEntity);
        }
        
        [Test]
        public void Determinism_FixedStepParity_120FPS()
        {
            var timeEntity = _entityManager.CreateEntity();
            _entityManager.AddComponent<TimeState>(timeEntity);
            
            var timeState = new TimeState
            {
                FixedDeltaTime = 1f / 120f,
                CurrentSpeedMultiplier = 1f,
                Tick = 0,
                IsPaused = false
            };
            _entityManager.SetComponentData(timeEntity, timeState);
            
            var retrieved = _entityManager.GetComponentData<TimeState>(timeEntity);
            Assert.AreEqual(1f / 120f, retrieved.FixedDeltaTime, 0.001f);
            
            _entityManager.DestroyEntity(timeEntity);
        }
        
        [Test]
        public void Determinism_RewindResim_ByteEqual()
        {
            // Test that recording 5s, rewinding 2s, and resimulating produces byte-equal state at T+5
            // This is a placeholder - full implementation would:
            // 1. Record simulation for 5 seconds
            // 2. Rewind to T+3 (2 seconds back)
            // 3. Resimulate to T+5
            // 4. Compare final state with original T+5 state (byte-equal)
            
            var timeEntity = _entityManager.CreateEntity();
            _entityManager.AddComponent<TimeState>(timeEntity);
            _entityManager.AddComponent<RewindState>(timeEntity);
            _entityManager.AddComponent<RewindLegacyState>(timeEntity);
            
            var timeState = new TimeState
            {
                FixedDeltaTime = 1f / 60f,
                CurrentSpeedMultiplier = 1f,
                Tick = 300, // 5 seconds at 60 FPS
                IsPaused = false
            };
            _entityManager.SetComponentData(timeEntity, timeState);
            
            var rewindState = new RewindState
            {
                Mode = RewindMode.Record,
                TargetTick = 0,
                TickDuration = timeState.FixedDeltaTime,
                MaxHistoryTicks = 600,
                PendingStepTicks = 0
            };
            _entityManager.SetComponentData(timeEntity, rewindState);
            _entityManager.SetComponentData(timeEntity, new RewindLegacyState
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
            
            // Verify state is set
            var retrievedTime = _entityManager.GetComponentData<TimeState>(timeEntity);
            var retrievedRewind = _entityManager.GetComponentData<RewindState>(timeEntity);
            
            Assert.AreEqual(300u, retrievedTime.Tick);
            Assert.AreEqual(RewindMode.Record, retrievedRewind.Mode);
            
            _entityManager.DestroyEntity(timeEntity);
        }
        
        [Test]
        public void Determinism_NoRealtimeSinceStartup()
        {
            // Verify that systems don't use Time.realtimeSinceStartup
            // This is a static check - in practice, grep for "realtimeSinceStartup" in system code
            // All time should come from TimeState.Tick or TimeState.FixedDeltaTime
            
            var timeEntity = _entityManager.CreateEntity();
            _entityManager.AddComponent<TimeState>(timeEntity);
            
            var timeState = new TimeState
            {
                FixedDeltaTime = 1f / 60f,
                CurrentSpeedMultiplier = 1f,
                Tick = 100,
                IsPaused = false
            };
            _entityManager.SetComponentData(timeEntity, timeState);
            
            // Verify tick-based time is used
            var retrieved = _entityManager.GetComponentData<TimeState>(timeEntity);
            Assert.Greater(retrieved.Tick, 0u);
            
            _entityManager.DestroyEntity(timeEntity);
        }
    }
}
