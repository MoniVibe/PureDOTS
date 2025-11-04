using PureDOTS.Runtime.Components;
using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Systems
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateAfter(typeof(PresentationSpawnSystem))]
    public partial struct PresentationRecycleSystem : ISystem
    {
        private ComponentLookup<PresentationHandle> _handleLookup;

        public void OnCreate(ref SystemState state)
        {
            _handleLookup = state.GetComponentLookup<PresentationHandle>();
            state.RequireForUpdate<PresentationCommandQueue>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var queueEntity = SystemAPI.GetSingletonEntity<PresentationCommandQueue>();
            var recycleBuffer = SystemAPI.GetBuffer<PresentationRecycleRequest>(queueEntity);
            if (recycleBuffer.Length == 0)
            {
                return;
            }

            var ecb = new EntityCommandBuffer(Allocator.Temp);
            _handleLookup.Update(ref state);

            for (int i = 0; i < recycleBuffer.Length; i++)
            {
                var request = recycleBuffer[i];
                if (!_handleLookup.HasComponent(request.Target))
                {
                    continue;
                }

                var handle = _handleLookup[request.Target];
                if (state.EntityManager.Exists(handle.Visual))
                {
                    ecb.DestroyEntity(handle.Visual);
                }

                ecb.RemoveComponent<PresentationHandle>(request.Target);
            }

            recycleBuffer.Clear();

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}

