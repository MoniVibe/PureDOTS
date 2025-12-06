using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace PureDOTS.Editor.Reflection
{
    /// <summary>
    /// Editor tool that scans all assemblies for ECS types and generates a JSON index.
    /// Run via menu: PureDOTS/Generate Type Reflection Index
    /// 
    /// See: Docs/Guides/DemoLockSystemsGuide.md#type-reflection-index
    /// API Reference: Docs/Guides/DemoLockSystemsAPI.md#type-reflection-index-api
    /// </summary>
    public static class TypeReflectionIndexGenerator
    {
        private const string OutputPath = "Packages/com.moni.puredots/Generated/TypeReflectionIndex.json";

        [MenuItem("PureDOTS/Generate Type Reflection Index")]
        public static void GenerateIndex()
        {
            try
            {
                var index = new TypeReflectionIndex
                {
                    GeneratedAt = DateTime.UtcNow.ToString("O"),
                    Components = new List<ComponentInfo>(),
                    Buffers = new List<BufferInfo>(),
                    Systems = new List<SystemInfo>(),
                    BlobTypes = new List<BlobTypeInfo>()
                };

                // Scan all assemblies
                var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                    .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
                    .Where(a => a.GetName().Name.StartsWith("PureDOTS") || 
                                a.GetName().Name.StartsWith("Unity.Entities") ||
                                a.GetName().Name.StartsWith("Unity.Collections"))
                    .ToList();

                foreach (var assembly in assemblies)
                {
                    try
                    {
                        ScanAssembly(assembly, index);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"Failed to scan assembly {assembly.GetName().Name}: {ex.Message}");
                    }
                }

                // Write JSON
                var json = JsonUtility.ToJson(index, true);
                var fullPath = Path.Combine(Application.dataPath, "..", OutputPath);
                var directory = Path.GetDirectoryName(fullPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(fullPath, json);
                Debug.Log($"Type Reflection Index generated: {OutputPath} ({index.Components.Count} components, {index.Buffers.Count} buffers, {index.Systems.Count} systems)");
                
                AssetDatabase.Refresh();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to generate Type Reflection Index: {ex}");
            }
        }

        private static void ScanAssembly(Assembly assembly, TypeReflectionIndex index)
        {
            var types = assembly.GetTypes()
                .Where(t => t.IsValueType && !t.IsEnum && !t.IsPrimitive)
                .ToList();

            foreach (var type in types)
            {
                // Check for IComponentData
                if (typeof(IComponentData).IsAssignableFrom(type) && !type.IsAbstract && !type.IsInterface)
                {
                    var info = ExtractComponentInfo(type);
                    if (info != null)
                    {
                        index.Components.Add(info);
                    }
                }

                // Check for IBufferElementData
                if (typeof(IBufferElementData).IsAssignableFrom(type) && !type.IsAbstract && !type.IsInterface)
                {
                    var info = ExtractBufferInfo(type);
                    if (info != null)
                    {
                        index.Buffers.Add(info);
                    }
                }

                // Check for ISystem
                if (typeof(ISystem).IsAssignableFrom(type) && !type.IsAbstract && !type.IsInterface)
                {
                    var info = ExtractSystemInfo(type);
                    if (info != null)
                    {
                        index.Systems.Add(info);
                    }
                }

                // Check for BlobAssetReference<T> usage
                ScanBlobTypes(type, index);
            }
        }

        private static ComponentInfo ExtractComponentInfo(Type type)
        {
            var info = new ComponentInfo
            {
                FullName = type.FullName,
                Namespace = type.Namespace,
                Name = type.Name,
                Fields = new List<FieldInfo>()
            };

            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
            foreach (var field in fields)
            {
                info.Fields.Add(new FieldInfo
                {
                    Name = field.Name,
                    Type = field.FieldType.Name,
                    FullType = field.FieldType.FullName
                });
            }

            return info;
        }

        private static BufferInfo ExtractBufferInfo(Type type)
        {
            var info = new BufferInfo
            {
                FullName = type.FullName,
                Namespace = type.Namespace,
                Name = type.Name,
                Fields = new List<FieldInfo>()
            };

            // Check for InternalBufferCapacity attribute
            var capacityAttr = type.GetCustomAttribute<InternalBufferCapacityAttribute>();
            if (capacityAttr != null)
            {
                info.InternalBufferCapacity = capacityAttr.Capacity;
            }

            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
            foreach (var field in fields)
            {
                info.Fields.Add(new FieldInfo
                {
                    Name = field.Name,
                    Type = field.FieldType.Name,
                    FullType = field.FieldType.FullName
                });
            }

            return info;
        }

        private static SystemInfo ExtractSystemInfo(Type type)
        {
            var info = new SystemInfo
            {
                FullName = type.FullName,
                Namespace = type.Namespace,
                Name = type.Name
            };

            // Check for UpdateInGroup attribute
            var updateInGroupAttr = type.GetCustomAttribute<UpdateInGroupAttribute>();
            if (updateInGroupAttr != null)
            {
                info.UpdateInGroup = updateInGroupAttr.GroupType.FullName;
                info.OrderFirst = updateInGroupAttr.OrderFirst;
                info.OrderLast = updateInGroupAttr.OrderLast;
            }

            // Check for UpdateAfter/UpdateBefore
            var updateAfterAttr = type.GetCustomAttribute<UpdateAfterAttribute>();
            if (updateAfterAttr != null)
            {
                info.UpdateAfter = updateAfterAttr.SystemType.FullName;
            }

            var updateBeforeAttr = type.GetCustomAttribute<UpdateBeforeAttribute>();
            if (updateBeforeAttr != null)
            {
                info.UpdateBefore = updateBeforeAttr.SystemType.FullName;
            }

            // Check for BurstCompile
            var burstAttr = type.GetCustomAttribute<Unity.Burst.BurstCompileAttribute>();
            if (burstAttr != null)
            {
                info.IsBurstCompiled = true;
            }

            return info;
        }

        private static void ScanBlobTypes(Type type, TypeReflectionIndex index)
        {
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
            foreach (var field in fields)
            {
                if (field.FieldType.IsGenericType)
                {
                    var genericType = field.FieldType.GetGenericTypeDefinition();
                    if (genericType == typeof(BlobAssetReference<>))
                    {
                        var blobType = field.FieldType.GetGenericArguments()[0];
                        if (!index.BlobTypes.Any(b => b.FullName == blobType.FullName))
                        {
                            index.BlobTypes.Add(new BlobTypeInfo
                            {
                                FullName = blobType.FullName,
                                Namespace = blobType.Namespace,
                                Name = blobType.Name
                            });
                        }
                    }
                }
            }
        }

        [Serializable]
        public class TypeReflectionIndex
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

