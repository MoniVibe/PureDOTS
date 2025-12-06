using Unity.Entities;
using Unity.Collections;

namespace PureDOTS.Runtime.Threading
{
    /// <summary>
    /// Thread role types for system groups, matching Dyson Sphere Program model.
    /// </summary>
    public enum ThreadRoleType : byte
    {
        MainOrchestrator = 0,  // Scheduling, dependency graph, ECS world stepping (Fixed 60Hz)
        Physics = 1,           // Rigid body, constraints, spatial updates (Parallel, fixed)
        Logic = 2,             // Production lines, power networks, AI (Async, sub-fixed)
        RenderingIO = 3        // Presentation, streaming, autosave (Variable)
    }

    /// <summary>
    /// Attribute to annotate system groups with their thread role.
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = false)]
    public class ThreadRoleAttribute : System.Attribute
    {
        public ThreadRoleType Role { get; }

        public ThreadRoleAttribute(ThreadRoleType role)
        {
            Role = role;
        }
    }

    /// <summary>
    /// Maps system groups to thread roles and manages role assignments.
    /// </summary>
    public static class ThreadRoleManager
    {
        /// <summary>
        /// Maps system group types to their thread roles.
        /// </summary>
        public static NativeHashMap<System.Type, ThreadRoleType> GetRoleMap()
        {
            var map = new NativeHashMap<System.Type, ThreadRoleType>(16, Allocator.Temp);
            
            // Main Orchestrator: SimulationSystemGroup (fixed 60Hz)
            map.TryAdd(typeof(Unity.Entities.SimulationSystemGroup), ThreadRoleType.MainOrchestrator);
            
            // Physics: PhysicsSystemGroup (parallel, fixed)
            map.TryAdd(typeof(Unity.Physics.Systems.PhysicsSystemGroup), ThreadRoleType.Physics);
            
            // Logic: GameplaySystemGroup (async, sub-fixed)
            map.TryAdd(typeof(PureDOTS.Systems.GameplaySystemGroup), ThreadRoleType.Logic);
            map.TryAdd(typeof(PureDOTS.Systems.AISystemGroup), ThreadRoleType.Logic);
            map.TryAdd(typeof(PureDOTS.Systems.VillagerSystemGroup), ThreadRoleType.Logic);
            map.TryAdd(typeof(PureDOTS.Systems.ResourceSystemGroup), ThreadRoleType.Logic);
            map.TryAdd(typeof(PureDOTS.Systems.EnvironmentSystemGroup), ThreadRoleType.Logic);
            map.TryAdd(typeof(PureDOTS.Systems.SpatialSystemGroup), ThreadRoleType.Logic);
            
            // Rendering/IO: PresentationSystemGroup (variable)
            map.TryAdd(typeof(Unity.Entities.PresentationSystemGroup), ThreadRoleType.RenderingIO);
            map.TryAdd(typeof(PureDOTS.Systems.PureDotsPresentationSystemGroup), ThreadRoleType.RenderingIO);
            
            return map;
        }

        /// <summary>
        /// Gets the thread role for a system group type, checking attributes first.
        /// </summary>
        public static bool TryGetRole(System.Type systemGroupType, out ThreadRoleType role)
        {
            // Check for attribute first
            var attributes = systemGroupType.GetCustomAttributes(typeof(ThreadRoleAttribute), false);
            if (attributes.Length > 0)
            {
                var attr = (ThreadRoleAttribute)attributes[0];
                role = attr.Role;
                return true;
            }

            // Fall back to static mapping
            using var map = GetRoleMap();
            return map.TryGetValue(systemGroupType, out role);
        }
    }
}

