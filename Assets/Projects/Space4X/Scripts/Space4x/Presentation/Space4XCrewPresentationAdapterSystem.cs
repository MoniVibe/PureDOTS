using PureDOTS.Runtime.Components;
using PureDOTS.Systems;
using Space4X.Runtime;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Space4X.Presentation
{
    /// <summary>
    /// Spawns and maintains presentation handles for Space4X crew aggregate entities.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(Space4X.Systems.Space4XCrewAggregationSystem))]
    public partial struct Space4XCrewPresentationAdapterSystem : ISystem
    {
        private ComponentLookup<PresentationHandle> _handleLookup;
        private ComponentLookup<LocalTransform> _transformLookup;
        private ComponentLookup<AggregateEntity> _aggregateLookup;

        public void OnCreate(ref SystemState state)
        {
            _handleLookup = state.GetComponentLookup<PresentationHandle>();
            _transformLookup = state.GetComponentLookup<LocalTransform>(true);
            _aggregateLookup = state.GetComponentLookup<AggregateEntity>(true);
            state.RequireForUpdate<Space4XCrewAggregateData>();
            state.RequireForUpdate<AggregateEntity>();
            state.RequireForUpdate<PresentationCommandQueue>();
        }

        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingletonEntity<PresentationCommandQueue>(out var queueEntity))
            {
                return;
            }

            var spawnBuffer = state.EntityManager.GetBuffer<PresentationSpawnRequest>(queueEntity);
            var recycleBuffer = state.EntityManager.GetBuffer<PresentationRecycleRequest>(queueEntity);

            _handleLookup.Update(ref state);
            _transformLookup.Update(ref state);
            _aggregateLookup.Update(ref state);

            foreach (var (aggregateRef, crewDataRef, transformRef, entity) in SystemAPI
                         .Query<RefRO<AggregateEntity>, RefRO<Space4XCrewAggregateData>, RefRO<LocalTransform>>()
                         .WithEntityAccess())
            {
                if (aggregateRef.ValueRO.Category != AggregateCategory.Crew)
                {
                    continue;
                }

                var hasHandle = _handleLookup.HasComponent(entity);
                if (aggregateRef.ValueRO.MemberCount == 0)
                {
                    if (hasHandle)
                    {
                        recycleBuffer.Add(new PresentationRecycleRequest { Target = entity });
                    }

                    continue;
                }

                var descriptorHash = Space4XCrewDescriptorHashes.Resolve(crewDataRef.ValueRO.Duty);
                if (!descriptorHash.IsValid)
                {
                    continue;
                }

                float3 position = transformRef.ValueRO.Position;
                quaternion rotation = transformRef.ValueRO.Rotation;

                if (crewDataRef.ValueRO.CurrentCraft != Entity.Null && _transformLookup.HasComponent(crewDataRef.ValueRO.CurrentCraft))
                {
                    var craftTransform = _transformLookup[crewDataRef.ValueRO.CurrentCraft];
                    position = craftTransform.Position;
                    rotation = craftTransform.Rotation;
                }
                else if (crewDataRef.ValueRO.HomeCarrier != Entity.Null && _transformLookup.HasComponent(crewDataRef.ValueRO.HomeCarrier))
                {
                    var carrierTransform = _transformLookup[crewDataRef.ValueRO.HomeCarrier];
                    position = carrierTransform.Position;
                    rotation = carrierTransform.Rotation;
                }

                var moraleRatio = math.saturate(aggregateRef.ValueRO.Morale * 0.01f);
                var tint = new float4(math.lerp(0.4f, 1f, moraleRatio), math.lerp(0.3f, 0.9f, moraleRatio), 1f, 1f);
                var spawnFlags = PresentationSpawnFlags.AllowPooling | PresentationSpawnFlags.OverrideTint;

                if (hasHandle)
                {
                    var handle = _handleLookup[entity];
                    if (handle.DescriptorHash != descriptorHash)
                    {
                        recycleBuffer.Add(new PresentationRecycleRequest { Target = entity });
                    }

                    continue;
                }

                spawnBuffer.Add(new PresentationSpawnRequest
                {
                    Target = entity,
                    DescriptorHash = descriptorHash,
                    Position = position,
                    Rotation = rotation,
                    ScaleMultiplier = 1f,
                    Tint = tint,
                    VariantSeed = math.hash(new uint2((uint)aggregateRef.ValueRO.MemberCount, (uint)crewDataRef.ValueRO.Duty)),
                    Flags = spawnFlags
                });
            }
        }

        public void OnDestroy(ref SystemState state)
        {
        }
    }

    internal static class Space4XCrewDescriptorHashes
    {
        private static readonly Unity.Entities.Hash128 Idle = Compute("space4x.crew.idle");
        private static readonly Unity.Entities.Hash128 Docked = Compute("space4x.crew.docked");
        private static readonly Unity.Entities.Hash128 Sortie = Compute("space4x.crew.sortie");
        private static readonly Unity.Entities.Hash128 Combat = Compute("space4x.crew.combat");
        private static readonly Unity.Entities.Hash128 Transfer = Compute("space4x.crew.transfer");

        public static Unity.Entities.Hash128 Resolve(Space4XCrewDuty duty)
        {
            return duty switch
            {
                Space4XCrewDuty.Docked => Docked,
                Space4XCrewDuty.Sortie => Sortie,
                Space4XCrewDuty.Combat => Combat,
                Space4XCrewDuty.Transfer => Transfer,
                _ => Idle
            };
        }

        private static Unity.Entities.Hash128 Compute(string key)
        {
            return PresentationKeyUtility.TryParseKey(key, out var hash, out _)
                ? hash
                : default;
        }
    }
}
