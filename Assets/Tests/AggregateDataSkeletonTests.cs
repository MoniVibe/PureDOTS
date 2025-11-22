using NUnit.Framework;
using PureDOTS.Runtime.Aggregates;
using Unity.Entities;

namespace PureDOTS.Tests
{
    /// <summary>
    /// Tests for PureDOTS aggregate (band/guild) data skeletons.
    /// Verifies that aggregate components compile and can be instantiated.
    /// </summary>
    public class AggregateDataSkeletonTests
    {
        [Test]
        public void Band_CanBeCreated()
        {
            var band = new Band
            {
                BandName = new Unity.Collections.FixedString64Bytes("Test Band"),
                Purpose = BandPurpose.Military_Warband,
                LeaderEntity = Entity.Null,
                FormationTick = 0,
                MemberCount = 0
            };
            
            Assert.AreEqual(BandPurpose.Military_Warband, band.Purpose);
            Assert.AreEqual(0, band.MemberCount);
        }
        
        [Test]
        public void BandMembership_CanBeCreated()
        {
            var membership = new BandMembership
            {
                BandEntity = Entity.Null,
                JoinedTick = 0,
                Role = BandRole.Combatant
            };
            
            Assert.AreEqual(BandRole.Combatant, membership.Role);
        }
        
        [Test]
        public void Guild_CanBeCreated()
        {
            var guild = new Guild
            {
                Type = Guild.GuildType.Heroes,
                GuildName = new Unity.Collections.FixedString64Bytes("Lightbringers"),
                FoundedTick = 0,
                HomeVillage = Entity.Null,
                HeadquartersPosition = Unity.Mathematics.float3.zero,
                MemberCount = 0,
                AverageMemberLevel = 0f,
                TotalExperience = 0,
                ReputationScore = 50,
                CurrentMission = new Unity.Collections.FixedString64Bytes("None")
            };
            
            Assert.AreEqual(Guild.GuildType.Heroes, guild.Type);
            Assert.AreEqual(50, guild.ReputationScore);
        }
        
        [Test]
        public void GuildLeadership_CanBeCreated()
        {
            var leadership = new GuildLeadership
            {
                Governance = GuildLeadership.GovernanceType.Democratic,
                GuildMasterEntity = Entity.Null,
                MasterElectedTick = 0,
                QuartermasterEntity = Entity.Null,
                RecruiterEntity = Entity.Null,
                DiplomatEntity = Entity.Null,
                WarMasterEntity = Entity.Null,
                SpyMasterEntity = Entity.Null,
                VoteInProgress = false,
                VoteProposal = new Unity.Collections.FixedString64Bytes(""),
                VoteEndTick = 0
            };
            
            Assert.AreEqual(GuildLeadership.GovernanceType.Democratic, leadership.Governance);
            Assert.IsFalse(leadership.VoteInProgress);
        }
        
        [Test]
        public void BandFormationCandidate_CanBeCreated()
        {
            var candidate = new BandFormationCandidate
            {
                InitiatorEntity = Entity.Null,
                SharedGoal = new Unity.Collections.FixedString128Bytes("Hunt the dragon"),
                ProposedTick = 0,
                ProspectiveMemberCount = 0
            };
            
            Assert.AreEqual(0, candidate.ProspectiveMemberCount);
        }
    }
}

