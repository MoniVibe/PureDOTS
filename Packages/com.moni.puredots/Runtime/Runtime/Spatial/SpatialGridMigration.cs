using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.Spatial
{
    /// <summary>
    /// Migration helper for converting legacy single-level grids to hierarchical grids.
    /// Preserves existing SpatialGridEntry data during migration.
    /// 
    /// <para>
    /// <b>Usage Example:</b>
    /// <code>
    /// var gridEntity = SystemAPI.GetSingletonEntity&lt;SpatialGridConfig&gt;();
    /// var config = SystemAPI.GetComponent&lt;SpatialGridConfig&gt;(gridEntity);
    /// var entries = SystemAPI.GetBuffer&lt;SpatialGridEntry&gt;(gridEntity);
    /// var ranges = SystemAPI.GetBuffer&lt;SpatialGridCellRange&gt;(gridEntity);
    /// 
    /// if (SpatialGridMigration.NeedsMigration(config))
    /// {
    ///     SpatialGridMigration.MigrateToHierarchical(ref config, ref entries, ref ranges);
    ///     SystemAPI.SetComponent(gridEntity, config);
    /// }
    /// </code>
    /// </para>
    /// 
    /// <para>
    /// <b>Migration Behavior:</b>
    /// - Creates default L0-L3 level configurations
    /// - Preserves all existing entity entries
    /// - Computes CellKey (Morton) for all entries automatically
    /// - Sets default refinement thresholds (100/10 entities per cell)
    /// </para>
    /// 
    /// See also: <see cref="HierarchicalSpatialGridGuide.md"/>
    /// </summary>
    public static class SpatialGridMigration
    {
        /// <summary>
        /// Migrates a legacy single-level grid to a hierarchical grid.
        /// Preserves all existing entity data.
        /// </summary>
        public static void MigrateToHierarchical(
            ref SpatialGridConfig config,
            ref DynamicBuffer<SpatialGridEntry> entries,
            ref DynamicBuffer<SpatialGridCellRange> ranges)
        {
            if (config.IsHierarchical)
            {
                return; // Already hierarchical
            }

            // Create default hierarchical levels
            var levels = new FixedList512Bytes<HierarchicalLevelConfig>();
            
            // L3_Local: Use existing cell size
            levels.Add(new HierarchicalLevelConfig
            {
                Level = HierarchicalGridLevel.L3_Local,
                CellSize = config.CellSize,
                TickRate = 60f, // 60 Hz
                UseAnalyticOrbits = false,
                WorldMin = config.WorldMin,
                WorldMax = config.WorldMax,
                CellCounts = config.CellCounts
            });

            // L2_Planet: 10x larger cells
            levels.Add(new HierarchicalLevelConfig
            {
                Level = HierarchicalGridLevel.L2_Planet,
                CellSize = config.CellSize * 10f,
                TickRate = 1f, // 1 Hz
                UseAnalyticOrbits = false,
                WorldMin = config.WorldMin,
                WorldMax = config.WorldMax,
                CellCounts = config.CellCounts / 10
            });

            // L1_System: 1000x larger cells
            levels.Add(new HierarchicalLevelConfig
            {
                Level = HierarchicalGridLevel.L1_System,
                CellSize = config.CellSize * 1000f,
                TickRate = 0.01f, // 0.01 Hz
                UseAnalyticOrbits = true,
                WorldMin = config.WorldMin,
                WorldMax = config.WorldMax,
                CellCounts = config.CellCounts / 1000
            });

            // L0_Galactic: 100000x larger cells
            levels.Add(new HierarchicalLevelConfig
            {
                Level = HierarchicalGridLevel.L0_Galactic,
                CellSize = config.CellSize * 100000f,
                TickRate = 0.001f, // 0.001 Hz
                UseAnalyticOrbits = true,
                WorldMin = config.WorldMin,
                WorldMax = config.WorldMax,
                CellCounts = config.CellCounts / 100000
            });

            // Update config
            config.IsHierarchical = true;
            config.HierarchicalLevels = levels;
            config.UpperDensityThreshold = 100.0f;
            config.LowerDensityThreshold = 10.0f;
            config.MaxSubdivisionDepth = 4;

            // Migrate entries: compute CellKey for all existing entries
            for (int i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];
                if (entry.CellKey == 0 && entry.CellId >= 0)
                {
                    // Compute Morton key from cell ID
                    SpatialHash.Unflatten(entry.CellId, config, out var coords);
                    entry.CellKey = SpaceFillingCurve.Morton3D(in coords);
                    entries[i] = entry;
                }
            }
        }

        /// <summary>
        /// Checks if a grid needs migration from legacy to hierarchical.
        /// </summary>
        public static bool NeedsMigration(in SpatialGridConfig config)
        {
            return !config.IsHierarchical && config.CellSize > 0f;
        }
    }
}

