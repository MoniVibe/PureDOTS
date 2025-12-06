using System.IO;
using UnityEngine;

namespace PureDOTS.Config
{
    /// <summary>
    /// Loads configuration files from JSON.
    /// </summary>
    public static class ConfigLoader
    {
        /// <summary>
        /// Loads a config from JSON file. Returns default if file doesn't exist.
        /// </summary>
        public static T Load<T>(string path) where T : struct
        {
            var fullPath = Path.Combine(Application.streamingAssetsPath, path);
            
            if (!File.Exists(fullPath))
            {
                Debug.LogWarning($"[ConfigLoader] Config file not found at {fullPath}, using defaults.");
                return default(T);
            }

            try
            {
                var json = File.ReadAllText(fullPath);
                return JsonUtility.FromJson<T>(json);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ConfigLoader] Failed to load config from {fullPath}: {ex.Message}");
                return default(T);
            }
        }
    }
}

