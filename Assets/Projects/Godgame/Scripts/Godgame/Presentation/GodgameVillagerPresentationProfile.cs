using System;
using PureDOTS.Runtime.Components;
using Unity.Collections;
using Unity.Entities;

namespace Godgame.Presentation
{
    public enum GodgameVillagerRace : byte
    {
        Human = 0,
        Beast = 1,
        Spirit = 2
    }

    public enum GodgameSocialStrata : byte
    {
        Commoner = 0,
        Artisan = 1,
        Noble = 2,
        Elite = 3,
        Outcast = 4
    }

    [Flags]
    public enum GodgamePresentationStateFlags : byte
    {
        None = 0,
        Recruited = 1 << 0,
        Adventuring = 1 << 1,
        RepresentingGuild = 1 << 2,
        RepresentingDynasty = 1 << 3,
        RepresentingCompany = 1 << 4
    }

    /// <summary>
    /// Describes the visual profile for a villager so presentation can vary by race/culture/strata.
    /// </summary>
    public struct GodgameVillagerPresentationProfile : IComponentData
    {
        public GodgameVillagerRace Race;
        public GodgameSocialStrata SocialStrata;
        public FixedString64Bytes CultureId;
    }

    /// <summary>
    /// Runtime presentation state flags (recruited, adventuring, etc.)
    /// </summary>
    public struct GodgamePresentationState : IComponentData
    {
        public GodgamePresentationStateFlags Flags;

        public bool HasFlag(GodgamePresentationStateFlags flag) => (Flags & flag) != 0;
        public void SetFlag(GodgamePresentationStateFlags flag, bool enabled)
        {
            if (enabled)
            {
                Flags |= flag;
            }
            else
            {
                Flags &= ~flag;
            }
        }
    }

    /// <summary>
    /// Optional affiliation descriptors that override visuals when villagers represent larger organizations.
    /// </summary>
    public struct GodgamePresentationAffiliation : IComponentData
    {
        public Unity.Entities.Hash128 GuildDescriptor;
        public Unity.Entities.Hash128 DynastyDescriptor;
        public Unity.Entities.Hash128 CompanyDescriptor;

        public bool HasGuild => GuildDescriptor.IsValid;
        public bool HasDynasty => DynastyDescriptor.IsValid;
        public bool HasCompany => CompanyDescriptor.IsValid;
    }
}
