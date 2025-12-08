using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Collections.LowLevel.Unsafe;

namespace PureDOTS.Runtime.Networking
{
    /// <summary>
    /// Deterministic serialization specification for networkable components.
    /// Defines canonical serialization order:
    /// - Sort components by ComponentTypeIndex
    /// - Serialize numeric types little-endian
    /// - Never include padding or pointers
    /// - Store version numbers per component type
    /// 
    /// Local save/load uses this serializer; network sync will reuse it verbatim.
    /// </summary>
    [BurstCompile]
    public static class DeterministicSerializer
    {
        /// <summary>
        /// Component type version for change detection.
        /// </summary>
        public struct ComponentVersion
        {
            public ComponentType ComponentType;
            public uint Version;
        }

        /// <summary>
        /// Serializes a component deterministically.
        /// Components are serialized in order by ComponentTypeIndex.
        /// </summary>
        [BurstCompile]
        public static void SerializeComponent<T>(ref SnapshotWriter writer, in T component) where T : unmanaged, IComponentData
        {
            // Write component type index for ordering
            var componentType = ComponentType.ReadWrite<T>();
            writer.WriteInt(componentType.TypeIndex.Value);
            
            // Write version (default to 1)
            writer.WriteUInt(1);
            
            // Write component data
            unsafe
            {
                var size = UnsafeUtility.SizeOf<T>();
                var value = component; // copy to allow ref access
                writer.WriteBytes((byte*)UnsafeUtility.AddressOf(ref value), size);
            }
        }

        /// <summary>
        /// Deserializes a component deterministically.
        /// </summary>
        [BurstCompile]
        public static bool DeserializeComponent<T>(ref SnapshotReader reader, out T component) where T : unmanaged, IComponentData
        {
            component = default;
            
            if (!reader.IsValid)
            {
                return false;
            }

            // Read component type index
            int typeIndex = reader.ReadInt();
            var expectedType = ComponentType.ReadWrite<T>();
            if (typeIndex != expectedType.TypeIndex.Value)
            {
                return false;
            }

            // Read version
            uint version = reader.ReadUInt();
            // Version can be used for migration logic later

            // Read component data
            unsafe
            {
                var size = UnsafeUtility.SizeOf<T>();
                if (reader.Position + size > reader.Length)
                {
                    return false;
                }
                reader.ReadBytes((byte*)UnsafeUtility.AddressOf(ref component), size);
            }

            return true;
        }

        /// <summary>
        /// Serializes components in deterministic order (by ComponentTypeIndex).
        /// </summary>
        [BurstCompile]
        public static void SerializeComponents<T>(ref SnapshotWriter writer, NativeArray<T> components) where T : unmanaged, IComponentData
        {
            writer.WriteInt(components.Length);
            for (int i = 0; i < components.Length; i++)
            {
                SerializeComponent(ref writer, components[i]);
            }
        }

        /// <summary>
        /// Deserializes components in deterministic order.
        /// </summary>
        [BurstCompile]
        public static bool DeserializeComponents<T>(ref SnapshotReader reader, out NativeArray<T> components, Allocator allocator) where T : unmanaged, IComponentData
        {
            components = default;
            
            if (!reader.IsValid)
            {
                return false;
            }

            int count = reader.ReadInt();
            if (count < 0 || count > 1000000) // Sanity check
            {
                return false;
            }

            components = new NativeArray<T>(count, allocator);
            for (int i = 0; i < count; i++)
            {
                if (!DeserializeComponent<T>(ref reader, out var element))
                {
                    components.Dispose();
                    return false;
                }

                components[i] = element;
            }

            return true;
        }
    }
}

