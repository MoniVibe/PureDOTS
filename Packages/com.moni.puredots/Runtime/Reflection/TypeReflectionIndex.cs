using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using PureDOTS.Runtime.Debugging;
using UnityEngine;

namespace PureDOTS.Runtime.Reflection
{
    /// <summary>
    /// Runtime loader for Type Reflection Index JSON.
    /// Provides query API for components, buffers, systems, and blob types.
    /// </summary>
    public static class TypeReflectionIndex
    {
        private static TypeReflectionIndexData _index;
        private static bool _loaded;

        /// <summary>
        /// Loads the reflection index from JSON file.
        /// </summary>
        public static bool Load()
        {
            if (_loaded && _index != null)
            {
                return true;
            }

            try
            {
                var path = Path.Combine(Application.dataPath, "..", "Packages/com.moni.puredots/Generated/TypeReflectionIndex.json");
                if (!File.Exists(path))
                {
                    DebugLog.LogWarning($"Type Reflection Index not found at {path}. Run PureDOTS/Generate Type Reflection Index in editor.");
                    return false;
                }

                var json = File.ReadAllText(path);
                _index = JsonUtility.FromJson<TypeReflectionIndexData>(json);
                _loaded = true;
                return true;
            }
            catch (Exception ex)
            {
                DebugLog.LogError($"Failed to load Type Reflection Index: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Gets all component types.
        /// </summary>
        public static IEnumerable<ComponentInfo> GetComponents()
        {
            if (!EnsureLoaded()) return Enumerable.Empty<ComponentInfo>();
            return _index.Components ?? Enumerable.Empty<ComponentInfo>();
        }

        /// <summary>
        /// Gets all buffer element types.
        /// </summary>
        public static IEnumerable<BufferInfo> GetBuffers()
        {
            if (!EnsureLoaded()) return Enumerable.Empty<BufferInfo>();
            return _index.Buffers ?? Enumerable.Empty<BufferInfo>();
        }

        /// <summary>
        /// Gets all system types.
        /// </summary>
        public static IEnumerable<SystemInfo> GetSystems()
        {
            if (!EnsureLoaded()) return Enumerable.Empty<SystemInfo>();
            return _index.Systems ?? Enumerable.Empty<SystemInfo>();
        }

        /// <summary>
        /// Gets all blob asset types.
        /// </summary>
        public static IEnumerable<BlobTypeInfo> GetBlobTypes()
        {
            if (!EnsureLoaded()) return Enumerable.Empty<BlobTypeInfo>();
            return _index.BlobTypes ?? Enumerable.Empty<BlobTypeInfo>();
        }

        /// <summary>
        /// Finds a component by full name.
        /// </summary>
        public static ComponentInfo FindComponent(string fullName)
        {
            if (!EnsureLoaded()) return null;
            return _index.Components?.FirstOrDefault(c => c.FullName == fullName);
        }

        /// <summary>
        /// Finds a system by full name.
        /// </summary>
        public static SystemInfo FindSystem(string fullName)
        {
            if (!EnsureLoaded()) return null;
            return _index.Systems?.FirstOrDefault(s => s.FullName == fullName);
        }

        private static bool EnsureLoaded()
        {
            if (!_loaded)
            {
                Load();
            }
            return _loaded && _index != null;
        }

        [Serializable]
        public class TypeReflectionIndexData
        {
            public string GeneratedAt;
            public List<ComponentInfo> Components;
            public List<BufferInfo> Buffers;
            public List<SystemInfo> Systems;
            public List<BlobTypeInfo> BlobTypes;
        }

        [Serializable]
        public class ComponentInfo
        {
            public string FullName;
            public string Namespace;
            public string Name;
            public List<FieldInfo> Fields;
        }

        [Serializable]
        public class BufferInfo
        {
            public string FullName;
            public string Namespace;
            public string Name;
            public int InternalBufferCapacity;
            public List<FieldInfo> Fields;
        }

        [Serializable]
        public class SystemInfo
        {
            public string FullName;
            public string Namespace;
            public string Name;
            public string UpdateInGroup;
            public bool OrderFirst;
            public bool OrderLast;
            public string UpdateAfter;
            public string UpdateBefore;
            public bool IsBurstCompiled;
        }

        [Serializable]
        public class BlobTypeInfo
        {
            public string FullName;
            public string Namespace;
            public string Name;
        }

        [Serializable]
        public class FieldInfo
        {
            public string Name;
            public string Type;
            public string FullType;
        }
    }
}

