using System.Diagnostics;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Registry;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Diagnostics
{
    /// <summary>
    /// Static validation helpers for diagnostics checks.
    /// </summary>
    public static class DiagnosticChecks
    {
        /// <summary>
        /// Validates that a blob asset reference is not null and is created.
        /// </summary>
        [Conditional("UNITY_ASSERTIONS")]
        public static bool ValidateBlobReference<T>(BlobAssetReference<T> blobRef, out string error) where T : struct
        {
            error = null;
            if (!blobRef.IsCreated)
            {
                error = $"BlobAssetReference<{typeof(T).Name}> is not created (null or disposed)";
                return false;
            }
            return true;
        }

        /// <summary>
        /// Validates that a float value is finite (not NaN or infinity).
        /// </summary>
        [Conditional("UNITY_ASSERTIONS")]
        public static bool ValidateFloat(float value, string fieldName, out string error)
        {
            error = null;
            if (float.IsNaN(value))
            {
                error = $"Field '{fieldName}' is NaN";
                return false;
            }
            if (float.IsInfinity(value))
            {
                error = $"Field '{fieldName}' is Infinity";
                return false;
            }
            return true;
        }

        /// <summary>
        /// Validates that a float3 value is finite.
        /// </summary>
        [Conditional("UNITY_ASSERTIONS")]
        public static bool ValidateFloat3(float3 value, string fieldName, out string error)
        {
            error = null;
            if (!ValidateFloat(value.x, $"{fieldName}.x", out error)) return false;
            if (!ValidateFloat(value.y, $"{fieldName}.y", out error)) return false;
            if (!ValidateFloat(value.z, $"{fieldName}.z", out error)) return false;
            return true;
        }

        /// <summary>
        /// Validates that a registry entry handle is valid (entity exists and has required components).
        /// </summary>
        [Conditional("UNITY_ASSERTIONS")]
        public static bool ValidateRegistryEntry(
            Entity entity,
            EntityManager entityManager,
            RegistryKind expectedKind,
            out string error)
        {
            error = null;
            if (!entityManager.Exists(entity))
            {
                error = $"Registry entry entity {entity} does not exist";
                return false;
            }

            if (!entityManager.HasComponent<RegistryMetadata>(entity))
            {
                error = $"Registry entry entity {entity} missing RegistryMetadata component";
                return false;
            }

            var metadata = entityManager.GetComponentData<RegistryMetadata>(entity);
            if (metadata.Kind != expectedKind && expectedKind != RegistryKind.Unknown)
            {
                error = $"Registry entry entity {entity} has kind {metadata.Kind}, expected {expectedKind}";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Validates that an entity has a valid archetype (no broken component combinations).
        /// </summary>
        [Conditional("UNITY_ASSERTIONS")]
        public static bool ValidateArchetype(Entity entity, EntityManager entityManager, out string error)
        {
            error = null;
            if (!entityManager.Exists(entity))
            {
                error = $"Entity {entity} does not exist";
                return false;
            }

            // Basic validation: entity exists and can be queried
            // More complex validation would check for incompatible component combinations
            // This is a placeholder for future expansion
            return true;
        }
    }
}

