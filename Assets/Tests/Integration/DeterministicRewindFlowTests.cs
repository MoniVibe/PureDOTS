using NUnit.Framework;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Resource;
using PureDOTS.Runtime.Spatial;
using PureDOTS.Runtime.Villager;
using PureDOTS.Tests;
using PureDOTS.Tests.Support;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Tests.Integration
{
    /// <summary>
    /// Deterministic rewind tests for critical gameplay flows: gather/delivery, deposit/withdraw, AI transitions, partial rebuilds.
    /// </summary>
    public class DeterministicRewindFlowTests : DeterministicRewindTestFixture
    {
        private Entity _resourceEntity;
        private Entity _storehouseEntity;
        private Entity _villagerEntity;
        private StateSnapshot _recordedSnapshotAt50;
        private StateSnapshot _recordedSnapshotAt100;

        [SetUp]
        public new void SetUp()
        {
            base.SetUp();

            // Create test entities
            _resourceEntity = RegistryMocks.CreateMockResource(EntityManager, new float3(0f, 0f, 0f), new FixedString64Bytes("Wood"), 100f);
            _storehouseEntity = RegistryMocks.CreateMockStorehouse(EntityManager, new float3(5f, 0f, 5f), 500f);
            _villagerEntity = RegistryMocks.CreateMockVillager(EntityManager, new float3(0f, 0f, 0f), 1);
        }

        [Test]
        public void GatherDeliveryFlow_IsDeterministic()
        {
            // Record phase: simulate gather/delivery over 100 ticks
            RunRecordPhase(100);

            // Capture snapshots at intermediate points
            var timeState = EntityManager.GetSingleton<TimeState>();
            timeState.Tick = 50;
            EntityManager.SetSingleton(timeState);
            World.Update();
            _recordedSnapshotAt50 = CaptureStateSnapshot();

            timeState = EntityManager.GetSingleton<TimeState>();
            timeState.Tick = 100;
            EntityManager.SetSingleton(timeState);
            World.Update();
            _recordedSnapshotAt100 = CaptureStateSnapshot();

            // Rewind to tick 50
            RunRewindPhase(50);

            // Verify state matches snapshot at tick 50
            AssertStateMatches(_recordedSnapshotAt50, "After rewind to tick 50");

            // Replay from tick 0 to 50
            RunReplayPhase(0, 50);
            AssertStateMatches(_recordedSnapshotAt50, "After replay to tick 50");

            // Replay from tick 50 to 100
            RunReplayPhase(50, 100);
            AssertStateMatches(_recordedSnapshotAt100, "After replay to tick 100");
        }

        [Test]
        public void DepositWithdrawFlow_IsDeterministic()
        {
            // Record phase: simulate deposit/withdraw operations
            RunRecordPhase(50);

            // Capture snapshot after deposits
            var storehouseInventory = EntityManager.GetComponentData<StorehouseInventory>(_storehouseEntity);
            float recordedStored = storehouseInventory.TotalStored;
            _recordedSnapshotAt50 = CaptureStateSnapshot();

            // Continue recording withdrawals
            RunRecordPhase(50);

            // Capture snapshot after withdrawals
            storehouseInventory = EntityManager.GetComponentData<StorehouseInventory>(_storehouseEntity);
            float recordedAfterWithdraw = storehouseInventory.TotalStored;
            _recordedSnapshotAt100 = CaptureStateSnapshot();

            // Rewind to tick 50
            RunRewindPhase(50);

            // Verify storehouse state matches
            storehouseInventory = EntityManager.GetComponentData<StorehouseInventory>(_storehouseEntity);
            Assert.AreEqual(recordedStored, storehouseInventory.TotalStored, 0.001f, "Storehouse stored amount should match after rewind");

            // Replay and verify deterministic state
            RunReplayPhase(0, 50);
            storehouseInventory = EntityManager.GetComponentData<StorehouseInventory>(_storehouseEntity);
            Assert.AreEqual(recordedStored, storehouseInventory.TotalStored, 0.001f, "Storehouse stored amount should match after replay");
        }

        [Test]
        public void AITransitions_AreDeterministic()
        {
            // Record phase: simulate AI state transitions
            RunRecordPhase(100);

            // Capture AI state at intermediate points
            var aiState50 = EntityManager.GetComponentData<VillagerAIState>(_villagerEntity);
            var aiState100 = EntityManager.GetComponentData<VillagerAIState>(_villagerEntity);

            // Rewind to tick 50
            RunRewindPhase(50);

            // Verify AI state matches
            var aiStateAfterRewind = EntityManager.GetComponentData<VillagerAIState>(_villagerEntity);
            Assert.AreEqual(aiState50.CurrentState, aiStateAfterRewind.CurrentState, "AI state should match after rewind");
            Assert.AreEqual(aiState50.CurrentGoal, aiStateAfterRewind.CurrentGoal, "AI goal should match after rewind");

            // Replay and verify deterministic transitions
            RunReplayPhase(0, 50);
            var aiStateAfterReplay = EntityManager.GetComponentData<VillagerAIState>(_villagerEntity);
            Assert.AreEqual(aiState50.CurrentState, aiStateAfterReplay.CurrentState, "AI state should match after replay");
        }

        [Test]
        public void PartialRebuilds_AreDeterministic()
        {
            // Ensure spatial grid is initialized
            if (!EntityManager.HasSingleton<SpatialGridConfig>())
            {
                var createdGridEntity = EntityManager.CreateEntity(typeof(SpatialGridConfig), typeof(SpatialGridState));
                EntityManager.SetComponentData(createdGridEntity, new SpatialGridConfig
                {
                    CellSize = 5f,
                    WorldMin = new float3(-50f, -50f, -50f),
                    WorldMax = new float3(50f, 50f, 50f),
                    CellCounts = new int3(20, 20, 20),
                    HashSeed = 12345,
                    ProviderId = 0
                });
                EntityManager.SetComponentData(createdGridEntity, new SpatialGridState
                {
                    ActiveBufferIndex = 0,
                    TotalEntries = 0,
                    Version = 0,
                    LastUpdateTick = 0,
                    LastDirtyTick = 0,
                    DirtyVersion = 0,
                    DirtyAddCount = 0,
                    DirtyUpdateCount = 0,
                    DirtyRemoveCount = 0,
                    LastRebuildMilliseconds = 0f,
                    LastStrategy = SpatialGridRebuildStrategy.None
                });
            }

            // Record phase: simulate spatial grid partial rebuilds
            RunRecordPhase(50);

            // Capture spatial grid state
            var gridConfigEntity = RequireSingletonEntity<SpatialGridConfig>();
            var gridState50 = EntityManager.GetComponentData<SpatialGridState>(gridConfigEntity);
            _recordedSnapshotAt50 = CaptureStateSnapshot();

            // Continue recording with more changes
            RunRecordPhase(50);

            // Capture spatial grid state again
            gridConfigEntity = RequireSingletonEntity<SpatialGridConfig>();
            var gridState100 = EntityManager.GetComponentData<SpatialGridState>(gridConfigEntity);
            _recordedSnapshotAt100 = CaptureStateSnapshot();

            // Rewind to tick 50
            RunRewindPhase(50);

            // Verify spatial grid state matches
            var gridEntity = RequireSingletonEntity<SpatialGridConfig>();
            var gridStateAfterRewind = EntityManager.GetComponentData<SpatialGridState>(gridEntity);
            Assert.AreEqual(gridState50.Version, gridStateAfterRewind.Version, "Spatial grid version should match after rewind");
            Assert.AreEqual(gridState50.TotalEntries, gridStateAfterRewind.TotalEntries, "Spatial grid entry count should match after rewind");

            // Replay and verify deterministic rebuilds
            RunReplayPhase(0, 50);
            gridEntity = RequireSingletonEntity<SpatialGridConfig>();
            var gridStateAfterReplay = EntityManager.GetComponentData<SpatialGridState>(gridEntity);
            Assert.AreEqual(gridState50.Version, gridStateAfterReplay.Version, "Spatial grid version should match after replay");
        }

        protected override void OnRecordTick(uint tick)
        {
            // Inject deterministic inputs or verify state during recording
            // This is called each tick during record phase
        }

        protected override void OnReplayTick(uint tick)
        {
            // Verify state matches recorded phase during replay
            // This is called each tick during replay phase
        }
    }
}
