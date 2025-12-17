using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Rendering
{
    /// <summary>
    /// Ensures legacy RenderCatalogSingleton data is mirrored onto the new RenderPresentationCatalog component.
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    public partial struct RenderPresentationCatalogSyncSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RenderCatalogSingleton>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (catalog, entity) in SystemAPI
                         .Query<RefRO<RenderCatalogSingleton>>()
                         .WithNone<RenderPresentationCatalog>()
                         .WithEntityAccess())
            {
                var value = catalog.ValueRO;
                ecb.AddComponent(entity, new RenderPresentationCatalog
                {
                    Blob = value.Blob,
                    RenderMeshArrayEntity = value.RenderMeshArrayEntity
                });
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
            state.Enabled = false;
        }

        public void OnDestroy(ref SystemState state)
        {
        }
    }
}
