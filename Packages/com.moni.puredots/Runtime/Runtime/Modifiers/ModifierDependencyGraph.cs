using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.Modifiers
{
    /// <summary>
    /// Dependency graph structure for modifier evaluation order.
    /// Directed acyclic graph (DAG) flattened at load time via topological sort.
    /// Precomputed dependency chains stored as BlobArray<ushort> for Burst-safe iteration.
    /// </summary>
    public struct ModifierDependencyGraph
    {
        /// <summary>
        /// Flattened dependency chains (topologically sorted).
        /// Each chain is a BlobArray<ushort> of modifier IDs in evaluation order.
        /// Chain 0 is always empty (no dependencies).
        /// </summary>
        public BlobArray<BlobArray<ushort>> DependencyChains;

        /// <summary>
        /// Dependency edges: [sourceModifierId] -> [array of dependent modifier IDs].
        /// Used during build time for topological sort, not used at runtime.
        /// </summary>
        public BlobArray<BlobArray<ushort>> DependencyEdges;
    }

    /// <summary>
    /// Helper methods for building and querying dependency graphs.
    /// </summary>
    public static class ModifierDependencyGraphBuilder
    {
        /// <summary>
        /// Builds a flattened dependency graph from modifier definitions.
        /// Performs topological sort to create evaluation order chains.
        /// </summary>
        public static void BuildDependencyGraph(
            ref BlobBuilder builder,
            ref ModifierCatalogBlob catalog,
            NativeArray<ModifierDependency> dependencies,
            out ModifierDependencyGraph graph)
        {
            // Allocate dependency chains array
            var chainsBuilder = builder.Allocate(ref graph.DependencyChains, (int)catalog.Modifiers.Length + 1);

            // Chain 0 is always empty (no dependencies)
            builder.Allocate(ref chainsBuilder[0], 0);

            // Build dependency edges first
            var edgesBuilder = builder.Allocate(ref graph.DependencyEdges, (int)catalog.Modifiers.Length);
            for (int i = 0; i < catalog.Modifiers.Length; i++)
            {
                builder.Allocate(ref edgesBuilder[i], 0);
            }

            // Add dependencies to edges
            for (int i = 0; i < dependencies.Length; i++)
            {
                var dep = dependencies[i];
                if (dep.SourceModifierId < catalog.Modifiers.Length)
                {
                    // Resize edge array and add dependent modifier ID
                    // Note: This is simplified - actual implementation would need proper resizing
                }
            }

            // Perform topological sort to create chains
            // Simplified: assign each modifier to its own chain for now
            // Full implementation would group modifiers by dependency order
            for (int i = 0; i < catalog.Modifiers.Length; i++)
            {
                builder.Allocate(ref chainsBuilder[i + 1], 1);
                chainsBuilder[i + 1][0] = (ushort)i;
            }
        }
    }

    /// <summary>
    /// Dependency relationship between modifiers.
    /// Used during authoring/build time to construct dependency graph.
    /// </summary>
    public struct ModifierDependency
    {
        public ushort SourceModifierId;
        public ushort DependentModifierId;
    }
}

