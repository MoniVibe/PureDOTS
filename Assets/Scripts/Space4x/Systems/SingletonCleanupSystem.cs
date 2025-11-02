using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Pooling;
using Unity.Entities;
using UnityEngine;

namespace Space4X.Systems
{
    /// <summary>
    /// Cleans up duplicate singleton entities that cause InvalidOperationException.
    /// Multiple entities with singleton components (HistorySettings, PoolingSettings, TimeSettingsConfig)
    /// can be created by authoring components or bootstrap, causing GetSingleton() to fail.
    /// This system ensures only one entity with each singleton component exists.
    /// Runs after CoreSingletonBootstrapSystem to clean up any duplicates created during initialization.
    /// </summary>
    [UpdateInGroup(typeof(PureDOTS.Systems.TimeSystemGroup), OrderFirst = true)]
    [UpdateAfter(typeof(PureDOTS.Systems.CoreSingletonBootstrapSystem))]
    public partial class SingletonCleanupSystem : SystemBase
    {
        protected override void OnCreate()
        {
            // Don't require anything - we'll check if singletons exist and clean duplicates
        }

        protected override void OnUpdate()
        {
            var entityManager = EntityManager;
            bool hadDuplicates = false;

            // Clean up duplicate HistorySettings
            var historyQuery = entityManager.CreateEntityQuery(typeof(HistorySettings));
            var historyCount = historyQuery.CalculateEntityCount();
            if (historyCount > 1)
            {
                var entities = historyQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
                Debug.LogWarning($"[SingletonCleanup] Found {entities.Length} HistorySettings entities (expected 1). Removing duplicates...");
                // Keep the first one, destroy the rest
                for (int i = 1; i < entities.Length; i++)
                {
                    entityManager.DestroyEntity(entities[i]);
                }
                entities.Dispose();
                hadDuplicates = true;
            }
            historyQuery.Dispose();

            // Clean up duplicate PoolingSettings
            var poolingQuery = entityManager.CreateEntityQuery(typeof(PoolingSettings));
            if (poolingQuery.CalculateEntityCount() > 1)
            {
                var entities = poolingQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
                Debug.LogWarning($"[SingletonCleanup] Found {entities.Length} PoolingSettings entities (expected 1). Removing duplicates...");
                // Keep the first one, destroy the rest
                for (int i = 1; i < entities.Length; i++)
                {
                    entityManager.DestroyEntity(entities[i]);
                }
                entities.Dispose();
                hadDuplicates = true;
            }
            poolingQuery.Dispose();

            // Clean up duplicate TimeSettingsConfig
            var timeConfigQuery = entityManager.CreateEntityQuery(typeof(TimeSettingsConfig));
            if (timeConfigQuery.CalculateEntityCount() > 1)
            {
                var entities = timeConfigQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
                Debug.LogWarning($"[SingletonCleanup] Found {entities.Length} TimeSettingsConfig entities (expected 1). Removing duplicates...");
                // Keep the first one, destroy the rest
                for (int i = 1; i < entities.Length; i++)
                {
                    entityManager.DestroyEntity(entities[i]);
                }
                entities.Dispose();
                hadDuplicates = true;
            }
            timeConfigQuery.Dispose();

            // Clean up duplicate PoolingSettingsConfig
            var poolingConfigQuery = entityManager.CreateEntityQuery(typeof(PoolingSettingsConfig));
            if (poolingConfigQuery.CalculateEntityCount() > 1)
            {
                var entities = poolingConfigQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
                Debug.LogWarning($"[SingletonCleanup] Found {entities.Length} PoolingSettingsConfig entities (expected 1). Removing duplicates...");
                // Keep the first one, destroy the rest
                for (int i = 1; i < entities.Length; i++)
                {
                    entityManager.DestroyEntity(entities[i]);
                }
                entities.Dispose();
                hadDuplicates = true;
            }
            poolingConfigQuery.Dispose();

            // Clean up duplicate HistorySettingsConfig
            var historyConfigQuery = entityManager.CreateEntityQuery(typeof(HistorySettingsConfig));
            if (historyConfigQuery.CalculateEntityCount() > 1)
            {
                var entities = historyConfigQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
                Debug.LogWarning($"[SingletonCleanup] Found {entities.Length} HistorySettingsConfig entities (expected 1). Removing duplicates...");
                // Keep the first one, destroy the rest
                for (int i = 1; i < entities.Length; i++)
                {
                    entityManager.DestroyEntity(entities[i]);
                }
                entities.Dispose();
                hadDuplicates = true;
            }
            historyConfigQuery.Dispose();

            if (hadDuplicates)
            {
                Debug.Log("[SingletonCleanup] Cleanup complete. Only one entity per singleton component should remain.");
            }
            // System stays enabled to catch any late-created duplicates
            // Overhead is minimal since queries are efficient and only run when singletons exist
        }
    }
}





