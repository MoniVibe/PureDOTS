using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.Threading
{
    /// <summary>
    /// Attribute to declare system dependencies for task graph construction.
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true)]
    public class DependsOnAttribute : System.Attribute
    {
        public Type[] Dependencies { get; }

        public DependsOnAttribute(params Type[] dependencies)
        {
            Dependencies = dependencies;
        }
    }

    /// <summary>
    /// Attribute to declare system outputs for task graph construction.
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true)]
    public class ProducesAttribute : System.Attribute
    {
        public Type[] Components { get; }

        public ProducesAttribute(params Type[] components)
        {
            Components = components;
        }
    }

    /// <summary>
    /// Task graph scheduler that builds a DAG from system dependencies and schedules independent jobs in parallel.
    /// </summary>
    [BurstCompile]
    public struct TaskGraphScheduler
    {
        /// <summary>
        /// Dependency graph: system type index -> list of dependent system type indices.
        /// </summary>
        private NativeHashMap<int, NativeList<int>> _dependencyGraph;

        /// <summary>
        /// Reverse dependency graph: system type index -> list of systems that depend on it.
        /// </summary>
        private NativeHashMap<int, NativeList<int>> _reverseDependencyGraph;

        /// <summary>
        /// In-degree count for each system (number of dependencies).
        /// </summary>
        private NativeHashMap<int, int> _inDegree;

        /// <summary>
        /// System type index -> system type mapping.
        /// </summary>
        private NativeHashMap<int, Type> _typeIndexToType;

        /// <summary>
        /// System type -> type index mapping.
        /// </summary>
        private NativeHashMap<Type, int> _typeToIndex;

        private int _nextTypeIndex;

        public TaskGraphScheduler(Allocator allocator)
        {
            _dependencyGraph = new NativeHashMap<int, NativeList<int>>(64, allocator);
            _reverseDependencyGraph = new NativeHashMap<int, NativeList<int>>(64, allocator);
            _inDegree = new NativeHashMap<int, int>(64, allocator);
            _typeIndexToType = new NativeHashMap<int, Type>(64, allocator);
            _typeToIndex = new NativeHashMap<Type, int>(64, allocator);
            _nextTypeIndex = 0;
        }

        public void Dispose()
        {
            if (_dependencyGraph.IsCreated)
            {
                foreach (var kvp in _dependencyGraph)
                {
                    kvp.Value.Dispose();
                }
                _dependencyGraph.Dispose();
            }

            if (_reverseDependencyGraph.IsCreated)
            {
                foreach (var kvp in _reverseDependencyGraph)
                {
                    kvp.Value.Dispose();
                }
                _reverseDependencyGraph.Dispose();
            }

            if (_inDegree.IsCreated)
            {
                _inDegree.Dispose();
            }

            if (_typeIndexToType.IsCreated)
            {
                _typeIndexToType.Dispose();
            }

            if (_typeToIndex.IsCreated)
            {
                _typeToIndex.Dispose();
            }
        }

        /// <summary>
        /// Registers a system type and its dependencies.
        /// </summary>
        public void RegisterSystem(Type systemType)
        {
            if (!_typeToIndex.TryGetValue(systemType, out int systemIndex))
            {
                systemIndex = _nextTypeIndex++;
                _typeToIndex[systemType] = systemIndex;
                _typeIndexToType[systemIndex] = systemType;
                _dependencyGraph[systemIndex] = new NativeList<int>(4, Allocator.Persistent);
                _reverseDependencyGraph[systemIndex] = new NativeList<int>(4, Allocator.Persistent);
                _inDegree[systemIndex] = 0;
            }

            // Process DependsOn attributes
            var dependsOnAttrs = systemType.GetCustomAttributes(typeof(DependsOnAttribute), false);
            foreach (DependsOnAttribute attr in dependsOnAttrs)
            {
                foreach (var depType in attr.Dependencies)
                {
                    if (!_typeToIndex.TryGetValue(depType, out int depIndex))
                    {
                        depIndex = _nextTypeIndex++;
                        _typeToIndex[depType] = depIndex;
                        _typeIndexToType[depIndex] = depType;
                        _dependencyGraph[depIndex] = new NativeList<int>(4, Allocator.Persistent);
                        _reverseDependencyGraph[depIndex] = new NativeList<int>(4, Allocator.Persistent);
                        _inDegree[depIndex] = 0;
                    }

                    // Add dependency edge: system depends on depType
                    if (!_dependencyGraph[systemIndex].Contains(depIndex))
                    {
                        _dependencyGraph[systemIndex].Add(depIndex);
                        _reverseDependencyGraph[depIndex].Add(systemIndex);
                        _inDegree[systemIndex] = _inDegree[systemIndex] + 1;
                    }
                }
            }
        }

        /// <summary>
        /// Builds dependency graph from all systems in the world.
        /// </summary>
        public void BuildGraph(World world)
        {
            var allSystems = new List<ComponentSystemBase>();
            world.GetAllSystems(allSystems);

            foreach (var system in allSystems)
            {
                RegisterSystem(system.GetType());
            }
        }

        /// <summary>
        /// Gets systems that can run in parallel (no dependencies or all dependencies satisfied).
        /// </summary>
        public NativeList<int> GetReadySystems(NativeHashSet<int> completedSystems)
        {
            var ready = new NativeList<int>(16, Allocator.Temp);

            foreach (var kvp in _inDegree)
            {
                int systemIndex = kvp.Key;
                int inDegree = kvp.Value;

                // Check if all dependencies are completed
                bool allDepsCompleted = true;
                if (_dependencyGraph.TryGetValue(systemIndex, out var deps))
                {
                    foreach (var depIndex in deps)
                    {
                        if (!completedSystems.Contains(depIndex))
                        {
                            allDepsCompleted = false;
                            break;
                        }
                    }
                }

                if (allDepsCompleted && !completedSystems.Contains(systemIndex))
                {
                    ready.Add(systemIndex);
                }
            }

            return ready;
        }

        /// <summary>
        /// Gets the system type for a type index.
        /// </summary>
        public bool TryGetSystemType(int typeIndex, out Type systemType)
        {
            return _typeIndexToType.TryGetValue(typeIndex, out systemType);
        }
    }
}

