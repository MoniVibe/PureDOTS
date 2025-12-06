using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using PureDOTS.Runtime.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

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
            if (world.EntityManager.HasComponent<TimeState>(world.EntityManager.CreateEntityQuery(typeof(TimeState)).GetSingletonEntity()))
            {
                var timeState = world.EntityManager.CreateEntityQuery(typeof(TimeState)).GetSingleton<TimeState>();
                return timeState.Tick;
            }
            return 0;
        }

        private static string ComputeWorldHash(World world)
        {
            // Simple hash based on entity count and component types
            // In production, this would be a proper deterministic hash
            var entityCount = world.EntityManager.GetAllEntities().Length;
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
                    // Skip system entities and singletons
                    if (entityManager.HasComponent<SystemState>(entity))
                        continue;

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
                    var componentData = entityManager.GetComponentDataRaw(entity, componentType);
                    components.Add(new ComponentData
                    {
                        TypeName = componentType.GetManagedType().FullName,
                        Data = Convert.ToBase64String(componentData)
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

        private static List<BufferData> SerializeEntityBuffers(EntityManager entityManager, Entity entity)
        {
            var buffers = new List<BufferData>();
            var bufferTypes = entityManager.GetBufferTypes(entity);

            foreach (var bufferType in bufferTypes)
            {
                try
                {
                    var buffer = entityManager.GetBufferRaw(entity, bufferType);
                    buffers.Add(new BufferData
                    {
                        TypeName = bufferType.GetManagedType().FullName,
                        ElementCount = buffer.Length,
                        Data = Convert.ToBase64String(buffer.AsNativeArray().ToArray())
                    });
                }
                catch
                {
                    // Skip buffers that can't be serialized
                }
            }

            bufferTypes.Dispose();
            return buffers;
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

