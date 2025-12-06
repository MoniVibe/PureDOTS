using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Mathematics.Geometry;

namespace PureDOTS.Runtime.Spatial
{
    /// <summary>
    /// Structure-of-Arrays (SoA) layout for octree nodes, optimized for Burst and SIMD operations.
    /// Stores flattened octree as parallel arrays for cache-efficient traversal.
    /// </summary>
    public struct OctreeSoA
    {
        /// <summary>
        /// Axis-aligned bounding boxes for each node.
        /// </summary>
        public NativeArray<AABB> Bounds;

        /// <summary>
        /// Parent node indices (-1 for root nodes).
        /// </summary>
        public NativeArray<int> Parent;

        /// <summary>
        /// Child node indices (8 per node, -1 for leaf nodes or empty children).
        /// Stored as flat array: children[nodeIndex * 8 + childIndex]
        /// </summary>
        public NativeArray<int> Children;

        /// <summary>
        /// Density values for each node (entityCount / cellVolume).
        /// </summary>
        public NativeArray<float> Density;

        /// <summary>
        /// Hierarchical level for each node (0-3).
        /// </summary>
        public NativeArray<byte> Level;

        /// <summary>
        /// Entity counts per node (hot data, queried per tick).
        /// </summary>
        public NativeArray<int> EntityCounts;

        /// <summary>
        /// Total number of nodes in the octree.
        /// </summary>
        public int NodeCount;

        /// <summary>
        /// Maximum subdivision depth allowed.
        /// </summary>
        public byte MaxDepth;

        /// <summary>
        /// Creates a new OctreeSoA with the specified capacity.
        /// </summary>
        public static OctreeSoA Create(int initialCapacity, byte maxDepth, Allocator allocator)
        {
            var octree = new OctreeSoA
            {
                Bounds = new NativeArray<AABB>(initialCapacity, allocator),
                Parent = new NativeArray<int>(initialCapacity, allocator),
                Children = new NativeArray<int>(initialCapacity * 8, allocator), // 8 children per node
                Density = new NativeArray<float>(initialCapacity, allocator),
                Level = new NativeArray<byte>(initialCapacity, allocator),
                EntityCounts = new NativeArray<int>(initialCapacity, allocator),
                NodeCount = 0,
                MaxDepth = maxDepth
            };

            // Initialize all children to -1 (empty)
            for (int i = 0; i < octree.Children.Length; i++)
            {
                octree.Children[i] = -1;
            }

            return octree;
        }

        /// <summary>
        /// Gets the child index for a node at the specified child slot (0-7).
        /// </summary>
        public readonly int GetChild(int nodeIndex, int childSlot)
        {
            if (nodeIndex < 0 || nodeIndex >= NodeCount || childSlot < 0 || childSlot >= 8)
            {
                return -1;
            }

            return Children[nodeIndex * 8 + childSlot];
        }

        /// <summary>
        /// Sets the child index for a node at the specified child slot.
        /// </summary>
        public void SetChild(int nodeIndex, int childSlot, int childIndex)
        {
            if (nodeIndex < 0 || nodeIndex >= NodeCount || childSlot < 0 || childSlot >= 8)
            {
                return;
            }

            Children[nodeIndex * 8 + childSlot] = childIndex;
        }

        /// <summary>
        /// Checks if a node is a leaf (has no children).
        /// </summary>
        public readonly bool IsLeaf(int nodeIndex)
        {
            if (nodeIndex < 0 || nodeIndex >= NodeCount)
            {
                return true;
            }

            for (int i = 0; i < 8; i++)
            {
                if (GetChild(nodeIndex, i) >= 0)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Gets the depth of a node (distance from root).
        /// </summary>
        public readonly int GetDepth(int nodeIndex)
        {
            if (nodeIndex < 0 || nodeIndex >= NodeCount)
            {
                return -1;
            }

            int depth = 0;
            int current = nodeIndex;
            while (current >= 0 && Parent[current] >= 0)
            {
                depth++;
                current = Parent[current];
                if (depth > MaxDepth) // Safety check
                {
                    return MaxDepth;
                }
            }

            return depth;
        }

        /// <summary>
        /// Adds a new node to the octree and returns its index.
        /// </summary>
        public int AddNode(AABB bounds, int parentIndex, byte level)
        {
            // Resize arrays if needed
            if (NodeCount >= Bounds.Length)
            {
                var newCapacity = Bounds.Length * 2;
                Resize(newCapacity);
            }

            var index = NodeCount++;
            Bounds[index] = bounds;
            Parent[index] = parentIndex;
            Level[index] = level;
            Density[index] = 0f;
            EntityCounts[index] = 0;

            // Initialize children to -1
            for (int i = 0; i < 8; i++)
            {
                SetChild(index, i, -1);
            }

            return index;
        }

        /// <summary>
        /// Subdivides a node into 8 octants.
        /// </summary>
        public void Subdivide(int nodeIndex)
        {
            if (nodeIndex < 0 || nodeIndex >= NodeCount)
            {
                return;
            }

            if (GetDepth(nodeIndex) >= MaxDepth)
            {
                return; // Max depth reached
            }

            var bounds = Bounds[nodeIndex];
            var center = bounds.Center;
            var halfSize = bounds.Size * 0.5f;
            var level = (byte)(Level[nodeIndex] + 1);

            // Create 8 octants
            var octantOffsets = new float3[]
            {
                new float3(-0.5f, -0.5f, -0.5f), // 0: -x, -y, -z
                new float3(0.5f, -0.5f, -0.5f),  // 1: +x, -y, -z
                new float3(-0.5f, 0.5f, -0.5f),  // 2: -x, +y, -z
                new float3(0.5f, 0.5f, -0.5f),   // 3: +x, +y, -z
                new float3(-0.5f, -0.5f, 0.5f),  // 4: -x, -y, +z
                new float3(0.5f, -0.5f, 0.5f),   // 5: +x, -y, +z
                new float3(-0.5f, 0.5f, 0.5f),   // 6: -x, +y, +z
                new float3(0.5f, 0.5f, 0.5f)     // 7: +x, +y, +z
            };

            for (int i = 0; i < 8; i++)
            {
                var octantCenter = center + octantOffsets[i] * halfSize;
                var octantBounds = new AABB
                {
                    Center = octantCenter,
                    Extents = halfSize * 0.5f
                };

                var childIndex = AddNode(octantBounds, nodeIndex, level);
                SetChild(nodeIndex, i, childIndex);
            }
        }

        /// <summary>
        /// Merges a node's children back into the parent (removes subdivision).
        /// </summary>
        public void Merge(int nodeIndex)
        {
            if (nodeIndex < 0 || nodeIndex >= NodeCount)
            {
                return;
            }

            // Remove all children (they will be garbage collected later)
            for (int i = 0; i < 8; i++)
            {
                SetChild(nodeIndex, i, -1);
            }
        }

        /// <summary>
        /// Resizes all arrays to the new capacity.
        /// </summary>
        private void Resize(int newCapacity)
        {
            var oldBounds = Bounds;
            var oldParent = Parent;
            var oldChildren = Children;
            var oldDensity = Density;
            var oldLevel = Level;
            var oldEntityCounts = EntityCounts;

            Bounds = new NativeArray<AABB>(newCapacity, oldBounds.GetAllocator());
            Parent = new NativeArray<int>(newCapacity, oldParent.GetAllocator());
            Children = new NativeArray<int>(newCapacity * 8, oldChildren.GetAllocator());
            Density = new NativeArray<float>(newCapacity, oldDensity.GetAllocator());
            Level = new NativeArray<byte>(newCapacity, oldLevel.GetAllocator());
            EntityCounts = new NativeArray<int>(newCapacity, oldEntityCounts.GetAllocator());

            // Copy old data
            for (int i = 0; i < NodeCount; i++)
            {
                Bounds[i] = oldBounds[i];
                Parent[i] = oldParent[i];
                Density[i] = oldDensity[i];
                Level[i] = oldLevel[i];
                EntityCounts[i] = oldEntityCounts[i];

                for (int j = 0; j < 8; j++)
                {
                    Children[i * 8 + j] = oldChildren[i * 8 + j];
                }
            }

            // Initialize new children slots to -1
            for (int i = NodeCount * 8; i < Children.Length; i++)
            {
                Children[i] = -1;
            }

            // Dispose old arrays
            if (oldBounds.IsCreated) oldBounds.Dispose();
            if (oldParent.IsCreated) oldParent.Dispose();
            if (oldChildren.IsCreated) oldChildren.Dispose();
            if (oldDensity.IsCreated) oldDensity.Dispose();
            if (oldLevel.IsCreated) oldLevel.Dispose();
            if (oldEntityCounts.IsCreated) oldEntityCounts.Dispose();
        }

        /// <summary>
        /// Disposes all native arrays.
        /// </summary>
        public void Dispose()
        {
            if (Bounds.IsCreated) Bounds.Dispose();
            if (Parent.IsCreated) Parent.Dispose();
            if (Children.IsCreated) Children.Dispose();
            if (Density.IsCreated) Density.Dispose();
            if (Level.IsCreated) Level.Dispose();
            if (EntityCounts.IsCreated) EntityCounts.Dispose();

            NodeCount = 0;
        }

        /// <summary>
        /// Checks if the octree has been disposed.
        /// </summary>
        public readonly bool IsCreated => Bounds.IsCreated;
    }
}

