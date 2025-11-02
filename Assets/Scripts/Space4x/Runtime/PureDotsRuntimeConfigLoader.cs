using System.Collections;
using PureDOTS.Authoring;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Resource;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Space4X.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PureDotsConfigAuthoring))]
    public sealed class PureDotsRuntimeConfigLoader : MonoBehaviour
    {
        private static bool s_initialized;

        private PureDotsConfigAuthoring _authoring;

        private void Awake()
        {
            _authoring = GetComponent<PureDotsConfigAuthoring>();
        }

        private void Start()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                return;
            }
#endif
            if (s_initialized)
            {
                return;
            }

            StartCoroutine(InitializeNextFrame());
        }

        private IEnumerator InitializeNextFrame()
        {
            yield return null;
            EnsureRuntimeConfig();
        }

        private void EnsureRuntimeConfig()
        {
            if (s_initialized)
            {
                return;
            }

            if (_authoring == null || _authoring.config == null)
            {
                Debug.LogWarning("[PureDotsRuntimeConfigLoader] Missing PureDotsRuntimeConfig asset.", this);
                return;
            }

            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
            {
                Debug.LogWarning("[PureDotsRuntimeConfigLoader] Default world not ready.", this);
                return;
            }

            var entityManager = world.EntityManager;

            EnsureResourceTypeIndex(entityManager);
            EnsureResourceRecipeSet(entityManager);

            s_initialized = true;
        }

        private void EnsureResourceTypeIndex(EntityManager entityManager)
        {
            using var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<ResourceTypeIndex>());
            if (!query.IsEmptyIgnoreFilter)
            {
                return;
            }

            var catalogAsset = _authoring.config.ResourceTypes;
            if (catalogAsset == null || catalogAsset.entries == null || catalogAsset.entries.Count == 0)
            {
                Debug.LogWarning("[PureDotsRuntimeConfigLoader] Resource type catalog empty; registry will remain disabled.", _authoring.config);
                return;
            }

            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<ResourceTypeIndexBlob>();

            var idsBuilder = builder.Allocate(ref root.Ids, catalogAsset.entries.Count);
            var displayNamesBuilder = builder.Allocate(ref root.DisplayNames, catalogAsset.entries.Count);
            var colorsBuilder = builder.Allocate(ref root.Colors, catalogAsset.entries.Count);

            for (int i = 0; i < catalogAsset.entries.Count; i++)
            {
                var entry = catalogAsset.entries[i];
                idsBuilder[i] = new FixedString64Bytes(entry.id ?? string.Empty);
                builder.AllocateString(ref displayNamesBuilder[i], entry.id ?? string.Empty);
                colorsBuilder[i] = entry.displayColor;
            }

            var blobAsset = builder.CreateBlobAssetReference<ResourceTypeIndexBlob>(Allocator.Persistent);
            builder.Dispose();

            var entity = entityManager.CreateEntity(typeof(ResourceTypeIndex));
            entityManager.SetComponentData(entity, new ResourceTypeIndex { Catalog = blobAsset });

            Debug.Log("[PureDotsRuntimeConfigLoader] ResourceTypeIndex singleton created.", _authoring.config);
        }

        private void EnsureResourceRecipeSet(EntityManager entityManager)
        {
            using var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<ResourceRecipeSet>());
            if (!query.IsEmptyIgnoreFilter)
            {
                return;
            }

            if (_authoring.config.RecipeCatalog is not ResourceRecipeCatalog catalog)
            {
                Debug.LogWarning("[PureDotsRuntimeConfigLoader] Recipe catalog asset missing.", _authoring.config);
                return;
            }

            var families = catalog.Families;
            var recipes = catalog.Recipes;
            var familyCount = families?.Count ?? 0;
            var recipeCount = recipes?.Count ?? 0;

            if (familyCount == 0 && recipeCount == 0)
            {
                Debug.LogWarning("[PureDotsRuntimeConfigLoader] Recipe catalog contains no data.", catalog);
                return;
            }

            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<ResourceRecipeSetBlob>();

            var familiesBuilder = builder.Allocate(ref root.Families, familyCount);
            for (int i = 0; i < familyCount; i++)
            {
                ref var familyBlob = ref familiesBuilder[i];
                var definition = families![i];
                familyBlob.Id = ToFixedString64(definition.id);
                familyBlob.DisplayName = ToFixedString64(definition.displayName);
                familyBlob.RawResourceId = ToFixedString64(definition.rawResourceId);
                familyBlob.RefinedResourceId = ToFixedString64(definition.refinedResourceId);
                familyBlob.CompositeResourceId = ToFixedString64(definition.compositeResourceId);
                familyBlob.Description = ToFixedString128(definition.description);
            }

            var recipesBuilder = builder.Allocate(ref root.Recipes, recipeCount);
            for (int i = 0; i < recipeCount; i++)
            {
                ref var recipeBlob = ref recipesBuilder[i];
                var definition = recipes![i];

                recipeBlob.Id = ToFixedString64(definition.id);
                recipeBlob.Kind = definition.kind;
                recipeBlob.FacilityTag = ToFixedString32(definition.facilityTag);
                recipeBlob.OutputResourceId = ToFixedString64(definition.outputResourceId);
                recipeBlob.OutputAmount = math.max(1, definition.outputAmount);
                recipeBlob.ProcessSeconds = math.max(0f, definition.processSeconds);
                recipeBlob.Notes = ToFixedString128(definition.notes);

                var inputs = definition.inputs;
                var ingredientBuilder = builder.Allocate(ref recipeBlob.Ingredients, inputs?.Length ?? 0);
                if (inputs != null)
                {
                    for (int j = 0; j < inputs.Length; j++)
                    {
                        ref var ingredientBlob = ref ingredientBuilder[j];
                        ingredientBlob.ResourceId = ToFixedString64(inputs[j].resourceId);
                        ingredientBlob.Amount = math.max(1, inputs[j].amount);
                    }
                }
            }

            var recipeBlobAsset = builder.CreateBlobAssetReference<ResourceRecipeSetBlob>(Allocator.Persistent);
            builder.Dispose();

            var entity = entityManager.CreateEntity(typeof(ResourceRecipeSet));
            entityManager.SetComponentData(entity, new ResourceRecipeSet { Value = recipeBlobAsset });

            Debug.Log("[PureDotsRuntimeConfigLoader] ResourceRecipeSet singleton created.", catalog);
        }

        private static FixedString32Bytes ToFixedString32(string value)
        {
            FixedString32Bytes result = default;
            if (!string.IsNullOrWhiteSpace(value))
            {
                result.Append(value.Trim());
            }
            return result;
        }

        private static FixedString64Bytes ToFixedString64(string value)
        {
            FixedString64Bytes result = default;
            if (!string.IsNullOrWhiteSpace(value))
            {
                result.Append(value.Trim());
            }
            return result;
        }

        private static FixedString128Bytes ToFixedString128(string value)
        {
            FixedString128Bytes result = default;
            if (!string.IsNullOrWhiteSpace(value))
            {
                result.Append(value.Trim());
            }
            return result;
        }
    }
}

