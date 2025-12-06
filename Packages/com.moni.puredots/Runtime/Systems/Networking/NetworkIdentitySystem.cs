using PureDOTS.Runtime.Networking;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Systems.Networking
{
    /// <summary>
    /// Assigns NetworkId to entities on spawn for multiplayer-ready architecture.
    /// Currently assigns deterministic IDs; later network layer will use these for ownership.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    public partial struct NetworkIdentitySystem : ISystem
    {
        private uint _nextNetworkId;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _nextNetworkId = 1;
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Find entities that need NetworkId assigned
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (entity, _) in SystemAPI.Query<RefRO<NetworkIdentityTag>>()
                         .WithNone<NetworkId>()
                         .WithEntityAccess())
            {
                // Assign deterministic NetworkId
                // In multiplayer, server would assign these
                var networkId = new NetworkId
                {
                    Guid = (ulong)entity.Index ^ ((ulong)_nextNetworkId << 32),
                    Authority = NetworkAuthority.Server // Default to server authority
                };

                ecb.AddComponent(entity, networkId);
                _nextNetworkId++;
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}

