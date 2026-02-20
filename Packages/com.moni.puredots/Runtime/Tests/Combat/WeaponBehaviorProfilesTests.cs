using NUnit.Framework;
using PureDOTS.Runtime.Combat;

namespace PureDOTS.Tests.Combat
{
    public class WeaponBehaviorProfilesTests
    {
        [Test]
        public void DissipativeProfiles_FallOffOverDistance()
        {
            var plasma = WeaponBehaviorProfiles.Resolve(WeaponBehaviorArchetype.DissipativePlasma);
            var flame = WeaponBehaviorProfiles.Resolve(WeaponBehaviorArchetype.DissipativeFlame);

            var plasmaNear = WeaponBehaviorProfiles.ResolveDistanceDamageMultiplier(10f, 200f, 1f, plasma);
            var plasmaFar = WeaponBehaviorProfiles.ResolveDistanceDamageMultiplier(180f, 200f, 1f, plasma);
            var flameNear = WeaponBehaviorProfiles.ResolveDistanceDamageMultiplier(5f, 120f, 1f, flame);
            var flameFar = WeaponBehaviorProfiles.ResolveDistanceDamageMultiplier(100f, 120f, 1f, flame);

            Assert.Greater(plasmaNear, plasmaFar);
            Assert.Greater(flameNear, flameFar);
        }

        [Test]
        public void GuidedProfiles_UseMoreAmmoAndLowerHitChance()
        {
            var missile = WeaponBehaviorProfiles.Resolve(WeaponBehaviorArchetype.GuidedMissile);
            var heavy = WeaponBehaviorProfiles.Resolve(WeaponBehaviorArchetype.GuidedHeavy);

            var missileAmmo = WeaponBehaviorProfiles.ResolveAmmoPerShot(1, missile);
            var heavyAmmo = WeaponBehaviorProfiles.ResolveAmmoPerShot(1, heavy);
            var missileHitMul = WeaponBehaviorProfiles.ResolveHitChanceMultiplier(missile);
            var heavyHitMul = WeaponBehaviorProfiles.ResolveHitChanceMultiplier(heavy);

            Assert.GreaterOrEqual(missileAmmo, 2);
            Assert.GreaterOrEqual(heavyAmmo, missileAmmo);
            Assert.Less(missileHitMul, 1f);
            Assert.Less(heavyHitMul, missileHitMul);
        }

        [Test]
        public void EnergyProfile_AddsHeatAndUsesNoAmmo()
        {
            var energy = WeaponBehaviorProfiles.Resolve(WeaponBehaviorArchetype.Energy);
            var heat = WeaponBehaviorProfiles.ResolveHeatCost(1f, energy);
            var ammo = WeaponBehaviorProfiles.ResolveAmmoPerShot(2, energy);

            Assert.Greater(heat, 1f);
            Assert.AreEqual(0, ammo);
        }
    }
}
