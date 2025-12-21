using PureDOTS.Runtime.Components;
using Unity.Entities;

namespace PureDOTS.Systems
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct PresentationContentRegistryBootstrapSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            if (SystemAPI.HasSingleton<PresentationContentRegistryReference>())
            {
                return;
            }

            var entity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(entity, new PresentationContentRegistryReference
            {
                Registry = BlobAssetReference<PresentationContentRegistryBlob>.Null
            });
        }

        public void OnUpdate(ref SystemState state)
        {
        }
    }
}
