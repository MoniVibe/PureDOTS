using NUnit.Framework;
using Space4X.Individuals;

namespace Space4X.Tests.Individuals
{
    /// <summary>
    /// EditMode tests for individual stats components.
    /// </summary>
    public class IndividualStatsTests
    {
        [Test]
        public void IndividualStats_Default_InitializesToZero()
        {
            var stats = new IndividualStats();

            Assert.AreEqual(0, stats.Command);
            Assert.AreEqual(0, stats.Tactics);
            Assert.AreEqual(0, stats.Logistics);
            Assert.AreEqual(0, stats.Diplomacy);
            Assert.AreEqual(0, stats.Engineering);
            Assert.AreEqual(0, stats.Resolve);
        }

        [Test]
        public void PhysiqueFinesseWill_Default_InitializesToZero()
        {
            var attrs = new PhysiqueFinesseWill();

            Assert.AreEqual(0, attrs.Physique);
            Assert.AreEqual(0, attrs.Finesse);
            Assert.AreEqual(0, attrs.Will);
            Assert.AreEqual(0, attrs.PhysiqueInclination);
            Assert.AreEqual(0, attrs.FinesseInclination);
            Assert.AreEqual(0, attrs.WillInclination);
            Assert.AreEqual(0f, attrs.GeneralXP);
        }

        [Test]
        public void PreordainProfile_Default_InitializesToCombatAce()
        {
            var profile = new PreordainProfile();
            Assert.AreEqual(PreordainTrack.CombatAce, profile.Track);
        }
    }
}

