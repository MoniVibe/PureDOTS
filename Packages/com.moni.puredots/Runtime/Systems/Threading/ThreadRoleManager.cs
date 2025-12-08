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
        /// Maps system group type indices to their thread roles.
        /// </summary>
        public static NativeHashMap<int, ThreadRoleType> GetRoleMap()
        {
            var map = new NativeHashMap<int, ThreadRoleType>(16, Allocator.Temp);

            TryAddRole(map, TypeManager.GetSystemTypeIndex<Unity.Entities.SimulationSystemGroup>().Index, ThreadRoleType.MainOrchestrator);
            TryAddRole(map, TypeManager.GetSystemTypeIndex<Unity.Physics.Systems.PhysicsSystemGroup>().Index, ThreadRoleType.Physics);

            TryAddRole(map, TypeManager.GetSystemTypeIndex<PureDOTS.Systems.GameplaySystemGroup>().Index, ThreadRoleType.Logic);
            TryAddRole(map, TypeManager.GetSystemTypeIndex<PureDOTS.Systems.AISystemGroup>().Index, ThreadRoleType.Logic);
            TryAddRole(map, TypeManager.GetSystemTypeIndex<PureDOTS.Systems.VillagerSystemGroup>().Index, ThreadRoleType.Logic);
            TryAddRole(map, TypeManager.GetSystemTypeIndex<PureDOTS.Systems.ResourceSystemGroup>().Index, ThreadRoleType.Logic);
            TryAddRole(map, TypeManager.GetSystemTypeIndex<PureDOTS.Systems.EnvironmentSystemGroup>().Index, ThreadRoleType.Logic);
            TryAddRole(map, TypeManager.GetSystemTypeIndex<PureDOTS.Systems.SpatialSystemGroup>().Index, ThreadRoleType.Logic);

            TryAddRole(map, TypeManager.GetSystemTypeIndex<Unity.Entities.PresentationSystemGroup>().Index, ThreadRoleType.RenderingIO);
            TryAddRole(map, TypeManager.GetSystemTypeIndex<PureDOTS.Systems.PureDotsPresentationSystemGroup>().Index, ThreadRoleType.RenderingIO);

            return map;
        }

        /// <summary>
        /// Gets the thread role for a system group type, checking attributes first.
        /// </summary>
        public static bool TryGetRole(System.Type systemGroupType, out ThreadRoleType role)
        {
            var attributes = systemGroupType.GetCustomAttributes(typeof(ThreadRoleAttribute), false);
            if (attributes.Length > 0)
            {
                var attr = (ThreadRoleAttribute)attributes[0];
                role = attr.Role;
                return true;
            }

            var typeIndex = TypeManager.GetSystemTypeIndex(systemGroupType).Index;
            using var map = GetRoleMap();
            return map.TryGetValue(typeIndex, out role);
        }

        private static void TryAddRole(NativeHashMap<int, ThreadRoleType> map, int typeIndex, ThreadRoleType role)
        {
            if (typeIndex >= 0)
            {
                map.TryAdd(typeIndex, role);
            }
        }
    }
}

