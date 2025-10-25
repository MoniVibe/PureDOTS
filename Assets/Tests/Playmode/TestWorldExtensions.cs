using Unity.Entities;

namespace PureDOTS.Tests
{
    internal static class TestWorldExtensions
    {
        public static SystemHandle EnsureSystem<T>(this World world) where T : unmanaged, ISystem
        {
            return world.GetOrCreateSystem<T>();
        }

        public static void UpdateSystem<T>(this World world) where T : unmanaged, ISystem
        {
            var handle = world.GetOrCreateSystem<T>();
            world.RunSystem(handle);
        }

        public static void RunSystem(this World world, SystemHandle handle)
        {
            handle.Update(world.Unmanaged);
        }
    }
}
