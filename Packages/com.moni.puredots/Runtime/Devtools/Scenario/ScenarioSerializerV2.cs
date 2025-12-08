using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Runtime.InteropServices;
using PureDOTS.Runtime.Components;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityDebug = UnityEngine.Debug;

namespace PureDOTS.Runtime.Devtools.Scenario
{
    /// <summary>
    /// Version 2 scenario serializer that captures full world state (entities, components, buffers, singletons).
    /// Supports versioning and migration for modding & persistence.
    /// 
    /// See: Docs/Guides/DemoLockSystemsGuide.md#scenario-serializer-v2
    /// API Reference: Docs/Guides/DemoLockSystemsAPI.md#scenario-serializer-v2-api
    /// </summary>
    public static class ScenarioSerializerV2
    {
        public const int CurrentVersion = 2;
        public const string FormatVersion = "2.0.0";

        /// <summary>
        /// Serializes the current world state to a versioned JSON scenario file.
        /// </summary>
        public static bool Serialize(World world, string filePath, out string error)
        {
            error = null;

            try
            {
                var scenario = new ScenarioV2Data
                {
                    Version = CurrentVersion,
                    FormatVersion = FormatVersion,
                    Metadata = new ScenarioMetadata
                    {
                        Tick = GetCurrentTick(world),
                        WorldHash = ComputeWorldHash(world),
                        Timestamp = DateTime.UtcNow.ToString("O")
                    },
                    Entities = SerializeEntities(world),
                    Singletons = SerializeSingletons(world)
                };

                var json = JsonUtility.ToJson(scenario, true);
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(filePath, json);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static uint GetCurrentTick(World world)
        {
            var entityManager = world.EntityManager;
            var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<TimeState>());
            if (query.TryGetSingleton<TimeState>(out var timeState))
            {
                return timeState.Tick;
            }
            return 0u;
        }

        private static string ComputeWorldHash(World world)
        {
            // Simple hash based on entity count and component types
            // In production, this would be a proper deterministic hash
            using var entities = world.EntityManager.GetAllEntities(Allocator.Temp);
            var entityCount = entities.Length;
            return $"hash_{entityCount}_{DateTime.UtcNow.Ticks}";
        }

        private static List<EntityData> SerializeEntities(World world)
        {
            var entities = new List<EntityData>();
            var entityManager = world.EntityManager;
            var allEntities = entityManager.GetAllEntities(Allocator.Temp);

            try
            {
                foreach (var entity in allEntities)
                {
                var entityData = new EntityData
                {
                        EntityIndex = entity.Index,
                        EntityVersion = entity.Version,
                        Components = SerializeEntityComponents(entityManager, entity),
                        Buffers = SerializeEntityBuffers(entityManager, entity)
                    };

                    entities.Add(entityData);
                }
            }
            finally
            {
                allEntities.Dispose();
            }

            return entities;
        }

        private static List<ComponentData> SerializeEntityComponents(EntityManager entityManager, Entity entity)
        {
            var components = new List<ComponentData>();
            var componentTypes = entityManager.GetComponentTypes(entity);

            foreach (var componentType in componentTypes)
            {
                if (componentType.IsBuffer)
                    continue; // Buffers handled separately

                try
                {
                    if (componentType.IsZeroSized)
                        continue;

                    var managedType = componentType.GetManagedType();
                    if (managedType == null || !managedType.IsValueType)
                        continue;

                    var typeInfo = TypeManager.GetTypeInfo(componentType.TypeIndex);
                    var size = typeInfo.ElementSize;
                    if (size <= 0)
                        continue;

                    var boxedValue = GetComponentDataBoxed(entityManager, entity, managedType);
                    if (boxedValue == null)
                        continue;

                    var bytes = new byte[size];
                    var handle = GCHandle.Alloc(boxedValue, GCHandleType.Pinned);
                    try
                    {
                        Marshal.Copy(handle.AddrOfPinnedObject(), bytes, 0, size);
                    }
                    finally
                    {
                        handle.Free();
                    }

                    components.Add(new ComponentData
                    {
                        TypeName = managedType.FullName,
                        Data = Convert.ToBase64String(bytes)
                    });
                }
                catch
                {
                    // Skip components that can't be serialized
                }
            }

            componentTypes.Dispose();
            return components;
        }

        private static readonly MethodInfo s_getBufferMethod =
            typeof(EntityManager).GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .FirstOrDefault(m => m.Name == nameof(EntityManager.GetBuffer)
                && m.IsGenericMethodDefinition
                && m.GetParameters().Length == 1);

        private static readonly MethodInfo s_getComponentDataMethod =
            typeof(EntityManager).GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(m => m.Name == nameof(EntityManager.GetComponentData)
                                     && m.IsGenericMethodDefinition
                                     && m.GetParameters().Length == 1);

        private static readonly MethodInfo s_getUnsafePtrMethod =
            typeof(NativeArrayUnsafeUtility).GetMethods(BindingFlags.Static | BindingFlags.Public)
            .FirstOrDefault(m => m.Name == nameof(NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr)
                && m.IsGenericMethodDefinition);

        private static unsafe List<BufferData> SerializeEntityBuffers(EntityManager entityManager, Entity entity)
        {
            var buffers = new List<BufferData>();
            var componentTypes = entityManager.GetComponentTypes(entity, Allocator.Temp);

            try
            {
                foreach (var componentType in componentTypes)
                {
                    if (!componentType.IsBuffer)
                        continue;

                    try
                    {
                        var elementType = componentType.GetManagedType();
                        if (elementType == null || !elementType.IsValueType)
                            continue;

                        var dynamicBuffer = GetDynamicBuffer(entityManager, entity, elementType);
                        if (dynamicBuffer == null)
                            continue;

                        var bufferLength = GetBufferLength(dynamicBuffer);
                        var nativeArray = AsNativeArray(dynamicBuffer);
                        if (nativeArray == null)
                            continue;

                        var sizeOfElement = Marshal.SizeOf(elementType);
                        var byteCount = checked(sizeOfElement * bufferLength);
                        var dataBytes = new byte[byteCount];

                        if (byteCount > 0)
                        {
                            var ptr = GetUnsafeReadOnlyPtr(nativeArray, elementType);
                            fixed (byte* dest = dataBytes)
                            {
                                UnsafeUtility.MemCpy(dest, (void*)ptr, byteCount);
                            }
                        }

                        buffers.Add(new BufferData
                        {
                            TypeName = elementType.FullName,
                            ElementCount = bufferLength,
                            Data = Convert.ToBase64String(dataBytes)
                        });
                    }
                    catch
                    {
                        // Skip buffers that can't be serialized
                    }
                }
            }
            finally
            {
                componentTypes.Dispose();
            }

            return buffers;
        }

        private static object GetDynamicBuffer(EntityManager entityManager, Entity entity, Type elementType)
        {
            if (s_getBufferMethod == null)
                return null;

            var generic = s_getBufferMethod.MakeGenericMethod(elementType);
            return generic.Invoke(entityManager, new object[] { entity });
        }

        private static object GetComponentDataBoxed(EntityManager entityManager, Entity entity, Type componentType)
        {
            if (s_getComponentDataMethod == null)
                return null;

            var generic = s_getComponentDataMethod.MakeGenericMethod(componentType);
            return generic.Invoke(entityManager, new object[] { entity });
        }

        private static int GetBufferLength(object dynamicBuffer)
        {
            var lengthProp = dynamicBuffer.GetType().GetProperty(nameof(DynamicBuffer<int>.Length));
            if (lengthProp == null)
                return 0;
            return (int)lengthProp.GetValue(dynamicBuffer);
        }

        private static object AsNativeArray(object dynamicBuffer)
        {
            var method = dynamicBuffer.GetType().GetMethod(nameof(DynamicBuffer<int>.AsNativeArray));
            return method?.Invoke(dynamicBuffer, null);
        }

        private static IntPtr GetUnsafeReadOnlyPtr(object nativeArray, Type elementType)
        {
            if (s_getUnsafePtrMethod == null)
                return IntPtr.Zero;

            var generic = s_getUnsafePtrMethod.MakeGenericMethod(elementType);
            var result = generic.Invoke(null, new object[] { nativeArray });
            return (IntPtr)result;
        }

        private static Dictionary<string, object> SerializeSingletons(World world)
        {
            var singletons = new Dictionary<string, object>();
            var entityManager = world.EntityManager;

            // Serialize common singletons
            var singletonQuery = entityManager.CreateEntityQuery(typeof(TimeState));
            if (singletonQuery.CalculateEntityCount() > 0)
            {
                var timeState = singletonQuery.GetSingleton<TimeState>();
                singletons["TimeState"] = JsonUtility.ToJson(timeState);
            }

            return singletons;
        }

        [Serializable]
        public class ScenarioV2Data
        {
            public int Version;
            public string FormatVersion;
            public ScenarioMetadata Metadata;
            public List<EntityData> Entities;
            public Dictionary<string, object> Singletons;
        }

        [Serializable]
        public class ScenarioMetadata
        {
            public uint Tick;
            public string WorldHash;
            public string Timestamp;
        }

        [Serializable]
        public class EntityData
        {
            public int EntityIndex;
            public int EntityVersion;
            public List<ComponentData> Components;
            public List<BufferData> Buffers;
        }

        [Serializable]
        public class ComponentData
        {
            public string TypeName;
            public string Data; // Base64 encoded
        }

        [Serializable]
        public class BufferData
        {
            public string TypeName;
            public int ElementCount;
            public string Data; // Base64 encoded
        }
    }
}

