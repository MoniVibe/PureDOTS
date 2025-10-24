using Unity.Entities;

namespace PureDOTS.Tests
{
    internal static class TestEntityManagerExtensions
    {
        public static bool HasSingleton<T>(this EntityManager entityManager) where T : unmanaged, IComponentData
        {
            using var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<T>());
            return !query.IsEmptyIgnoreFilter;
        }

        public static T GetSingleton<T>(this EntityManager entityManager) where T : unmanaged, IComponentData
        {
            using var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<T>());
            return query.GetSingleton<T>();
        }

        public static Entity GetSingletonEntity<T>(this EntityManager entityManager) where T : unmanaged, IComponentData
        {
            using var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<T>());
            return query.GetSingletonEntity();
        }

        public static void SetSingleton<T>(this EntityManager entityManager, in T value) where T : unmanaged, IComponentData
        {
            using var query = entityManager.CreateEntityQuery(ComponentType.ReadWrite<T>());
            var entity = query.GetSingletonEntity();
            entityManager.SetComponentData(entity, value);
        }
    }
}
