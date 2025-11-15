using PureDOTS.Runtime.Components;
using PureDOTS.Systems;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Godgame.Presentation
{
    /// <summary>
    /// Emits presentation spawn/recycle commands for Godgame villagers so visuals stay aligned with simulation state.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(VillagerAISystem))]
    public partial struct GodgameVillagerPresentationAdapterSystem : ISystem
    {
        private EntityQuery _villagerQuery;
        private ComponentLookup<GodgamePresentationBinding> _bindingLookup;
        private ComponentLookup<VillagerDisciplineState> _disciplineLookup;
        private ComponentLookup<GodgameVillagerPresentationProfile> _profileLookup;
        private ComponentLookup<GodgamePresentationState> _stateLookup;
        private ComponentLookup<GodgamePresentationAffiliation> _affiliationLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _bindingLookup = state.GetComponentLookup<GodgamePresentationBinding>();
            _disciplineLookup = state.GetComponentLookup<VillagerDisciplineState>(true);
            _profileLookup = state.GetComponentLookup<GodgameVillagerPresentationProfile>(true);
            _stateLookup = state.GetComponentLookup<GodgamePresentationState>(true);
            _affiliationLookup = state.GetComponentLookup<GodgamePresentationAffiliation>(true);

            _villagerQuery = SystemAPI.QueryBuilder()
                .WithAll<VillagerId, LocalTransform, VillagerFlags>()
                .Build();

            state.RequireForUpdate(_villagerQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _bindingLookup.Update(ref state);
            _disciplineLookup.Update(ref state);
            _profileLookup.Update(ref state);
            _stateLookup.Update(ref state);
            _affiliationLookup.Update(ref state);

            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (villagerId, transform, flags, entity) in SystemAPI
                         .Query<RefRO<VillagerId>, RefRO<LocalTransform>, RefRO<VillagerFlags>>()
                         .WithEntityAccess())
            {
                var villagerFlags = flags.ValueRO;

                if (villagerFlags.IsDead)
                {
                    if (_bindingLookup.HasComponent(entity))
                    {
                        ecb.RemoveComponent<GodgamePresentationBinding>(entity);
                        ecb.AddComponent<GodgamePresentationDirtyTag>(entity);
                    }

                    continue;
                }

                var discipline = VillagerDisciplineType.Unassigned;
                if (_disciplineLookup.HasComponent(entity))
                {
                    discipline = _disciplineLookup[entity].Value;
                }

                var profile = _profileLookup.HasComponent(entity)
                    ? _profileLookup[entity]
                    : new GodgameVillagerPresentationProfile
                    {
                        Race = GodgameVillagerRace.Human,
                        SocialStrata = GodgameSocialStrata.Commoner,
                        CultureId = default
                    };

                var presentationState = _stateLookup.HasComponent(entity)
                    ? _stateLookup[entity]
                    : new GodgamePresentationState { Flags = GodgamePresentationStateFlags.None };

                var affiliation = _affiliationLookup.HasComponent(entity)
                    ? _affiliationLookup[entity]
                    : default;

                var descriptorHash = GodgameVillagerPresentationDescriptors.Resolve(
                    villagerFlags,
                    discipline,
                    profile,
                    presentationState,
                    affiliation);
                if (!descriptorHash.IsValid)
                {
                    continue;
                }

                bool overrideScale = math.abs(transform.ValueRO.Scale - 1f) > 1e-3f;
                var newBinding = new GodgamePresentationBinding
                {
                    Descriptor = descriptorHash,
                    PositionOffset = float3.zero,
                    RotationOffset = quaternion.identity,
                    ScaleMultiplier = overrideScale ? transform.ValueRO.Scale : 1f,
                    Tint = float4.zero,
                    VariantSeed = math.hash(new uint2((uint)villagerId.ValueRO.Value, (uint)discipline)),
                    Flags = GodgamePresentationFlagUtility.WithOverrides(false, overrideScale, false)
                };

                GodgamePresentationBindingUtility.ApplyBinding(entity, newBinding, ref _bindingLookup, ecb);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

    }

    internal static class GodgameVillagerPresentationDescriptors
    {
        private static readonly Unity.Entities.Hash128 HumanCommoner = Compute("godgame.villager.human.commoner");
        private static readonly Unity.Entities.Hash128 HumanArtisan = Compute("godgame.villager.human.artisan");
        private static readonly Unity.Entities.Hash128 HumanNoble = Compute("godgame.villager.human.noble");
        private static readonly Unity.Entities.Hash128 HumanElite = Compute("godgame.villager.human.elite");
        private static readonly Unity.Entities.Hash128 HumanOutcast = Compute("godgame.villager.human.outcast");
        private static readonly Unity.Entities.Hash128 HumanRecruit = Compute("godgame.villager.human.recruited");
        private static readonly Unity.Entities.Hash128 HumanAdventurer = Compute("godgame.villager.human.adventurer");
        private static readonly Unity.Entities.Hash128 HumanGuild = Compute("godgame.villager.human.guild");
        private static readonly Unity.Entities.Hash128 HumanDynasty = Compute("godgame.villager.human.dynasty");
        private static readonly Unity.Entities.Hash128 HumanCompany = Compute("godgame.villager.human.company");

        private static readonly Unity.Entities.Hash128 Worker = Compute("godgame.villager.worker");
        private static readonly Unity.Entities.Hash128 Warrior = Compute("godgame.villager.warrior");
        private static readonly Unity.Entities.Hash128 Worshipper = Compute("godgame.villager.worshipper");
        private static readonly Unity.Entities.Hash128 Builder = Compute("godgame.villager.builder");

        private static readonly Unity.Entities.Hash128 BeastDefault = Compute("godgame.villager.animal.generic");
        private static readonly Unity.Entities.Hash128 BeastAdventurer = Compute("godgame.villager.animal.adventurer");

        private static readonly Unity.Entities.Hash128 SpiritDefault = Compute("godgame.villager.spirit.default");
        private static readonly Unity.Entities.Hash128 SpiritAdventurer = Compute("godgame.villager.spirit.adventurer");

        private static readonly Unity.Entities.Hash128[] HumanStrataDescriptors =
        {
            HumanCommoner,
            HumanArtisan,
            HumanNoble,
            HumanElite,
            HumanOutcast
        };

        public static Unity.Entities.Hash128 Resolve(
            in VillagerFlags flags,
            VillagerDisciplineType discipline,
            in GodgameVillagerPresentationProfile profile,
            in GodgamePresentationState state,
            in GodgamePresentationAffiliation affiliation)
        {
            if (profile.Race == GodgameVillagerRace.Beast)
            {
                if (state.HasFlag(GodgamePresentationStateFlags.Adventuring) || flags.IsInCombat)
                {
                    return BeastAdventurer.IsValid ? BeastAdventurer : BeastDefault;
                }

                return BeastDefault;
            }

            if (profile.Race == GodgameVillagerRace.Spirit)
            {
                if (state.HasFlag(GodgamePresentationStateFlags.Adventuring))
                {
                    return SpiritAdventurer.IsValid ? SpiritAdventurer : SpiritDefault;
                }

                return SpiritDefault;
            }

            if (flags.IsInCombat)
            {
                return Warrior;
            }

            if (state.HasFlag(GodgamePresentationStateFlags.Adventuring))
            {
                return HumanAdventurer;
            }

            if (state.HasFlag(GodgamePresentationStateFlags.Recruited))
            {
                return HumanRecruit;
            }

            if (state.HasFlag(GodgamePresentationStateFlags.RepresentingGuild))
            {
                if (affiliation.GuildDescriptor.IsValid)
                {
                    return affiliation.GuildDescriptor;
                }

                return HumanGuild;
            }

            if (state.HasFlag(GodgamePresentationStateFlags.RepresentingDynasty))
            {
                if (affiliation.DynastyDescriptor.IsValid)
                {
                    return affiliation.DynastyDescriptor;
                }

                return HumanDynasty;
            }

            if (state.HasFlag(GodgamePresentationStateFlags.RepresentingCompany))
            {
                if (affiliation.CompanyDescriptor.IsValid)
                {
                    return affiliation.CompanyDescriptor;
                }

                return HumanCompany;
            }

            switch (discipline)
            {
                case VillagerDisciplineType.Warrior:
                    return Warrior;
                case VillagerDisciplineType.Worshipper:
                    return Worshipper;
                case VillagerDisciplineType.Builder:
                    return Builder;
                case VillagerDisciplineType.Forester:
                case VillagerDisciplineType.Farmer:
                case VillagerDisciplineType.Miner:
                    return Worker;
            }

            return ResolveStrata(profile);
        }

        private static Unity.Entities.Hash128 ResolveStrata(in GodgameVillagerPresentationProfile profile)
        {
            int index = math.clamp((int)profile.SocialStrata, 0, HumanStrataDescriptors.Length - 1);
            var descriptor = HumanStrataDescriptors[index];
            return descriptor.IsValid ? descriptor : HumanCommoner;
        }

        private static Unity.Entities.Hash128 Compute(string key)
        {
            return PresentationKeyUtility.TryParseKey(key, out var hash, out _)
                ? hash
                : default;
        }
    }
}
