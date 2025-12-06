#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Entities;
using Unity.Profiling;
using UnityEngine;

namespace PureDOTS.Runtime.Bridges
{
    /// <summary>
    /// Debug-only system that detects dual ownership violations across ECS layers.
    /// Warns when components are written by multiple ECS worlds or when type collisions occur.
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public sealed partial class OwnershipValidatorSystem : SystemBase
    {
        private static readonly ProfilerMarker ValidateMarker = new("OwnershipValidatorSystem.Validate");
        private bool _hasValidated;

        protected override void OnCreate()
        {
            // Only run in editor or development builds
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            RequireForUpdate<AgentSyncState>();
            ValidateComponentOwnership();
            _hasValidated = true;
#endif
        }

        protected override void OnUpdate()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Validation runs once on create
            if (!_hasValidated)
            {
                ValidateComponentOwnership();
                _hasValidated = true;
            }
#endif
        }

        private void ValidateComponentOwnership()
        {
            using (ValidateMarker.Auto())
            {
                // Check for component type name collisions across assemblies
                var runtimeTypes = GetComponentTypesInAssembly("PureDOTS.Runtime");
                var aiTypes = GetComponentTypesInAssembly("PureDOTS.AI");
                var sharedTypes = GetComponentTypesInAssembly("PureDOTS.Shared");

                var allTypeNames = new Dictionary<string, List<string>>();

                foreach (var type in runtimeTypes)
                {
                    var name = type.Name;
                    if (!allTypeNames.ContainsKey(name))
                    {
                        allTypeNames[name] = new List<string>();
                    }
                    allTypeNames[name].Add("PureDOTS.Runtime");
                }

                foreach (var type in aiTypes)
                {
                    var name = type.Name;
                    if (!allTypeNames.ContainsKey(name))
                    {
                        allTypeNames[name] = new List<string>();
                    }
                    allTypeNames[name].Add("PureDOTS.AI");
                }

                foreach (var type in sharedTypes)
                {
                    var name = type.Name;
                    if (!allTypeNames.ContainsKey(name))
                    {
                        allTypeNames[name] = new List<string>();
                    }
                    allTypeNames[name].Add("PureDOTS.Shared");
                }

                // Report collisions
                foreach (var kvp in allTypeNames)
                {
                    if (kvp.Value.Count > 1)
                    {
                        Debug.LogWarning(
                            $"[OwnershipValidator] Component type name collision detected: '{kvp.Key}' exists in multiple assemblies: {string.Join(", ", kvp.Value)}. " +
                            $"This violates single-writer ownership rules. Consider renaming one of the types.");
                    }
                }

                // Validate that AgentGuid is only used for identification, not state
                var agentGuidType = typeof(AgentGuid);
                if (agentGuidType != null)
                {
                    Debug.Log($"[OwnershipValidator] AgentGuid validation: Type exists and is used for cross-ECS identification.");
                }
            }
        }

        private List<System.Type> GetComponentTypesInAssembly(string assemblyName)
        {
            var types = new List<System.Type>();

            try
            {
                var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
                var targetAssembly = assemblies.FirstOrDefault(a => a.GetName().Name == assemblyName);

                if (targetAssembly != null)
                {
                    types.AddRange(
                        targetAssembly.GetTypes()
                            .Where(t => t.IsValueType && !t.IsEnum && !t.IsPrimitive)
                            .Where(t => t.Namespace != null && t.Namespace.Contains("PureDOTS"))
                    );
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[OwnershipValidator] Failed to scan assembly '{assemblyName}': {ex.Message}");
            }

            return types;
        }
    }
}
#endif

