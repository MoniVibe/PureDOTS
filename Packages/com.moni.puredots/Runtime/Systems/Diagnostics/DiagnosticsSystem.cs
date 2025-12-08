using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Diagnostics;
using PureDOTS.Runtime.Registry;
using PureDOTS.Runtime.Resource;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace PureDOTS.Systems.Diagnostics
{
    /// <summary>
    /// Runtime diagnostics system that validates data integrity and surfaces errors.
    /// Runs in PresentationSystemGroup after simulation to catch issues before they cascade.
    /// 
    /// See: Docs/Guides/DemoLockSystemsGuide.md#error-handling--diagnostics-layer
    /// API Reference: Docs/Guides/DemoLockSystemsAPI.md#diagnostics-api
    /// </summary>
    [UpdateInGroup(typeof(Unity.Entities.PresentationSystemGroup))]
    public partial struct DiagnosticsSystem : ISystem
    {
        private EntityQuery _blobQuery;
        private EntityQuery _registryQuery;
        private ComponentLookup<RegistryMetadata> _registryMetadataLookup;

        public void OnCreate(ref SystemState state)
        {
            // Create config singleton if it doesn't exist
            if (!SystemAPI.HasSingleton<DiagnosticsConfig>())
            {
                var entity = state.EntityManager.CreateEntity();
                state.EntityManager.AddComponent<DiagnosticsConfig>(entity);
                state.EntityManager.SetComponentData(entity, DiagnosticsConfig.Default);
            }

            // Query for entities with blob references
            _blobQuery = SystemAPI.QueryBuilder()
                .WithAll<ResourceTypeIndex>()
                .Build();

            // Query for registry entities
            _registryQuery = SystemAPI.QueryBuilder()
                .WithAll<RegistryMetadata>()
                .Build();

            _registryMetadataLookup = state.GetComponentLookup<RegistryMetadata>(isReadOnly: true);
        }

        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.HasSingleton<DiagnosticsConfig>() || !SystemAPI.HasSingleton<DebugDisplayData>())
            {
                return;
            }

            var config = SystemAPI.GetSingleton<DiagnosticsConfig>();
            var debugData = SystemAPI.GetSingletonRW<DebugDisplayData>();

            _registryMetadataLookup.Update(ref state);

            var errorCount = 0;
            var archetypeErrors = 0;
            var blobErrors = 0;
            var registryErrors = 0;
            var boundsErrors = 0;

            using var errorMessages = new NativeList<FixedString512Bytes>(Allocator.Temp);

            // Archetype validation
            if (config.EnableArchetypeValidation && archetypeErrors < config.MaxErrorsPerCategory)
            {
                // Basic archetype validation - check for entities that exist
                // More complex validation can be added later
            }

            // Blob reference validation
            if (config.EnableBlobValidation && blobErrors < config.MaxErrorsPerCategory)
            {
                foreach (var (resourceTypeIndex, entity) in SystemAPI.Query<RefRO<ResourceTypeIndex>>().WithEntityAccess())
                {
                    if (errorCount >= config.MaxTotalErrorsPerTick)
                        break;

                    if (!DiagnosticChecks.ValidateBlobReference(resourceTypeIndex.ValueRO.Catalog, out var error))
                    {
                        blobErrors++;
                        errorCount++;
                        if (errorMessages.Length < 20) // Limit message count
                        {
                            var msg = new FixedString512Bytes();
                            msg.Append($"Entity {entity.Index}: {error}");
                            errorMessages.Add(msg);
                        }
                    }
                }
            }

            // Registry entry validation
            if (config.EnableRegistryValidation && registryErrors < config.MaxErrorsPerCategory)
            {
                foreach (var (metadata, entity) in SystemAPI.Query<RefRO<RegistryMetadata>>().WithEntityAccess())
                {
                    if (errorCount >= config.MaxTotalErrorsPerTick)
                        break;

                    if (!DiagnosticChecks.ValidateRegistryEntry(entity, state.EntityManager, metadata.ValueRO.Kind, out var error))
                    {
                        registryErrors++;
                        errorCount++;
                        if (errorMessages.Length < 20)
                        {
                            var msg = new FixedString512Bytes();
                            msg.Append($"Registry {entity.Index}: {error}");
                            errorMessages.Add(msg);
                        }
                    }
                }
            }

            // Component bounds validation (NaN, infinity checks)
            if (config.EnableComponentBoundsValidation && boundsErrors < config.MaxErrorsPerCategory)
            {
                // Check LocalTransform positions
                foreach (var (transform, entity) in SystemAPI.Query<RefRO<Unity.Transforms.LocalTransform>>().WithEntityAccess())
                {
                    if (errorCount >= config.MaxTotalErrorsPerTick)
                        break;

                    if (!DiagnosticChecks.ValidateFloat3(transform.ValueRO.Position, "LocalTransform.Position", out var error))
                    {
                        boundsErrors++;
                        errorCount++;
                        if (errorMessages.Length < 20)
                        {
                            var msg = new FixedString512Bytes();
                            msg.Append($"Entity {entity.Index}: {error}");
                            errorMessages.Add(msg);
                        }
                    }
                }

                // Check VillagerNeeds values
                foreach (var (needs, entity) in SystemAPI.Query<RefRO<VillagerNeeds>>().WithEntityAccess())
                {
                    if (errorCount >= config.MaxTotalErrorsPerTick)
                        break;

                    if (!DiagnosticChecks.ValidateFloat(needs.ValueRO.Health, "VillagerNeeds.Health", out var error))
                    {
                        boundsErrors++;
                        errorCount++;
                        if (errorMessages.Length < 20)
                        {
                            var msg = new FixedString512Bytes();
                            msg.Append($"Villager {entity.Index}: {error}");
                            errorMessages.Add(msg);
                        }
                    }
                }
            }

            // Surface errors to DebugDisplayData
            debugData.ValueRW.DiagnosticsErrorCount = errorCount;
            debugData.ValueRW.DiagnosticsArchetypeErrors = archetypeErrors;
            debugData.ValueRW.DiagnosticsBlobErrors = blobErrors;
            debugData.ValueRW.DiagnosticsRegistryErrors = registryErrors;
            debugData.ValueRW.DiagnosticsBoundsErrors = boundsErrors;

            if (errorCount > 0)
            {
                var alertText = new FixedString512Bytes();
                alertText.Append($"Diagnostics: {errorCount} errors");
                if (blobErrors > 0) alertText.Append($" ({blobErrors} blob)");
                if (registryErrors > 0) alertText.Append($" ({registryErrors} registry)");
                if (boundsErrors > 0) alertText.Append($" ({boundsErrors} bounds)");

                debugData.ValueRW.DiagnosticsAlertText = alertText;

                // Log to console
                if (errorMessages.Length > 0)
                {
                    var firstError = errorMessages[0].ToString();
                    Debug.LogWarning($"[Diagnostics] {alertText.ToString()}\nFirst error: {firstError}");
                }
            }
            else
            {
                debugData.ValueRW.DiagnosticsAlertText = default;
            }
        }
    }
}

