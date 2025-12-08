using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

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
        /// Dependency edges represented as (systemIndex, dependencyIndex).
        /// </summary>
        private NativeList<int2> _dependencyEdges;

        /// <summary>
        /// In-degree count for each system (number of dependencies).
        /// </summary>
        private NativeHashMap<int, int> _inDegree;

        /// <summary>
        /// System type index -> system type mapping.
        /// </summary>
        private Dictionary<int, Type> _typeIndexToType;

        /// <summary>
        /// System type -> type index mapping.
        /// </summary>
        private Dictionary<Type, int> _typeToIndex;

        private int _nextTypeIndex;

        public TaskGraphScheduler(Allocator allocator)
        {
            _dependencyEdges = new NativeList<int2>(64, allocator);
            _inDegree = new NativeHashMap<int, int>(64, allocator);
            _typeIndexToType = new Dictionary<int, Type>(64);
            _typeToIndex = new Dictionary<Type, int>(64);
            _nextTypeIndex = 0;
        }

        public void Dispose()
        {
            if (_dependencyEdges.IsCreated)
            {
                _dependencyEdges.Dispose();
            }

            if (_inDegree.IsCreated)
            {
                _inDegree.Dispose();
            }

            _typeIndexToType?.Clear();
            _typeToIndex?.Clear();
        }

        private bool EdgeExists(int systemIndex, int dependencyIndex)
        {
            for (int i = 0; i < _dependencyEdges.Length; i++)
            {
                var edge = _dependencyEdges[i];
                if (edge.x == systemIndex && edge.y == dependencyIndex)
                {
                    return true;
                }
            }

            return false;
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
                        _inDegree[depIndex] = 0;
                    }

                    if (EdgeExists(systemIndex, depIndex))
                    {
                        continue;
                    }

                    _dependencyEdges.Add(new int2(systemIndex, depIndex));
                    _inDegree[systemIndex] = _inDegree[systemIndex] + 1;
                }
            }
        }

        /// <summary>
        /// Builds dependency graph from all systems in the world.
        /// </summary>
        public void BuildGraph(World world)
        {
            foreach (var system in world.Systems)
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

                // Check if all dependencies are completed
                bool allDepsCompleted = true;
                for (int i = 0; i < _dependencyEdges.Length; i++)
                {
                    var edge = _dependencyEdges[i];
                    if (edge.x != systemIndex)
                    {
                        continue;
                    }

                    if (!completedSystems.Contains(edge.y))
                    {
                        allDepsCompleted = false;
                        break;
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

