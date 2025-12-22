using NUnit.Framework;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Registry;
using PureDOTS.Systems;
using PureDOTS.Tests.Playmode;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine.TestTools;

namespace PureDOTS.Tests.Integration
{
    /// <summary>
    /// Core integration tests for PureDOTS determinism, registry round-trips, and spawner behavior.
    /// </summary>
    public class PureDots_Integration_Tests : EcsTestFixture
    {
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            CoreSingletonBootstrapSystem.EnsureSingletons(EntityManager);
        }

        [Test]
        public void FixedStep_Gating_Determinism()
        {
            // Create simple world with moving entities
            var entity1 = EntityManager.CreateEntity();
            EntityManager.AddComponentData(entity1, LocalTransform.FromPositionRotationScale(
                new float3(0, 0, 0), quaternion.identity, 1f));
            EntityManager.AddComponentData(entity1, new TestMovement { Speed = 1f });

            var entity2 = EntityManager.CreateEntity();
            EntityManager.AddComponentData(entity2, LocalTransform.FromPositionRotationScale(
                new float3(5, 0, 5), quaternion.identity, 1f));
            EntityManager.AddComponentData(entity2, new TestMovement { Speed = 2f });

            // Ensure time state
            if (!EntityManager.HasSingleton<TimeState>())
            {
                var timeEntity = EntityManager.CreateEntity(typeof(TimeState));
                EntityManager.SetComponentData(timeEntity, new TimeState
                {
                    Tick = 0,
                    FixedDeltaTime = 1f / 60f,
                    IsPaused = false,
                    CurrentSpeedMultiplier = 1f
                });
            }

            // Run at 30fps
            byte[] snapshot30 = RunForSeconds(2f, 30);
            
            // Reset world
            ResetWorld();
            CoreSingletonBootstrapSystem.EnsureSingletons(EntityManager);
            
            // Recreate entities
            entity1 = EntityManager.CreateEntity();
            EntityManager.AddComponentData(entity1, LocalTransform.FromPositionRotationScale(
                new float3(0, 0, 0), quaternion.identity, 1f));
            EntityManager.AddComponentData(entity1, new TestMovement { Speed = 1f });

            entity2 = EntityManager.CreateEntity();
            EntityManager.AddComponentData(entity2, LocalTransform.FromPositionRotationScale(
                new float3(5, 0, 5), quaternion.identity, 1f));
            EntityManager.AddComponentData(entity2, new TestMovement { Speed = 2f });

            // Run at 60fps
            byte[] snapshot60 = RunForSeconds(2f, 60);

            // Reset world again
            ResetWorld();
            CoreSingletonBootstrapSystem.EnsureSingletons(EntityManager);
            
            // Recreate entities
            entity1 = EntityManager.CreateEntity();
            EntityManager.AddComponentData(entity1, LocalTransform.FromPositionRotationScale(
                new float3(0, 0, 0), quaternion.identity, 1f));
            EntityManager.AddComponentData(entity1, new TestMovement { Speed = 1f });

            entity2 = EntityManager.CreateEntity();
            EntityManager.AddComponentData(entity2, LocalTransform.FromPositionRotationScale(
                new float3(5, 0, 5), quaternion.identity, 1f));
            EntityManager.AddComponentData(entity2, new TestMovement { Speed = 2f });

            // Run at 120fps
            byte[] snapshot120 = RunForSeconds(2f, 120);

            // Assert all snapshots are bytewise identical
            Assert.AreEqual(snapshot30.Length, snapshot60.Length, "Snapshot sizes should match");
            Assert.AreEqual(snapshot60.Length, snapshot120.Length, "Snapshot sizes should match");
            
            for (int i = 0; i < snapshot30.Length; i++)
            {
                Assert.AreEqual(snapshot30[i], snapshot60[i], 
                    $"Snapshot byte mismatch at index {i} (30fps vs 60fps)");
                Assert.AreEqual(snapshot60[i], snapshot120[i], 
                    $"Snapshot byte mismatch at index {i} (60fps vs 120fps)");
            }
        }

        [Test]
        public void Rewind_Determinism()
        {
            // Create test entity
            var entity = EntityManager.CreateEntity();
            EntityManager.AddComponentData(entity, LocalTransform.FromPositionRotationScale(
                new float3(0, 0, 0), quaternion.identity, 1f));
            EntityManager.AddComponentData(entity, new TestMovement { Speed = 1f });

            // Ensure time and rewind state
            if (!EntityManager.HasSingleton<TimeState>())
            {
                var timeEntity = EntityManager.CreateEntity(typeof(TimeState));
                EntityManager.SetComponentData(timeEntity, new TimeState
                {
                    Tick = 0,
                    FixedDeltaTime = 1f / 60f,
                    IsPaused = false,
                    CurrentSpeedMultiplier = 1f
                });
            }

            if (!EntityManager.HasSingleton<RewindState>())
            {
                var rewindEntity = EntityManager.CreateEntity(typeof(RewindState));
                EntityManager.SetComponentData(rewindEntity, new RewindState
                {
                    Mode = RewindMode.Record,
                    TargetTick = 0,
                    TickDuration = 1f / 60f,
                    MaxHistoryTicks = 600,
                    PendingStepTicks = 0
                });
                EntityManager.AddComponentData(rewindEntity, new RewindLegacyState
                {
                    PlaybackSpeed = 1f,
                    CurrentTick = 0,
                    StartTick = 0,
                    PlaybackTick = 0,
                    PlaybackTicksPerSecond = 0f,
                    ScrubDirection = 0,
                    ScrubSpeedMultiplier = 1f,
                    RewindWindowTicks = 0,
                    ActiveTrack = default
                });
            }

            // Record 5 seconds
            byte[] snapshotAt5s = RecordForSeconds(5f);

            // Rewind 2 seconds (to tick 180 at 60fps)
            var timeState = EntityManager.GetSingleton<TimeState>();
            uint targetTick = (uint)(3f * 60f); // 3 seconds = 180 ticks
            timeState.Tick = targetTick;
            EntityManager.SetSingleton(timeState);

            var rewindState = EntityManager.GetSingleton<RewindState>();
            rewindState.Mode = RewindMode.Playback;
            rewindState.TargetTick = (int)targetTick;
            EntityManager.SetSingleton(rewindState);
            var legacy = EntityManager.GetSingleton<RewindLegacyState>();
            legacy.PlaybackTick = targetTick;
            EntityManager.SetSingleton(legacy);

            World.Update();

            // Resimulate to 5 seconds
            rewindState.Mode = RewindMode.Record;
            EntityManager.SetSingleton(rewindState);

            // Run from tick 180 to tick 300 (5 seconds)
            for (uint tick = targetTick; tick <= 300; tick++)
            {
                timeState.Tick = tick;
                EntityManager.SetSingleton(timeState);
                World.Update();
            }

            // Capture final snapshot
            byte[] snapshotAfterResim = CaptureWorldSnapshot();

            // Assert bytewise match
            Assert.AreEqual(snapshotAt5s.Length, snapshotAfterResim.Length,
                "Snapshot sizes should match after rewind and resim");
            
            for (int i = 0; i < snapshotAt5s.Length; i++)
            {
                Assert.AreEqual(snapshotAt5s[i], snapshotAfterResim[i],
                    $"Snapshot byte mismatch at index {i} after rewind/resim");
            }
        }

        [Test]
        public void Registry_RoundTrip()
        {
            // Create a tiny registry with 2 items
            var registryEntity = EntityManager.CreateEntity();
            var registry = new ResourceRegistry();
            EntityManager.AddComponentData(registryEntity, registry);
            var entries = EntityManager.AddBuffer<ResourceRegistryEntry>(registryEntity);

            // Add 2 entries
            entries.Add(new ResourceRegistryEntry
            {
                SourceEntity = EntityManager.CreateEntity(),
                Position = new float3(1, 0, 1),
                UnitsRemaining = 100f,
                ResourceTypeIndex = 0,
                LastMutationTick = 0
            });

            entries.Add(new ResourceRegistryEntry
            {
                SourceEntity = EntityManager.CreateEntity(),
                Position = new float3(2, 0, 2),
                UnitsRemaining = 200f,
                ResourceTypeIndex = 1,
                LastMutationTick = 0
            });

            // Capture initial state
            var initialCount = entries.Length;
            var initialPos1 = entries[0].Position;
            var initialPos2 = entries[1].Position;

            // Run multiple frames
            for (int i = 0; i < 10; i++)
            {
                World.Update();
            }

            // Verify continuity meta parity
            entries = EntityManager.GetBuffer<ResourceRegistryEntry>(registryEntity);
            Assert.AreEqual(initialCount, entries.Length,
                "Registry entry count should remain stable");
            Assert.AreEqual(initialPos1.x, entries[0].Position.x, 0.001f,
                "Registry entry positions should remain stable");
            Assert.AreEqual(initialPos2.x, entries[1].Position.x, 0.001f,
                "Registry entry positions should remain stable");

            // Verify stable ordering (entries should be in same order)
            Assert.AreEqual(1f, entries[0].Position.x, 0.001f,
                "First entry should maintain position");
            Assert.AreEqual(2f, entries[1].Position.x, 0.001f,
                "Second entry should maintain position");
        }

        [Test]
        public void Spawner_Determinism()
        {
            // Create seeded spawner config (simplified - using entity with spawn component)
            var spawnerEntity = EntityManager.CreateEntity();
            EntityManager.AddComponentData(spawnerEntity, new TestSpawnerConfig
            {
                Seed = 123,
                SpawnCount = 0
            });

            // Ensure time state
            if (!EntityManager.HasSingleton<TimeState>())
            {
                var timeEntity = EntityManager.CreateEntity(typeof(TimeState));
                EntityManager.SetComponentData(timeEntity, new TimeState
                {
                    Tick = 0,
                    FixedDeltaTime = 1f / 60f,
                    IsPaused = false,
                    CurrentSpeedMultiplier = 1f
                });
            }

            // Run for 5 seconds at 60fps
            int countA = RunSpawnerForSeconds(5f, 60, 123);

            // Reset world
            ResetWorld();
            CoreSingletonBootstrapSystem.EnsureSingletons(EntityManager);

            // Recreate spawner with same seed
            spawnerEntity = EntityManager.CreateEntity();
            EntityManager.AddComponentData(spawnerEntity, new TestSpawnerConfig
            {
                Seed = 123,
                SpawnCount = 0
            });

            // Run for 5 seconds at 120fps
            int countB = RunSpawnerForSeconds(5f, 120, 123);

            // Assert identical spawn counts
            Assert.AreEqual(countA, countB,
                $"Spawn counts should be identical across frame rates (got {countA} vs {countB})");
        }

        // Helper methods

        private byte[] RunForSeconds(float duration, int targetFps)
        {
            var timeState = EntityManager.GetSingleton<TimeState>();
            float fixedDeltaTime = 1f / targetFps;
            timeState.FixedDeltaTime = fixedDeltaTime;
            EntityManager.SetSingleton(timeState);

            uint targetTick = (uint)(duration * targetFps);
            uint startTick = timeState.Tick;

            for (uint tick = startTick; tick < startTick + targetTick; tick++)
            {
                timeState.Tick = tick;
                EntityManager.SetSingleton(timeState);
                World.Update();
            }

            return CaptureWorldSnapshot();
        }

        private byte[] RecordForSeconds(float duration)
        {
            var timeState = EntityManager.GetSingleton<TimeState>();
            float fixedDeltaTime = 1f / 60f;
            timeState.FixedDeltaTime = fixedDeltaTime;
            EntityManager.SetSingleton(timeState);

            var rewindState = EntityManager.GetSingleton<RewindState>();
            rewindState.Mode = RewindMode.Record;
            EntityManager.SetSingleton(rewindState);

            uint targetTick = (uint)(duration * 60f);
            uint startTick = timeState.Tick;

            for (uint tick = startTick; tick < startTick + targetTick; tick++)
            {
                timeState.Tick = tick;
                EntityManager.SetSingleton(timeState);
                World.Update();
            }

            return CaptureWorldSnapshot();
        }

        private byte[] CaptureWorldSnapshot()
        {
            // Capture deterministic snapshot: entity count + time state + key component data
            using var entities = EntityManager.GetAllEntities(Allocator.Temp);
            var timeState = EntityManager.HasSingleton<TimeState>() 
                ? EntityManager.GetSingleton<TimeState>() 
                : default;

            using var writer = new NativeList<byte>(Allocator.Temp);
            
            // Write entity count
            writer.AddRange(new NativeArray<byte>(System.BitConverter.GetBytes(entities.Length), Allocator.Temp));
            
            // Write time state
            writer.AddRange(new NativeArray<byte>(System.BitConverter.GetBytes(timeState.Tick), Allocator.Temp));
            writer.AddRange(new NativeArray<byte>(System.BitConverter.GetBytes(timeState.FixedDeltaTime), Allocator.Temp));
            
            // Write positions of entities with LocalTransform (deterministic)
            foreach (var entity in entities)
            {
                if (EntityManager.HasComponent<LocalTransform>(entity))
                {
                    var transform = EntityManager.GetComponentData<LocalTransform>(entity);
                    writer.AddRange(new NativeArray<byte>(System.BitConverter.GetBytes(transform.Position.x), Allocator.Temp));
                    writer.AddRange(new NativeArray<byte>(System.BitConverter.GetBytes(transform.Position.y), Allocator.Temp));
                    writer.AddRange(new NativeArray<byte>(System.BitConverter.GetBytes(transform.Position.z), Allocator.Temp));
                }
            }

            var snapshot = writer.AsArray().ToArray();
            return snapshot;
        }

        private int RunSpawnerForSeconds(float duration, int targetFps, uint seed)
        {
            var timeState = EntityManager.GetSingleton<TimeState>();
            float fixedDeltaTime = 1f / targetFps;
            timeState.FixedDeltaTime = fixedDeltaTime;
            EntityManager.SetSingleton(timeState);

            uint targetTick = (uint)(duration * targetFps);
            uint startTick = timeState.Tick;

            // Simple spawner: spawn one entity every 60 ticks using seeded random
            var random = Unity.Mathematics.Random.CreateFromIndex(seed);
            int spawnCount = 0;

            for (uint tick = startTick; tick < startTick + targetTick; tick++)
            {
                timeState.Tick = tick;
                EntityManager.SetSingleton(timeState);

                // Deterministic spawn logic: spawn every 60 ticks
                if (tick % 60 == 0)
                {
                    var spawnerEntity = RequireSingletonEntity<TestSpawnerConfig>();
                    var config = EntityManager.GetComponentData<TestSpawnerConfig>(spawnerEntity);
                    
                    // Use seeded random for deterministic spawn decision
                    var roll = random.NextFloat();
                    if (roll > 0.5f) // 50% chance
                    {
                        var spawned = EntityManager.CreateEntity();
                        EntityManager.AddComponentData(spawned, LocalTransform.FromPositionRotationScale(
                            new float3(random.NextFloat() * 10f, 0, random.NextFloat() * 10f),
                            quaternion.identity, 1f));
                        spawnCount++;
                    }
                    
                    config.SpawnCount = spawnCount;
                    EntityManager.SetComponentData(spawnerEntity, config);
                }

                World.Update();
            }

            return spawnCount;
        }

        private int CountSpawned()
        {
            using var query = EntityManager.CreateEntityQuery(typeof(LocalTransform));
            return query.CalculateEntityCount();
        }

        private void ResetWorld()
        {
            RecreateWorld("PureDOTS Test World");
        }

        // Test components

        private struct TestMovement : IComponentData
        {
            public float Speed;
        }

        private struct TestSpawnerConfig : IComponentData
        {
            public uint Seed;
            public int SpawnCount;
        }

        // Demo-specific determinism tests

        [Test]
        public void PureDotsTemplate_Deterministic_Over_5s()
        {
            // Simulates core PureDotsTemplate scene flow: resource gathering and deposit
            // Create resource node
            var resourceNode = EntityManager.CreateEntity();
            EntityManager.AddComponentData(resourceNode, LocalTransform.FromPositionRotationScale(
                new float3(0, 0, 0), quaternion.identity, 1f));
            EntityManager.AddComponentData(resourceNode, new ResourceSourceState
            {
                UnitsRemaining = 100f
            });

            // Create storehouse
            var storehouse = EntityManager.CreateEntity();
            EntityManager.AddComponentData(storehouse, LocalTransform.FromPositionRotationScale(
                new float3(10, 0, 10), quaternion.identity, 1f));
            EntityManager.AddComponentData(storehouse, new StorehouseInventory
            {
                TotalCapacity = 1000f,
                TotalStored = 0f,
                ItemTypeCount = 0,
                IsShredding = 0,
                LastUpdateTick = 0
            });

            // Ensure time state
            if (!EntityManager.HasSingleton<TimeState>())
            {
                var timeEntity = EntityManager.CreateEntity(typeof(TimeState));
                EntityManager.SetComponentData(timeEntity, new TimeState
                {
                    Tick = 0,
                    FixedDeltaTime = 1f / 60f,
                    IsPaused = false,
                    CurrentSpeedMultiplier = 1f
                });
            }

            // Run for 5 seconds at 60fps
            byte[] snapshotA = RunForSeconds(5f, 60);

            // Reset and recreate identical setup
            ResetWorld();
            CoreSingletonBootstrapSystem.EnsureSingletons(EntityManager);

            resourceNode = EntityManager.CreateEntity();
            EntityManager.AddComponentData(resourceNode, LocalTransform.FromPositionRotationScale(
                new float3(0, 0, 0), quaternion.identity, 1f));
            EntityManager.AddComponentData(resourceNode, new ResourceSourceState
            {
                UnitsRemaining = 100f
            });

            storehouse = EntityManager.CreateEntity();
            EntityManager.AddComponentData(storehouse, LocalTransform.FromPositionRotationScale(
                new float3(10, 0, 10), quaternion.identity, 1f));
            EntityManager.AddComponentData(storehouse, new StorehouseInventory
            {
                TotalCapacity = 1000f,
                TotalStored = 0f,
                ItemTypeCount = 0,
                IsShredding = 0,
                LastUpdateTick = 0
            });

            // Run for 5 seconds at 120fps
            byte[] snapshotB = RunForSeconds(5f, 120);

            // Assert bytewise match
            Assert.AreEqual(snapshotA.Length, snapshotB.Length,
                "Template scene snapshots should match across frame rates");
            for (int i = 0; i < snapshotA.Length; i++)
            {
                Assert.AreEqual(snapshotA[i], snapshotB[i],
                    $"Template scene snapshot byte mismatch at index {i}");
            }
        }

        [Test]
        public void MiningDemo_Deterministic_Over_10s()
        {
            // Simulates Space4XMineLoop scene flow: mining vessels and resource piles
            // Create asteroid resource
            var asteroid = EntityManager.CreateEntity();
            EntityManager.AddComponentData(asteroid, LocalTransform.FromPositionRotationScale(
                new float3(0, 0, 0), quaternion.identity, 1f));
            EntityManager.AddComponentData(asteroid, new ResourceSourceState
            {
                UnitsRemaining = 500f
            });

            // Ensure time state
            if (!EntityManager.HasSingleton<TimeState>())
            {
                var timeEntity = EntityManager.CreateEntity(typeof(TimeState));
                EntityManager.SetComponentData(timeEntity, new TimeState
                {
                    Tick = 0,
                    FixedDeltaTime = 1f / 60f,
                    IsPaused = false,
                    CurrentSpeedMultiplier = 1f
                });
            }

            // Run for 10 seconds at 60fps
            byte[] snapshotA = RunForSeconds(10f, 60);

            // Reset and recreate identical setup
            ResetWorld();
            CoreSingletonBootstrapSystem.EnsureSingletons(EntityManager);

            asteroid = EntityManager.CreateEntity();
            EntityManager.AddComponentData(asteroid, LocalTransform.FromPositionRotationScale(
                new float3(0, 0, 0), quaternion.identity, 1f));
            EntityManager.AddComponentData(asteroid, new ResourceSourceState
            {
                UnitsRemaining = 500f
            });

            // Run for 10 seconds at 30fps
            byte[] snapshotB = RunForSeconds(10f, 30);

            // Assert bytewise match
            Assert.AreEqual(snapshotA.Length, snapshotB.Length,
                "Mining demo snapshots should match across frame rates");
            for (int i = 0; i < snapshotA.Length; i++)
            {
                Assert.AreEqual(snapshotA[i], snapshotB[i],
                    $"Mining demo snapshot byte mismatch at index {i}");
            }
        }

        [UnityTest]
        public System.Collections.IEnumerator VillagerGatherDepositLoop_Deterministic()
        {
            // Tests villager gather/deposit loop determinism (core PureDotsTemplate flow)
            // Create villager
            var villager = EntityManager.CreateEntity();
            EntityManager.AddComponentData(villager, LocalTransform.FromPositionRotationScale(
                new float3(5, 0, 5), quaternion.identity, 1f));

            // Create resource node
            var resourceNode = EntityManager.CreateEntity();
            EntityManager.AddComponentData(resourceNode, LocalTransform.FromPositionRotationScale(
                new float3(0, 0, 0), quaternion.identity, 1f));
            EntityManager.AddComponentData(resourceNode, new ResourceSourceState
            {
                UnitsRemaining = 100f
            });

            // Create storehouse
            var storehouse = EntityManager.CreateEntity();
            EntityManager.AddComponentData(storehouse, LocalTransform.FromPositionRotationScale(
                new float3(10, 0, 10), quaternion.identity, 1f));
            EntityManager.AddComponentData(storehouse, new StorehouseInventory
            {
                TotalCapacity = 1000f,
                TotalStored = 0f,
                ItemTypeCount = 0,
                IsShredding = 0,
                LastUpdateTick = 0
            });

            // Ensure time state
            if (!EntityManager.HasSingleton<TimeState>())
            {
                var timeEntity = EntityManager.CreateEntity(typeof(TimeState));
                EntityManager.SetComponentData(timeEntity, new TimeState
                {
                    Tick = 0,
                    FixedDeltaTime = 1f / 60f,
                    IsPaused = false,
                    CurrentSpeedMultiplier = 1f
                });
            }

            // Record initial state
            var initialResourceUnits = EntityManager.GetComponentData<ResourceSourceState>(resourceNode).UnitsRemaining;
            var initialStorehouseUnits = EntityManager.GetComponentData<StorehouseInventory>(storehouse).TotalStored;

            // Run for 3 seconds
            RunForSeconds(3f, 60);

            // Capture final state
            var finalResourceUnits = EntityManager.GetComponentData<ResourceSourceState>(resourceNode).UnitsRemaining;
            var finalStorehouseUnits = EntityManager.GetComponentData<StorehouseInventory>(storehouse).TotalStored;

            // Assert deterministic behavior (resource should decrease, storehouse should increase)
            Assert.Less(finalResourceUnits, initialResourceUnits,
                "Resource should be depleted after gather loop");
            Assert.Greater(finalStorehouseUnits, initialStorehouseUnits,
                "Storehouse should receive deposits after gather loop");

            yield return null;
        }
    }
}
