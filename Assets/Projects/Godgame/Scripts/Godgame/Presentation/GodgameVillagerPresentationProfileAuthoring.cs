using PureDOTS.Runtime.Components;
using Unity.Entities;
using UnityEngine;

namespace Godgame.Presentation
{
    [DisallowMultipleComponent]
    public sealed class GodgameVillagerPresentationProfileAuthoring : MonoBehaviour
    {
        [Header("Profile")]
        public GodgameVillagerRace race = GodgameVillagerRace.Human;
        public GodgameSocialStrata socialStrata = GodgameSocialStrata.Commoner;
        [Tooltip("Optional culture identifier (e.g., 'culture.lowlands').")]
        public string cultureId = "culture.default";

        [Header("Initial State")]
        public bool startRecruited;
        public bool startAdventuring;

        [Header("Affiliations (optional descriptor keys)")]
        public string guildDescriptorKey = "godgame.villager.human.guild";
        public string dynastyDescriptorKey = "godgame.villager.human.dynasty";
        public string companyDescriptorKey = "godgame.villager.human.company";
        public bool representsGuild;
        public bool representsDynasty;
        public bool representsCompany;

        public sealed class Baker : Unity.Entities.Baker<GodgameVillagerPresentationProfileAuthoring>
        {
            public override void Bake(GodgameVillagerPresentationProfileAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic | TransformUsageFlags.Renderable);

                var profile = new GodgameVillagerPresentationProfile
                {
                    Race = authoring.race,
                    SocialStrata = authoring.socialStrata,
                    CultureId = new Unity.Collections.FixedString64Bytes(
                        string.IsNullOrWhiteSpace(authoring.cultureId)
                            ? "culture.default"
                            : authoring.cultureId.Trim().ToLowerInvariant())
                };
                AddComponent(entity, profile);

                var flags = GodgamePresentationStateFlags.None;
                if (authoring.startRecruited)
                {
                    flags |= GodgamePresentationStateFlags.Recruited;
                }

                if (authoring.startAdventuring)
                {
                    flags |= GodgamePresentationStateFlags.Adventuring;
                }

                if (authoring.representsGuild)
                {
                    flags |= GodgamePresentationStateFlags.RepresentingGuild;
                }

                if (authoring.representsDynasty)
                {
                    flags |= GodgamePresentationStateFlags.RepresentingDynasty;
                }

                if (authoring.representsCompany)
                {
                    flags |= GodgamePresentationStateFlags.RepresentingCompany;
                }

                AddComponent(entity, new GodgamePresentationState { Flags = flags });

                var affiliation = new GodgamePresentationAffiliation
                {
                    GuildDescriptor = ParseDescriptor(authoring.representsGuild, authoring.guildDescriptorKey, authoring, "guild"),
                    DynastyDescriptor = ParseDescriptor(authoring.representsDynasty, authoring.dynastyDescriptorKey, authoring, "dynasty"),
                    CompanyDescriptor = ParseDescriptor(authoring.representsCompany, authoring.companyDescriptorKey, authoring, "company")
                };

                if (affiliation.GuildDescriptor.IsValid ||
                    affiliation.DynastyDescriptor.IsValid ||
                    affiliation.CompanyDescriptor.IsValid)
                {
                    AddComponent(entity, affiliation);
                }
            }

            private static Unity.Entities.Hash128 ParseDescriptor(bool enabled, string key, Component context, string fieldLabel)
            {
                if (!enabled)
                {
                    return default;
                }

                if (string.IsNullOrWhiteSpace(key))
                {
                    Debug.LogWarning($"GodgameVillagerPresentationProfileAuthoring '{context.name}' is set to represent a {fieldLabel} but no descriptor key was provided.");
                    return default;
                }

                if (!PresentationKeyUtility.TryParseKey(key, out var hash, out _))
                {
                    Debug.LogWarning($"GodgameVillagerPresentationProfileAuthoring '{context.name}' has an invalid {fieldLabel} key '{key}'.");
                    return default;
                }

                return hash;
            }
        }
    }
}
