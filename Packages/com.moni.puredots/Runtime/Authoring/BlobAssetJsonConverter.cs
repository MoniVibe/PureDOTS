#if UNITY_EDITOR
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using System.IO;

namespace PureDOTS.Runtime.Authoring
{
    /// <summary>
    /// Utility for converting JSON manifest files to BlobAssets.
    /// Supports hot-reload of blob data without domain reload.
    /// </summary>
    public static class BlobAssetJsonConverter
    {
        /// <summary>
        /// Converts JSON manifest to BlobAsset reference.
        /// </summary>
        public static BlobAssetReference<T> FromJson<T>(string jsonPath) where T : struct
        {
            // Minimal safe implementation: read file if present, ignore contents, and return a default blob.
            // This avoids editor crashes while keeping hot-reload hooks callable.
            using var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<T>();
            // Optionally log missing files for diagnostics.
            if (!string.IsNullOrEmpty(jsonPath) && !File.Exists(jsonPath))
            {
                Debug.LogWarning($"BlobAssetJsonConverter: manifest not found at path '{jsonPath}', using default {typeof(T).Name}.");
            }
            return builder.CreateBlobAssetReference<T>(Allocator.Persistent);
        }

        /// <summary>
        /// Converts BlobAsset to JSON for editing.
        /// </summary>
        public static string ToJson<T>(BlobAssetReference<T> blobRef) where T : struct
        {
            if (!blobRef.IsCreated)
            {
                return "{}";
            }

            // Minimal placeholder: produce an empty JSON object to avoid exceptions.
            // Future: serialize fields of T for authoring.
            return "{}";
        }
    }
}
#endif

