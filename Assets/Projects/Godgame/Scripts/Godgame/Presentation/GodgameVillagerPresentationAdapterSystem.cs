using PureDOTS.Runtime.Components;
using PureDOTS.Systems;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Godgame.Presentation
{
    /// <summary>
    /// Emits presentation spawn/recycle commands for Godgame villagers so visuals stay aligned with simulation state.
    /// DISABLED: Using Unity default objects for now. Re-enable when custom visuals are ready.
    /// </summary>
    [DisableAutoCreation]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(VillagerAISystem))]
    public partial struct GodgameVillagerPresentationAdapterSystem : ISystem
    {
        private EntityQuery _villagerQuery;
        private ComponentLookup<PresentationHandle> _handleLookup;
        private ComponentLookup<VillagerDisciplineState> _disciplineLookup;

        public void OnCreate(ref SystemState state)
        {
            _handleLookup = state.GetComponentLookup<PresentationHandle>();
            _disciplineLookup = state.GetComponentLookup<VillagerDisciplineState>(true);

            _villagerQuery = SystemAPI.QueryBuilder()
                .WithAll<VillagerId, LocalTransform, VillagerFlags>()
                .Build();

            state.RequireForUpdate(_villagerQuery);
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
            _disciplineLookup.Update(ref state);

            foreach (var (villagerId, transform, flags, entity) in SystemAPI
                         .Query<RefRO<VillagerId>, RefRO<LocalTransform>, RefRO<VillagerFlags>>()
                         .WithEntityAccess())
            {
                var hasHandle = _handleLookup.HasComponent(entity);
                var villagerFlags = flags.ValueRO;

                if (villagerFlags.IsDead)
                {
                    if (hasHandle)
                    {
                        recycleBuffer.Add(new PresentationRecycleRequest { Target = entity });
                    }

                    continue;
                }

                var discipline = VillagerDisciplineType.Unassigned;
                if (_disciplineLookup.HasComponent(entity))
                {
                    discipline = _disciplineLookup[entity].Value;
                }

                var descriptorHash = GodgameVillagerPresentationDescriptors.Resolve(villagerFlags, discipline);
                if (!descriptorHash.IsValid)
                {
                    continue;
                }

                if (hasHandle)
                {
                    var handle = _handleLookup[entity];
                    if (handle.DescriptorHash != descriptorHash)
                    {
                        recycleBuffer.Add(new PresentationRecycleRequest { Target = entity });
                    }

                    continue;
                }

                var spawnFlags = PresentationSpawnFlags.AllowPooling;
                var scaleMultiplier = 1f;
                if (math.abs(transform.ValueRO.Scale - 1f) > 1e-3f)
                {
                    spawnFlags |= PresentationSpawnFlags.OverrideScale;
                    scaleMultiplier = transform.ValueRO.Scale;
                }

                spawnBuffer.Add(new PresentationSpawnRequest
                {
                    Target = entity,
                    DescriptorHash = descriptorHash,
                    Position = transform.ValueRO.Position,
                    Rotation = transform.ValueRO.Rotation,
                    ScaleMultiplier = scaleMultiplier,
                    Tint = float4.zero,
                    VariantSeed = math.hash(new uint2((uint)villagerId.ValueRO.Value, (uint)discipline)),
                    Flags = spawnFlags
                });
            }
        }

        public void OnDestroy(ref SystemState state)
        {
        }
    }

    internal static class GodgameVillagerPresentationDescriptors
    {
        public static readonly Unity.Entities.Hash128 Default = Compute("godgame.villager.default");
        private static readonly Unity.Entities.Hash128 Worker = Compute("godgame.villager.worker");
        private static readonly Unity.Entities.Hash128 Warrior = Compute("godgame.villager.warrior");
        private static readonly Unity.Entities.Hash128 Worshipper = Compute("godgame.villager.worshipper");
        private static readonly Unity.Entities.Hash128 Builder = Compute("godgame.villager.builder");

        public static Unity.Entities.Hash128 Resolve(in VillagerFlags flags, VillagerDisciplineType discipline)
        {
            if (flags.IsInCombat)
            {
                return Warrior;
            }

            return discipline switch
            {
                VillagerDisciplineType.Warrior => Warrior,
                VillagerDisciplineType.Worshipper => Worshipper,
                VillagerDisciplineType.Builder => Builder,
                VillagerDisciplineType.Forester => Worker,
                VillagerDisciplineType.Farmer => Worker,
                VillagerDisciplineType.Miner => Worker,
                _ => Default
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
