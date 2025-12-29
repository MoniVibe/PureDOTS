using PureDOTS.Rendering;
using PureDOTS.Runtime.Physics;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;

namespace PureDOTS.Systems.Physics
{
    /// <summary>
    /// Applies collider specs from the profile to entities with RenderSemanticKey that lack explicit physics authoring.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateBefore(typeof(PhysicsBodyBootstrapSystem))]
    public partial struct PhysicsColliderProfileRepairSystem : ISystem
    {
        private EntityQuery _repairQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _repairQuery = SystemAPI.QueryBuilder()
                .WithAll<RenderSemanticKey>()
                .WithNone<PhysicsCollider, PhysicsColliderSpec, PhysicsInteractionConfig, RequiresPhysics>()
                .Build();

            state.RequireForUpdate(_repairQuery);
            state.RequireForUpdate<PhysicsColliderProfileComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton<PhysicsColliderProfileComponent>(out var profileComponent) ||
                !profileComponent.Profile.IsCreated)
            {
                return;
            }

            ref var entries = ref profileComponent.Profile.Value.Entries;
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (semanticKey, entity) in SystemAPI
                         .Query<RefRO<RenderSemanticKey>>()
                         .WithNone<PhysicsCollider, PhysicsColliderSpec, PhysicsInteractionConfig, RequiresPhysics>()
                         .WithEntityAccess())
            {
                if (!PhysicsColliderProfileHelpers.TryGetSpec(ref entries, semanticKey.ValueRO.Value, out var spec))
                {
                    continue;
                }

                ecb.AddComponent(entity, new RequiresPhysics
                {
                    Priority = 0,
                    Flags = spec.Flags
                });
                ecb.AddComponent(entity, spec);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}
