using System.Collections.Generic;
using Unity.Entities;
using Unity.Jobs;

namespace PureDOTS.Runtime.Threading
{
    /// <summary>
    /// Manages thread affinity by domain, partitioning Unity JobWorker threads.
    /// </summary>
    public static class ThreadAffinityManager
    {
        /// <summary>
        /// Domain types for thread partitioning.
        /// </summary>
        public enum ThreadDomain : byte
        {
            Simulation = 0,  // Simulation/Logic: 2-4 threads
            Physics = 1,      // Physics: 2-4 threads
            AsyncIO = 2,     // Async/IO: 1-2 threads
            Background = 3    // Background: 1 thread
        }

        /// <summary>
        /// Thread assignment configuration per domain.
        /// </summary>
        public struct DomainThreadConfig
        {
            public ThreadDomain Domain;
            public int ThreadCount;
            public int Priority; // Lower = higher priority

            public DomainThreadConfig(ThreadDomain domain, int threadCount, int priority)
            {
                Domain = domain;
                ThreadCount = threadCount;
                Priority = priority;
            }
        }

        private static readonly Dictionary<ThreadDomain, List<int>> _domainThreadIds = new();
        private static bool _initialized = false;

        /// <summary>
        /// Initializes thread affinity based on threading config.
        /// </summary>
        public static void Initialize(in ThreadingConfig config)
        {
            if (_initialized)
            {
                return;
            }

            _domainThreadIds.Clear();

            // Note: Unity's JobSystem doesn't expose direct thread affinity control.
            // This is a conceptual mapping that can be used for work distribution.
            // Actual thread assignment happens via JobWorkerSettings in Unity 2023+.

            int currentThreadId = 0;

            // Simulation domain
            var simulationThreads = new List<int>();
            for (int i = 0; i < config.SimulationThreadCount; i++)
            {
                simulationThreads.Add(currentThreadId++);
            }
            _domainThreadIds[ThreadDomain.Simulation] = simulationThreads;

            // Physics domain
            var physicsThreads = new List<int>();
            for (int i = 0; i < config.PhysicsThreadCount; i++)
            {
                physicsThreads.Add(currentThreadId++);
            }
            _domainThreadIds[ThreadDomain.Physics] = physicsThreads;

            // Async/IO domain
            var asyncIOThreads = new List<int>();
            for (int i = 0; i < config.AsyncIOThreadCount; i++)
            {
                asyncIOThreads.Add(currentThreadId++);
            }
            _domainThreadIds[ThreadDomain.AsyncIO] = asyncIOThreads;

            // Background domain
            var backgroundThreads = new List<int>();
            for (int i = 0; i < config.BackgroundThreadCount; i++)
            {
                backgroundThreads.Add(currentThreadId++);
            }
            _domainThreadIds[ThreadDomain.Background] = backgroundThreads;

            _initialized = true;
        }

        /// <summary>
        /// Gets thread IDs for a domain.
        /// </summary>
        public static bool TryGetThreadIds(ThreadDomain domain, out IReadOnlyList<int> threadIds)
        {
            if (_domainThreadIds.TryGetValue(domain, out var ids))
            {
                threadIds = ids;
                return true;
            }

            threadIds = null;
            return false;
        }

        /// <summary>
        /// Gets the domain for a thread ID.
        /// </summary>
        public static bool TryGetDomain(int threadId, out ThreadDomain domain)
        {
            foreach (var kvp in _domainThreadIds)
            {
                if (kvp.Value.Contains(threadId))
                {
                    domain = kvp.Key;
                    return true;
                }
            }

            domain = ThreadDomain.Simulation;
            return false;
        }

        /// <summary>
        /// Resets thread affinity (for testing).
        /// </summary>
        public static void Reset()
        {
            _domainThreadIds.Clear();
            _initialized = false;
        }
    }
}

