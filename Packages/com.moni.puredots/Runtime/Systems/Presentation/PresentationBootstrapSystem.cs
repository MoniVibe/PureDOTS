using PureDOTS.Runtime.Components;
using Unity.Entities;

namespace PureDOTS.Systems
{
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderFirst = true)]
    public partial struct PresentationBootstrapSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            if (SystemAPI.TryGetSingletonEntity<PresentationCommandQueue>(out _))
            {
                return;
            }

            var entity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(entity, new PresentationCommandQueue());
            state.EntityManager.AddBuffer<PresentationSpawnRequest>(entity);
            state.EntityManager.AddBuffer<PresentationRecycleRequest>(entity);
        }

        public void OnUpdate(ref SystemState state)
        {
        }
    }
}

