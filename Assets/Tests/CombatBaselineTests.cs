using NUnit.Framework;
using PureDOTS.Runtime.Combat;
using Unity.Entities;

namespace PureDOTS.Tests
{
    /// <summary>
    /// Tests for PureDOTS combat baseline components.
    /// Verifies that combat components compile and can be instantiated.
    /// </summary>
    public class CombatBaselineTests
    {
        [Test]
        public void CombatStats_CanBeCreated()
        {
            var combatStats = new CombatStats
            {
                Attack = 75,
                Defense = 65,
                Morale = 80,
                AttackSpeed = 70,
                AttackDamage = 50,
                Accuracy = 80,
                CriticalChance = 15,
                Health = 100,
                CurrentHealth = 100,
                Stamina = 10,
                CurrentStamina = 10,
                SpellPower = 60,
                ManaPool = 50,
                CurrentMana = 50,
                EquippedWeapon = Entity.Null,
                EquippedArmor = Entity.Null,
                EquippedShield = Entity.Null,
                CombatExperience = 0,
                IsInCombat = false,
                CurrentOpponent = Entity.Null
            };
            
            Assert.AreEqual(75, combatStats.Attack);
            Assert.AreEqual(100, combatStats.Health);
            Assert.IsFalse(combatStats.IsInCombat);
        }
        
        [Test]
        public void BaseAttributes_CanBeCreated()
        {
            var attributes = new BaseAttributes
            {
                Strength = 70,
                Finesse = 80,
                Will = 60,
                Intelligence = 75
            };
            
            Assert.AreEqual(70, attributes.Strength);
            Assert.AreEqual(80, attributes.Finesse);
        }
        
        [Test]
        public void ActiveCombat_CanBeCreated()
        {
            var combat = new ActiveCombat
            {
                Type = ActiveCombat.CombatType.Duel,
                Combatant1 = Entity.Null,
                Combatant2 = Entity.Null,
                CombatStartTick = 0,
                CurrentRound = 1,
                Combatant1Stance = ActiveCombat.CombatStance.Balanced,
                Combatant2Stance = ActiveCombat.CombatStance.Balanced,
                Combatant1Damage = 0,
                Combatant2Damage = 0,
                IsDuelToFirstBlood = false,
                IsDuelToYield = true,
                IsDuelToDeath = false,
                WitnessEntity = Entity.Null,
                WitnessCount = 0
            };
            
            Assert.AreEqual(ActiveCombat.CombatType.Duel, combat.Type);
            Assert.IsTrue(combat.IsDuelToYield);
        }
        
        [Test]
        public void Weapon_CanBeCreated()
        {
            var weapon = new Weapon
            {
                Type = Weapon.WeaponType.Sword,
                BaseDamage = 25,
                HitBonus = 5,
                ArmorPenetration = 20,
                CriticalChanceBonus = 5,
                Durability = 1000,
                MaxDurability = 1000,
                Value = 500,
                WeaponName = new Unity.Collections.FixedString64Bytes("Longsword")
            };
            
            Assert.AreEqual(Weapon.WeaponType.Sword, weapon.Type);
            Assert.AreEqual(25, weapon.BaseDamage);
        }
        
        [Test]
        public void Armor_CanBeCreated()
        {
            var armor = new Armor
            {
                Type = Armor.ArmorType.Plate,
                ArmorValue = 30,
                DodgePenalty = -10,
                StaminaDrain = -2,
                EffectivenessVsSword = 0.7f,
                EffectivenessVsMace = 0.4f,
                EffectivenessVsArrow = 0.3f,
                Durability = 1000,
                MaxDurability = 1000,
                Value = 1000,
                ArmorName = new Unity.Collections.FixedString64Bytes("Plate Armor")
            };
            
            Assert.AreEqual(Armor.ArmorType.Plate, armor.Type);
            Assert.AreEqual(0.7f, armor.EffectivenessVsSword);
        }
    }
}

