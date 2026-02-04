using PureDOTS.Runtime.Anatomy;
using PureDOTS.Runtime.Components;
using Unity.Burst;
using Unity.Entities;

namespace PureDOTS.Systems.Anatomy
{
    /// <summary>
    /// Ensures an anatomy catalog exists (creates a default catalog if none is authored).
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct AnatomyCatalogBootstrapSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
        }

        public void OnUpdate(ref SystemState state)
        {
            if (SystemAPI.HasSingleton<AnatomyCatalogRef>())
            {
                state.Enabled = false;
                return;
            }

            var entity = state.EntityManager.CreateEntity(typeof(AnatomyCatalogRef));
            state.EntityManager.SetComponentData(entity, new AnatomyCatalogRef
            {
                Catalog = AnatomyCatalogDefaults.BuildDefaultCatalog()
            });

            state.Enabled = false;
        }

        public void OnDestroy(ref SystemState state)
        {
            if (!SystemAPI.HasSingleton<AnatomyCatalogRef>())
            {
                return;
            }

            var catalogRef = SystemAPI.GetSingleton<AnatomyCatalogRef>();
            if (catalogRef.Catalog.IsCreated)
            {
                catalogRef.Catalog.Dispose();
            }
        }
    }
}
