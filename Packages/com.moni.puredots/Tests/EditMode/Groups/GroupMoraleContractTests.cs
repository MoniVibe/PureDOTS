using NUnit.Framework;
using PureDOTS.Runtime.Groups;

namespace PureDOTS.Tests.EditMode.Groups
{
    public class GroupMoraleContractTests
    {
        [Test]
        public void NormalizeMorale01_HandlesBothNormalizedAndAbsoluteScales()
        {
            Assert.AreEqual(0.75f, GroupMoraleContract.NormalizeMorale01(0.75f), 0.0001f);
            Assert.AreEqual(0.8f, GroupMoraleContract.NormalizeMorale01(800f), 0.0001f);
            Assert.AreEqual(1f, GroupMoraleContract.NormalizeMorale01(1600f), 0.0001f);
        }

        [Test]
        public void ResolvePhase_MapsThresholdBandsDeterministically()
        {
            var profile = GroupMoraleContractProfile.Default;

            Assert.AreEqual(GroupMoralePhase.Routed, GroupMoraleContract.ResolvePhase(0.1f, in profile));
            Assert.AreEqual(GroupMoralePhase.Breaking, GroupMoraleContract.ResolvePhase(0.28f, in profile));
            Assert.AreEqual(GroupMoralePhase.Strained, GroupMoraleContract.ResolvePhase(0.4f, in profile));
            Assert.AreEqual(GroupMoralePhase.Steady, GroupMoraleContract.ResolvePhase(0.6f, in profile));
            Assert.AreEqual(GroupMoralePhase.Resilient, GroupMoraleContract.ResolvePhase(0.9f, in profile));
        }

        [Test]
        public void ResolveIntent_BreakingWithLowCohesion_ChoosesSplit()
        {
            var profile = GroupMoraleContractProfile.Default;
            var intent = GroupMoraleContract.ResolveIntent(
                GroupMoralePhase.Breaking,
                cohesion01: 0.2f,
                anchorSecurity01: 0.55f,
                commitment01: 0.6f,
                in profile);

            Assert.AreEqual(GroupMoraleIntent.SplitAndStabilize, intent);
        }

        [Test]
        public void ResolveIntent_BreakingWithStableCohesion_SeeksAnchor()
        {
            var profile = GroupMoraleContractProfile.Default;
            var intent = GroupMoraleContract.ResolveIntent(
                GroupMoralePhase.Breaking,
                cohesion01: 0.7f,
                anchorSecurity01: 0.65f,
                commitment01: 0.6f,
                in profile);

            Assert.AreEqual(GroupMoraleIntent.SeekAnchor, intent);
        }

        [Test]
        public void ShouldRejoin_RequiresSteadySignalsAndLowPressure()
        {
            var profile = GroupMoraleContractProfile.Default;

            Assert.IsTrue(GroupMoraleContract.ShouldRejoin(
                GroupMoralePhase.Steady,
                cohesion01: 0.8f,
                morale01: 0.8f,
                commitment01: 0.75f,
                pressure01: 0.2f,
                in profile));

            Assert.IsFalse(GroupMoraleContract.ShouldRejoin(
                GroupMoralePhase.Steady,
                cohesion01: 0.8f,
                morale01: 0.8f,
                commitment01: 0.75f,
                pressure01: 0.8f,
                in profile));
        }
    }
}
