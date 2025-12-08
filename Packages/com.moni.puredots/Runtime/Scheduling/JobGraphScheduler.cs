using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace PureDOTS.Runtime.Scheduling
{
    /// <summary>
    /// Builds and manages a deterministic job dependency graph.
    /// Only executes jobs that have dirty input components.
    /// </summary>
    public class JobGraphScheduler
    {
        private struct SystemNode
        {
            public SystemHandle Handle;
            public Type SystemType;
            public NativeList<ComponentType> ReadComponents;
            public NativeList<ComponentType> WriteComponents;
            public SystemBudget Budget;
            public NativeList<int> Dependencies; // Indices of systems that must run before this
            public NativeList<int> Dependents; // Indices of systems that depend on this
            public bool IsExecuted;
        }

        private List<SystemNode> _nodes;
        private Dictionary<SystemHandle, int> _handleToIndex;
        private DirtyComponentTracker _dirtyTracker;
        private Allocator _allocator;

        public JobGraphScheduler(Allocator allocator)
        {
            _nodes = new List<SystemNode>(64);
            _handleToIndex = new Dictionary<SystemHandle, int>(64);
            _dirtyTracker = new DirtyComponentTracker(allocator);
            _allocator = allocator;
        }

        /// <summary>
        /// Builds the dependency graph from system attributes and component access patterns.
        /// Should be called once after all systems are registered.
        /// </summary>
        public void BuildGraph(World world)
        {
            _nodes.Clear();
            _handleToIndex.Clear();

            // Collect all systems
            var systemList = new List<(SystemHandle Handle, Type Type)>();
            foreach (var system in world.Systems)
            {
                if (system is ComponentSystemBase)
                {
                    var handle = world.GetExistingSystem(system.GetType());
                    if (handle != SystemHandle.Null)
                    {
                        systemList.Add((handle, system.GetType()));
                    }
                }
            }

            // Build nodes
            for (int i = 0; i < systemList.Count; i++)
            {
                var handle = systemList[i].Handle;
                var systemType = systemList[i].Type;
                var system = world.GetExistingSystemManaged(systemType);
                
                if (system == null) continue;

                var node = new SystemNode
                {
                    Handle = handle,
                    SystemType = systemType,
                    ReadComponents = new NativeList<ComponentType>(8, _allocator),
                    WriteComponents = new NativeList<ComponentType>(8, _allocator),
                    Budget = GetSystemBudget(system),
                    Dependencies = new NativeList<int>(4, _allocator),
                    Dependents = new NativeList<int>(4, _allocator),
                    IsExecuted = false
                };
                
                ExtractReadComponents(system, ref node.ReadComponents);
                ExtractWriteComponents(system, ref node.WriteComponents);

                _nodes.Add(node);
                _handleToIndex[handle] = _nodes.Count - 1;
            }

            // Build dependency edges from JobDependency attributes
            BuildDependencyEdges(world);
        }

        /// <summary>
        /// Determines which systems need to run based on dirty components.
        /// Returns a list of system handles in execution order.
        /// </summary>
        public NativeList<SystemHandle> GetExecutionOrder()
        {
            var executionOrder = new NativeList<SystemHandle>(_nodes.Count, _allocator);

            // Reset execution flags
            for (int i = 0; i < _nodes.Count; i++)
            {
                var node = _nodes[i];
                node.IsExecuted = false;
                _nodes[i] = node;
            }

            // Topological sort with dirty component checking
            var inDegree = new NativeArray<int>(_nodes.Count, Allocator.Temp);
            var queue = new NativeQueue<int>(Allocator.Temp);

            // Calculate in-degrees
            for (int i = 0; i < _nodes.Count; i++)
            {
                inDegree[i] = _nodes[i].Dependencies.Length;
                if (inDegree[i] == 0 && NeedsExecution(i))
                {
                    queue.Enqueue(i);
                }
            }

            // Process nodes
            while (queue.TryDequeue(out int current))
            {
                var node = _nodes[current];
                executionOrder.Add(node.Handle);
                node.IsExecuted = true;
                _nodes[current] = node;

                // Update dependents
                for (int depIdx = 0; depIdx < node.Dependents.Length; depIdx++)
                {
                    int dependent = node.Dependents[depIdx];
                    inDegree[dependent]--;
                    if (inDegree[dependent] == 0 && NeedsExecution(dependent))
                    {
                        queue.Enqueue(dependent);
                    }
                }
            }

            inDegree.Dispose();
            queue.Dispose();

            return executionOrder;
        }

        private bool NeedsExecution(int nodeIndex)
        {
            var node = _nodes[nodeIndex];

            // Check if any read components are dirty
            foreach (var component in node.ReadComponents)
            {
                if (_dirtyTracker.IsDirty(component))
                {
                    return true;
                }
            }

            // Check if any write components are dirty (might need to propagate)
            foreach (var component in node.WriteComponents)
            {
                if (_dirtyTracker.IsDirty(component))
                {
                    return true;
                }
            }

            return false;
        }

        private void ExtractReadComponents(ComponentSystemBase system, ref NativeList<ComponentType> components)
        {
            // Simplified: In a real implementation, this would analyze EntityQuery
            // For now, leave empty - would need reflection or query analysis
            components.Clear();
        }

        private void ExtractWriteComponents(ComponentSystemBase system, ref NativeList<ComponentType> components)
        {
            // Simplified: In a real implementation, this would analyze EntityQuery
            // For now, leave empty - would need reflection or query analysis
            components.Clear();
        }

        private SystemBudget GetSystemBudget(ComponentSystemBase system)
        {
            // Try to get budget from component, otherwise use defaults
            // This would require storing budgets in a singleton or component
            return new SystemBudget(1.0f, 128, 16.67f); // Default: 1ms cost, priority 128, max 16.67ms
        }

        private void BuildDependencyEdges(World world)
        {
            // Build edges from JobDependency attributes
            for (int i = 0; i < _nodes.Count; i++)
            {
                var node = _nodes[i];
                var system = world.GetExistingSystemManaged(node.SystemType);
                
                if (system == null) continue;

                var attributes = system.GetType().GetCustomAttributes(typeof(JobDependencyAttribute), true);
                foreach (JobDependencyAttribute attr in attributes)
                {
                    var dependencyHandle = world.GetExistingSystem(attr.DependencyType);
                    if (dependencyHandle == SystemHandle.Null) continue;

                    if (_handleToIndex.TryGetValue(dependencyHandle, out int depIndex))
                    {
                        if (attr.Relationship == DependencyType.After)
                        {
                            // This system runs after dependency
                            node.Dependencies.Add(depIndex);
                            var depNode = _nodes[depIndex];
                            depNode.Dependents.Add(i);
                            _nodes[depIndex] = depNode;
                        }
                        else // Before
                        {
                            // This system runs before dependency
                            node.Dependents.Add(depIndex);
                            var depNode = _nodes[depIndex];
                            depNode.Dependencies.Add(i);
                            _nodes[depIndex] = depNode;
                        }
                    }
                }

                _nodes[i] = node;
            }
        }

        public void MarkDirty(ComponentType componentType)
        {
            _dirtyTracker.MarkDirty(componentType);
        }

        public void ClearDirty()
        {
            _dirtyTracker.Clear();
        }

        public void Dispose()
        {
            for (int i = 0; i < _nodes.Count; i++)
            {
                var node = _nodes[i];
                if (node.ReadComponents.IsCreated)
                    node.ReadComponents.Dispose();
                if (node.WriteComponents.IsCreated)
                    node.WriteComponents.Dispose();
                if (node.Dependencies.IsCreated)
                    node.Dependencies.Dispose();
                if (node.Dependents.IsCreated)
                    node.Dependents.Dispose();
            }
            _nodes.Clear();
            _handleToIndex.Clear();
            _dirtyTracker.Dispose();
        }
    }
}
