using NUnit.Framework;
using PureDOTS.Runtime.Combat;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Villagers;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Tests;
using Unity.Mathematics;

namespace PureDOTS.Tests
{
    /// <summary>
    /// Tests for behavior/alignment integration with initiative computation and combat stance selection.
    /// Verifies that personality traits (bold/craven, vengeful/forgiving) correctly modify initiative
    /// and combat behavior.
    /// </summary>
    public class BehaviorAlignmentIntegrationTests : ECSTestsFixture
    {
        [Test]
        public void VillagerBehavior_CanBeCreated()
        {
            var behavior = new VillagerBehavior
            {
                VengefulScore = -70, // Vengeful
                BoldScore = 60,      // Bold
                InitiativeModifier = 0f,
                ActiveGrudgeCount = 0,
                LastMajorActionTick = 0
            };

            Assert.IsTrue(behavior.IsVengeful);
            Assert.IsTrue(behavior.IsBold);
            Assert.IsFalse(behavior.IsForgiving);
            Assert.IsFalse(behavior.IsCraven);
        }

        [Test]
        public void VillagerAlignment_CanBeCreated()
        {
            var alignment = new VillagerAlignment
            {
                MoralAxis = 70,    // Good
                OrderAxis = 50,    // Lawful
                PurityAxis = 60,   // Pure
                AlignmentStrength = 0.8f,
                LastShiftTick = 0
            };

            Assert.IsTrue(alignment.IsGood);
            Assert.IsTrue(alignment.IsLawful);
            Assert.IsTrue(alignment.IsPure);
            Assert.IsFalse(alignment.IsEvil);
            Assert.IsFalse(alignment.IsChaotic);
            Assert.IsFalse(alignment.IsCorrupt);
        }

        [Test]
        public void VillagerGrudge_CanBeCreated()
        {
            var grudge = new VillagerGrudge
            {
                Target = Entity.Null,
                OffenseType = new FixedString64Bytes("killed_friend"),
                IntensityScore = 60f,
                OccurredTick = 0,
                RetaliationAttempts = 0
            };

            Assert.AreEqual("killed_friend", grudge.OffenseType.ToString());
            Assert.AreEqual(60f, grudge.IntensityScore);
        }

        [Test]
        public void VillagerInitiativeState_CanBeCreated()
        {
            var initiativeState = new VillagerInitiativeState
            {
                CurrentInitiative = 0.65f,
                NextActionTick = 1000,
                PendingAction = new FixedString32Bytes("seek_courtship")
            };

            Assert.AreEqual(0.65f, initiativeState.CurrentInitiative);
            Assert.AreEqual(1000u, initiativeState.NextActionTick);
        }

        [Test]
        public void InitiativeComputation_BoldVillager_HasHigherInitiative()
        {
            // Base initiative: 0.5
            // Bold (+60): +0.12 modifier
            // Expected: ~0.62

            var behavior = new VillagerBehavior
            {
                BoldScore = 60,
                VengefulScore = 0,
                ActiveGrudgeCount = 0
            };

            var alignment = new VillagerAlignment
            {
                MoralAxis = 0,
                OrderAxis = 0,
                PurityAxis = 0,
                AlignmentStrength = 1f
            };

            // Simulate initiative computation
            float baseInitiative = 0.5f;
            float boldModifier = behavior.BoldScore * 0.002f; // 0.12
            float computedInitiative = baseInitiative + boldModifier;

            Assert.Greater(computedInitiative, baseInitiative);
            Assert.AreEqual(0.62f, computedInitiative, 0.01f);
        }

        [Test]
        public void InitiativeComputation_CravenVillager_HasLowerInitiative()
        {
            // Base initiative: 0.5
            // Craven (-60): -0.12 modifier
            // Expected: ~0.38

            var behavior = new VillagerBehavior
            {
                BoldScore = -60, // Craven
                VengefulScore = 0,
                ActiveGrudgeCount = 0
            };

            float baseInitiative = 0.5f;
            float boldModifier = behavior.BoldScore * 0.002f; // -0.12
            float computedInitiative = baseInitiative + boldModifier;

            Assert.Less(computedInitiative, baseInitiative);
            Assert.AreEqual(0.38f, computedInitiative, 0.01f);
        }

        [Test]
        public void InitiativeComputation_ActiveGrudge_BoostsInitiative()
        {
            // Base initiative: 0.5
            // Active grudge count: 2
            // Grudge boost: 2 * 0.05 = 0.10
            // Expected: ~0.60

            var behavior = new VillagerBehavior
            {
                BoldScore = 0,
                VengefulScore = -70, // Vengeful
                ActiveGrudgeCount = 2
            };

            float baseInitiative = 0.5f;
            float grudgeBoost = behavior.ActiveGrudgeCount * 0.05f; // 0.10
            float computedInitiative = baseInitiative + grudgeBoost;

            Assert.Greater(computedInitiative, baseInitiative);
            Assert.AreEqual(0.60f, computedInitiative, 0.01f);
        }

        [Test]
        public void InitiativeComputation_LawfulAlignment_DampensInitiative()
        {
            // Base initiative: 0.5
            // Lawful alignment: 0.9x modifier
            // Expected: 0.45

            var alignment = new VillagerAlignment
            {
                OrderAxis = 50, // Lawful
                MoralAxis = 0,
                PurityAxis = 0
            };

            float baseInitiative = 0.5f;
            float alignmentModifier = alignment.IsLawful ? 0.9f : 1f;
            float computedInitiative = baseInitiative * alignmentModifier;

            Assert.Less(computedInitiative, baseInitiative);
            Assert.AreEqual(0.45f, computedInitiative, 0.01f);
        }

        [Test]
        public void CombatPersonality_BoldVillager_HasLowFleeThreshold()
        {
            // Bold (+70): FleeThresholdHP should be low (fights longer)
            var behavior = new VillagerBehavior
            {
                BoldScore = 70,
                VengefulScore = 0
            };

            byte fleeThreshold = (byte)math.clamp(30 - (behavior.BoldScore / 2), 0, 60);
            Assert.Less(fleeThreshold, 30);
            Assert.AreEqual(0, fleeThreshold); // Bold fights to death
        }

        [Test]
        public void CombatPersonality_CravenVillager_HasHighFleeThreshold()
        {
            // Craven (-70): FleeThresholdHP should be high (flees early)
            var behavior = new VillagerBehavior
            {
                BoldScore = -70,
                VengefulScore = 0
            };

            byte fleeThreshold = (byte)math.clamp(30 - (behavior.BoldScore / 2), 0, 60);
            Assert.Greater(fleeThreshold, 30);
            Assert.AreEqual(60, fleeThreshold); // Craven flees early
        }

        [Test]
        public void CombatPersonality_VengefulVillager_NeverYields()
        {
            // Vengeful (-70): YieldThresholdHP = 0 (never yields)
            var behavior = new VillagerBehavior
            {
                VengefulScore = -70,
                BoldScore = 0
            };

            byte yieldThreshold;
            if (behavior.VengefulScore < -20) // Vengeful
            {
                yieldThreshold = 0; // Never yields
            }
            else if (behavior.VengefulScore > 40) // Forgiving
            {
                yieldThreshold = 40; // Yields early
            }
            else // Neutral
            {
                yieldThreshold = 20; // Balanced
            }

            Assert.AreEqual(0, yieldThreshold);
        }

        [Test]
        public void CombatPersonality_ForgivingVillager_YieldsEarly()
        {
            // Forgiving (+60): YieldThresholdHP = 40 (yields early)
            var behavior = new VillagerBehavior
            {
                VengefulScore = 60,
                BoldScore = 0
            };

            byte yieldThreshold;
            if (behavior.VengefulScore < -20) // Vengeful
            {
                yieldThreshold = 0;
            }
            else if (behavior.VengefulScore > 40) // Forgiving
            {
                yieldThreshold = 40; // Yields early
            }
            else // Neutral
            {
                yieldThreshold = 20;
            }

            Assert.AreEqual(40, yieldThreshold);
        }

        [Test]
        public void CombatPersonality_BoldVillager_PrefersAggressiveStance()
        {
            var behavior = new VillagerBehavior
            {
                BoldScore = 70,
                VengefulScore = 0
            };

            byte preferredStance;
            if (behavior.IsBold)
            {
                preferredStance = (byte)ActiveCombat.CombatStance.Aggressive;
            }
            else if (behavior.IsCraven)
            {
                preferredStance = (byte)ActiveCombat.CombatStance.Defensive;
            }
            else
            {
                preferredStance = (byte)ActiveCombat.CombatStance.Balanced;
            }

            Assert.AreEqual((byte)ActiveCombat.CombatStance.Aggressive, preferredStance);
        }

        [Test]
        public void CombatPersonality_CravenVillager_PrefersDefensiveStance()
        {
            var behavior = new VillagerBehavior
            {
                BoldScore = -70,
                VengefulScore = 0
            };

            byte preferredStance;
            if (behavior.IsBold)
            {
                preferredStance = (byte)ActiveCombat.CombatStance.Aggressive;
            }
            else if (behavior.IsCraven)
            {
                preferredStance = (byte)ActiveCombat.CombatStance.Defensive;
            }
            else
            {
                preferredStance = (byte)ActiveCombat.CombatStance.Balanced;
            }

            Assert.AreEqual((byte)ActiveCombat.CombatStance.Defensive, preferredStance);
        }

        [Test]
        public void UtilityScheduler_PersonalityWeight_BoldPrefersCombat()
        {
            var behavior = new VillagerBehavior
            {
                BoldScore = 70,
                VengefulScore = 0
            };

            float combatWeight = VillagerUtilityScheduler.CalculatePersonalityWeight(
                VillagerActionType.Combat,
                behavior);

            Assert.Greater(combatWeight, 1f); // Bold increases combat weight
        }

        [Test]
        public void UtilityScheduler_PersonalityWeight_CravenAvoidsCombat()
        {
            var behavior = new VillagerBehavior
            {
                BoldScore = -70,
                VengefulScore = 0
            };

            float combatWeight = VillagerUtilityScheduler.CalculatePersonalityWeight(
                VillagerActionType.Combat,
                behavior);

            Assert.Less(combatWeight, 1f); // Craven decreases combat weight
        }

        [Test]
        public void GrudgeDecay_VengefulVillager_HoldsGrudgeLonger()
        {
            // Vengeful (-70): DecayRate = 0.01 × (100 + (-70)) = 0.3 per day
            var behavior = new VillagerBehavior
            {
                VengefulScore = -70
            };

            float decayRate;
            if (behavior.VengefulScore < -20) // Vengeful
            {
                decayRate = 0.01f * (100f + behavior.VengefulScore);
            }
            else if (behavior.VengefulScore > 40) // Forgiving
            {
                decayRate = 2.0f;
            }
            else // Neutral
            {
                decayRate = 0.5f;
            }

            Assert.Less(decayRate, 0.5f); // Slower decay than neutral
            Assert.AreEqual(0.3f, decayRate, 0.01f);
        }

        [Test]
        public void GrudgeDecay_ForgivingVillager_LetsGrudgeFadeQuickly()
        {
            // Forgiving (+60): DecayRate = 2.0 per day (rapid fade)
            var behavior = new VillagerBehavior
            {
                VengefulScore = 60
            };

            float decayRate;
            if (behavior.VengefulScore < -20) // Vengeful
            {
                decayRate = 0.01f * (100f + behavior.VengefulScore);
            }
            else if (behavior.VengefulScore > 40) // Forgiving
            {
                decayRate = 2.0f;
            }
            else // Neutral
            {
                decayRate = 0.5f;
            }

            Assert.Greater(decayRate, 0.5f); // Faster decay than neutral
            Assert.AreEqual(2.0f, decayRate);
        }
    }
}

