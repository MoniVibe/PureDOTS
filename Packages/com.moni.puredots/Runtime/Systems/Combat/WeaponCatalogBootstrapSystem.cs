using PureDOTS.Runtime.Combat;
using PureDOTS.Runtime.Components;
using Unity.Entities;

namespace PureDOTS.Systems.Combat
{
    /// <summary>
    /// Ensures a weapon catalog exists for headless/early iteration runs.
    /// Creates a minimal default catalog if no authored catalog is present.
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct WeaponCatalogBootstrapSystem : ISystem
    {
        private static BlobAssetReference<WeaponCatalogBlob> s_DefaultCatalog;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
        }

        public void OnUpdate(ref SystemState state)
        {
            if (SystemAPI.HasSingleton<WeaponCatalog>())
            {
                state.Enabled = false;
                return;
            }

            if (!s_DefaultCatalog.IsCreated)
            {
                s_DefaultCatalog = WeaponCatalogDefaults.CreateDefaultCatalog();
            }

            var entity = state.EntityManager.CreateEntity(typeof(WeaponCatalog));
            state.EntityManager.SetComponentData(entity, new WeaponCatalog
            {
                Catalog = s_DefaultCatalog
            });

            state.Enabled = false;
        }

        public void OnDestroy(ref SystemState state)
        {
            if (s_DefaultCatalog.IsCreated)
            {
                s_DefaultCatalog.Dispose();
                s_DefaultCatalog = default;
            }
        }
    }
}
