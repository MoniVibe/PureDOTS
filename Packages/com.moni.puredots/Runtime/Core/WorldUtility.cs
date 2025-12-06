using Unity.Entities;
using UnityEngine;

namespace PureDOTS.Core
{
    /// <summary>
    /// Utility for creating and managing ECS worlds.
    /// </summary>
    public static class WorldUtility
    {
        /// <summary>
        /// Creates a Unity Entities world with the specified name.
        /// </summary>
        public static World CreateWorld<TWorldType>(string name) where TWorldType : class
        {
            var world = new World(name, WorldFlags.Game);
            WorldRegistry.RegisterWorld(name, world);
            return world;
        }

        /// <summary>
        /// Registry to track all active worlds.
        /// </summary>
        public static class WorldRegistry
        {
            private static System.Collections.Generic.Dictionary<string, World> _worlds = new();

            public static void RegisterWorld(string name, World world)
            {
                _worlds[name] = world;
            }

            public static World GetWorld(string name)
            {
                return _worlds.TryGetValue(name, out var world) ? world : null;
            }

            public static void DisposeAll()
            {
                foreach (var world in _worlds.Values)
                {
                    if (world.IsCreated)
                    {
                        world.Dispose();
                    }
                }
                _worlds.Clear();
            }
        }
    }

    /// <summary>
    /// Wrapper types for different ECS world types.
    /// </summary>
    public class BodyECSWorld { }
    public class MindECSWorld { }
    public class AggregateECSWorld { }
    public class PresentationECSWorld { }
}

