#if UNITY_EDITOR
using System.IO;
using NUnit.Framework;
using PureDOTS.Editor;
using PureDOTS.Runtime.Components;
using PureDOTS.Systems;
using Unity.Entities;
using UnityEngine;
using UnityEngine.TestTools;

namespace PureDOTS.Tests.EditMode
{
    /// <summary>
    /// Budget validation tests that export JSON artifacts for CI.
    /// </summary>
    public class PureDots_Budget_Tests
    {
        private World _world = null!;
        private EntityManager _entityManager;
        private string _artifactsDir;

        [SetUp]
        public void SetUp()
        {
            _world = new World("PureDOTS Budget Test World");
            World.DefaultGameObjectInjectionWorld = _world;
            _entityManager = _world.EntityManager;
            CoreSingletonBootstrapSystem.EnsureSingletons(_entityManager);

            // Create artifacts directory
            _artifactsDir = Path.Combine(Application.dataPath, "..", "CI", "TestResults", "Artifacts");
            Directory.CreateDirectory(_artifactsDir);
        }

        [Test]
        public void Budget_Validation_ExportsJSON()
        {
            // Run a few frames to stabilize
            for (int i = 0; i < 10; i++)
            {
                _world.Update();
            }

            // Validate budgets
            var results = PureDotsBudgetValidator.ValidateBudgets(_world);

            // Export JSON artifact
            string json = ExportBudgetResults(results);
            string artifactPath = Path.Combine(_artifactsDir, "budget_results.json");
            File.WriteAllText(artifactPath, json);

            UnityEngine.Debug.Log($"Budget results exported to: {artifactPath}");
            UnityEngine.Debug.Log(json);

            // Assert budgets are met (this will fail CI if exceeded)
            PureDotsBudgetValidator.AssertBudgetsMet(_world, "Baseline scene");
        }

        [Test]
        public void Budget_FixedTick_UnderLimit()
        {
            // Ensure time state
            var timeQuery = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<TimeState>());
            if (timeQuery.IsEmptyIgnoreFilter)
            {
                var timeEntity = _entityManager.CreateEntity(typeof(TimeState));
                _entityManager.SetComponentData(timeEntity, new TimeState
                {
                    Tick = 0,
                    FixedDeltaTime = 1f / 60f,
                    IsPaused = false,
                    CurrentSpeedMultiplier = 1f
                });
            }

            // Run a few frames
            for (int i = 0; i < 10; i++)
            {
                _world.Update();
            }

            var results = PureDotsBudgetValidator.ValidateBudgets(_world);
            
            Assert.Less(results.FixedTickMs, PureDotsBudgetValidator.FixedTickBudgetMs,
                $"FixedTick should be under {PureDotsBudgetValidator.FixedTickBudgetMs}ms, got {results.FixedTickMs:F2}ms");
        }

        [Test]
        public void Budget_SnapshotRing_UnderLimit()
        {
            // Ensure history settings
            var historyQuery = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<HistorySettings>());
            if (historyQuery.IsEmptyIgnoreFilter)
            {
                var historyEntity = _entityManager.CreateEntity(typeof(HistorySettings));
                _entityManager.SetComponentData(historyEntity,
                    PureDOTS.Runtime.Components.HistorySettingsDefaults.CreateDefault());
            }

            var results = PureDotsBudgetValidator.ValidateBudgets(_world);
            
            Assert.LessOrEqual(results.SnapshotRingSize, PureDotsBudgetValidator.SnapshotRingBudget,
                $"Snapshot ring should be <= {PureDotsBudgetValidator.SnapshotRingBudget}, got {results.SnapshotRingSize}");
        }

        [Test]
        public void Budget_PresentationSpawn_UnderCap()
        {
            // Bootstrap presentation systems
            _world.GetOrCreateSystem<PresentationBootstrapSystem>().Update(_world.Unmanaged);

            // Ensure presentation pool stats
            var statsQuery = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<PresentationPoolStats>());
            if (statsQuery.IsEmptyIgnoreFilter)
            {
                var poolEntity = _entityManager.CreateEntity(typeof(PresentationPoolStats));
                _entityManager.SetComponentData(poolEntity, new PresentationPoolStats
                {
                    SpawnedThisFrame = 0
                });
            }

            var results = PureDotsBudgetValidator.ValidateBudgets(_world);
            
            Assert.LessOrEqual(results.SpawnsPerFrame, PureDotsBudgetValidator.PresentationSpawnCap,
                $"Presentation spawns per frame should be <= {PureDotsBudgetValidator.PresentationSpawnCap}, got {results.SpawnsPerFrame}");
        }

        private string ExportBudgetResults(PureDotsBudgetValidator.BudgetResults results)
        {
            // Simple JSON export (no external dependencies)
            return $@"{{
  ""fixedTickMs"": {results.FixedTickMs:F2},
  ""memoryMB"": {results.MemoryMB:F2},
  ""snapshotRingSize"": {results.SnapshotRingSize},
  ""spawnsPerFrame"": {results.SpawnsPerFrame},
  ""allBudgetsMet"": {results.AllBudgetsMet.ToString().ToLower()}
}}";
        }

        [TearDown]
        public void TearDown()
        {
            if (World.DefaultGameObjectInjectionWorld == _world)
            {
                World.DefaultGameObjectInjectionWorld = null;
            }

            if (_world.IsCreated)
            {
                _world.Dispose();
            }
        }
    }
}
#endif

