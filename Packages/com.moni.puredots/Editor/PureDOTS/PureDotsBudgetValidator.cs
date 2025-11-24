#if UNITY_EDITOR
using System;
using System.Diagnostics;
using PureDOTS.Runtime.Components;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Assertions;

namespace PureDOTS.Editor
{
    /// <summary>
    /// Editor-only budget validator for performance and memory constraints.
    /// </summary>
    public static class PureDotsBudgetValidator
    {
        public struct BudgetResults
        {
            public float FixedTickMs;
            public float MemoryMB;
            public int SnapshotRingSize;
            public int SpawnsPerFrame;
            public bool AllBudgetsMet;
        }

        public const float FixedTickBudgetMs = 16.6f; // 60fps target
        public const int SnapshotRingBudget = 1000; // Configurable limit
        public const int PresentationSpawnCap = 100; // Spawns per frame cap

        /// <summary>
        /// Validates all budgets and returns results.
        /// </summary>
        public static BudgetResults ValidateBudgets(World world)
        {
            var results = new BudgetResults
            {
                AllBudgetsMet = true
            };

            // Budget 1: FixedTick duration
            if (TryGetSingleton(world, out TimeState timeState))
            {
                results.FixedTickMs = timeState.FixedDeltaTime * 1000f; // Convert to milliseconds
                
                if (results.FixedTickMs >= FixedTickBudgetMs)
                {
                    results.AllBudgetsMet = false;
                    UnityEngine.Debug.LogWarning(
                        $"FixedTick budget exceeded: {results.FixedTickMs:F2}ms >= {FixedTickBudgetMs:F2}ms");
                }
            }

            // Budget 2: Snapshot ring size
            if (TryGetSingleton(world, out HistorySettings historySettings))
            {
                // Calculate ring size from horizon and stride
                // Ring size ≈ horizon / stride (simplified calculation)
                float ringSize = historySettings.DefaultHorizonSeconds / historySettings.DefaultStrideSeconds;
                results.SnapshotRingSize = Mathf.CeilToInt(ringSize);
                
                if (results.SnapshotRingSize > SnapshotRingBudget)
                {
                    results.AllBudgetsMet = false;
                    UnityEngine.Debug.LogWarning(
                        $"Snapshot ring budget exceeded: {results.SnapshotRingSize} > {SnapshotRingBudget}");
                }
            }

            // Budget 3: Presentation spawn count
            if (TryGetSingleton(world, out PresentationPoolStats poolStats))
            {
                results.SpawnsPerFrame = (int)poolStats.SpawnedThisFrame;
                
                if (results.SpawnsPerFrame > PresentationSpawnCap)
                {
                    results.AllBudgetsMet = false;
                    UnityEngine.Debug.LogWarning(
                        $"Presentation spawn cap exceeded: {results.SpawnsPerFrame} > {PresentationSpawnCap}");
                }
            }

            // Memory measurement (GC allocations)
            long memoryBefore = GC.GetTotalMemory(false);
            GC.Collect();
            long memoryAfter = GC.GetTotalMemory(true);
            results.MemoryMB = (memoryAfter - memoryBefore) / (1024f * 1024f);

            return results;
        }

        /// <summary>
        /// Asserts budgets are met, throwing AssertionException if any budget is exceeded.
        /// </summary>
        public static void AssertBudgetsMet(World world, string context = "")
        {
            var results = ValidateBudgets(world);
            
            if (!results.AllBudgetsMet)
            {
                string message = string.IsNullOrEmpty(context) 
                    ? "Budget validation failed" 
                    : $"Budget validation failed in {context}";
                
                message += $"\n  FixedTick: {results.FixedTickMs:F2}ms (budget: {FixedTickBudgetMs:F2}ms)";
                message += $"\n  SnapshotRing: {results.SnapshotRingSize} (budget: {SnapshotRingBudget})";
                message += $"\n  SpawnsPerFrame: {results.SpawnsPerFrame} (budget: {PresentationSpawnCap})";
                message += $"\n  Memory: {results.MemoryMB:F2}MB";
                
                throw new AssertionException(message, string.Empty);
            }
        }

        private static bool TryGetSingleton<T>(World world, out T component) where T : unmanaged, IComponentData
        {
            var query = world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<T>());
            if (query.IsEmptyIgnoreFilter)
            {
                component = default;
                return false;
            }

            component = query.GetSingleton<T>();
            return true;
        }
    }
}
#endif

