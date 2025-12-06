using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.Serialization;
using PureDOTS.Runtime.Config;

namespace PureDOTS.Systems
{
    /// <summary>
    /// Loads tuning profiles from JSON and creates BlobAssets.
    /// Supports hot-reload for live gameplay iteration.
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct TuningProfileLoaderSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            // System will load profiles on demand
        }

        public void OnUpdate(ref SystemState state)
        {
            // Profile loading would happen here
            // For now, this is a placeholder showing the pattern
        }
    }

    /// <summary>
    /// Helper for loading tuning profiles from JSON.
    /// </summary>
    public static class TuningProfileLoader
    {
        /// <summary>
        /// Loads a tuning profile from JSON file and creates a BlobAsset.
        /// </summary>
        public static BlobAssetReference<TuningProfileBlob> LoadFromJson(string jsonPath)
        {
            // Simplified: In a real implementation, this would:
            // 1. Read JSON file
            // 2. Parse JSON into TuningProfileBlob structure
            // 3. Create BlobAsset using BlobBuilder
            // 4. Return BlobAssetReference

            var builder = new BlobBuilder(Allocator.Temp);
            ref var profile = ref builder.ConstructRoot<TuningProfileBlob>();

            // Example: Set profile name
            builder.AllocateString(ref profile.ProfileName, "DefaultProfile");
            builder.AllocateString(ref profile.Domain, "Physics");

            // Example: Add parameters
            var parametersBuilder = builder.Allocate(ref profile.Parameters, 1);
            ref var gravityParam = ref parametersBuilder[0];
            builder.AllocateString(ref gravityParam.Name, "Gravity");
            gravityParam.Value = 9.81f;
            gravityParam.Type = 0; // float

            var result = builder.CreateBlobAssetReference<TuningProfileBlob>(Allocator.Persistent);
            builder.Dispose();
            return result;
        }

        /// <summary>
        /// Hot-reloads a tuning profile, updating existing BlobAsset references.
        /// </summary>
        public static void HotReloadProfile(
            EntityManager entityManager,
            Entity profileEntity,
            string jsonPath)
        {
            var newProfile = LoadFromJson(jsonPath);

            if (entityManager.HasComponent<TuningProfileRef>(profileEntity))
            {
                var oldRef = entityManager.GetComponentData<TuningProfileRef>(profileEntity);
                oldRef.Profile.Dispose(); // Dispose old profile

                entityManager.SetComponentData(profileEntity, new TuningProfileRef { Profile = newProfile });

                // Update metadata version
                if (entityManager.HasComponent<TuningProfileMetadata>(profileEntity))
                {
                    var metadata = entityManager.GetComponentData<TuningProfileMetadata>(profileEntity);
                    metadata.Version++;
                    entityManager.SetComponentData(profileEntity, metadata);
                }
            }
        }
    }
}

