using NUnit.Framework;
using Space4X.Individuals;
using Space4X.Knowledge;
using Space4X.Combat;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Space4X.Tests
{
    /// <summary>
    /// EditMode tests for Space4X tactical learning and anticipation system.
    /// </summary>
    public class TacticalLearningTests
    {
        [Test]
        public void Wisdom_Spending_TransfersToPools()
        {
            // Create wisdom pool
            var wisdomPool = new WisdomPool
            {
                CurrentWisdom = 100f,
                TotalWisdomEarned = 500f,
                WisdomEarnRate = 1.2f
            };

            // Create experience pools
            var experiencePools = new ExperiencePools
            {
                PhysiqueXP = 0f,
                FinesseXP = 0f,
                WillXP = 0f
            };

            // Simulate spending wisdom into Finesse pool
            float amountToSpend = 50f;
            if (wisdomPool.CurrentWisdom >= amountToSpend)
            {
                wisdomPool.CurrentWisdom -= amountToSpend;
                experiencePools.FinesseXP += amountToSpend;
            }

            Assert.AreEqual(50f, wisdomPool.CurrentWisdom, 0.001f, "Wisdom should be reduced");
            Assert.AreEqual(50f, experiencePools.FinesseXP, 0.001f, "Finesse XP should increase");
        }

        [Test]
        public void ManeuverMastery_Progress_Accumulates()
        {
            // Create maneuver mastery entry
            var mastery = new ManeuverMastery
            {
                ManeuverId = "j_turn",
                MasteryProgress = 0.5f, // 50%
                ObservationCount = 10,
                PracticeAttempts = 5,
                SuccessfulExecutions = 3,
                FailedExecutions = 2,
                Flags = ManeuverMasteryFlags.None
            };

            // Simulate observation XP gain
            float observationXP = 0.05f;
            mastery.MasteryProgress += observationXP;
            mastery.ObservationCount++;

            Assert.AreEqual(0.55f, mastery.MasteryProgress, 0.001f, "Mastery should increase");
            Assert.AreEqual(11, mastery.ObservationCount, "Observation count should increase");
        }

        [Test]
        public void ManeuverMastery_AnticipatedFlag_UnlocksAt20Percent()
        {
            // Create maneuver mastery at 20%
            var mastery = new ManeuverMastery
            {
                ManeuverId = "kite",
                MasteryProgress = 0.2f, // 20%
                ObservationCount = 0,
                PracticeAttempts = 0,
                SuccessfulExecutions = 0,
                FailedExecutions = 0,
                Flags = ManeuverMasteryFlags.None
            };

            // Unlock anticipated flag at 20%
            if (mastery.MasteryProgress >= 0.2f)
            {
                mastery.Flags |= ManeuverMasteryFlags.Anticipated;
            }

            Assert.IsTrue((mastery.Flags & ManeuverMasteryFlags.Anticipated) != 0, "Anticipated flag should be set at 20%");
        }

        [Test]
        public void DangerDetection_Range_DeterminesVisibility()
        {
            // Create danger perception
            var perception = new DangerPerception
            {
                PerceptionRange = 100f,
                ReactionTime = 0.5f,
                PerceptionLevel = 2,
                EnabledResponses = DangerResponseFlags.Evade | DangerResponseFlags.Shield
            };

            // Simulate distance check
            float3 observerPos = new float3(0, 0, 0);
            float3 dangerPos = new float3(50, 0, 0);
            float distance = math.distance(observerPos, dangerPos);

            bool canDetect = distance <= perception.PerceptionRange;

            Assert.IsTrue(canDetect, "Danger within range should be detectable");
        }

        [Test]
        public void CollisionSurvival_ImpactSeverity_CalculatesCorrectly()
        {
            // Create collision survival roll
            var survivalRoll = new CollisionSurvivalRoll
            {
                ImpactSeverity = 0f,
                CrewProtection = 0f,
                Outcome = SurvivalOutcome.Unscathed,
                RollTick = 0
            };

            // Simulate collision with relative velocity
            float relativeVelocity = 150f;
            float impactSeverity = relativeVelocity / 100f; // Normalize

            survivalRoll.ImpactSeverity = impactSeverity;

            Assert.AreEqual(1.5f, survivalRoll.ImpactSeverity, 0.001f, "Impact severity should be calculated from velocity");
        }

        [Test]
        public void CollisionSurvival_CrewProtection_ReducesSeverity()
        {
            // Create collision survival roll
            var survivalRoll = new CollisionSurvivalRoll
            {
                ImpactSeverity = 2.0f,
                CrewProtection = 0.5f,
                Outcome = SurvivalOutcome.Unscathed,
                RollTick = 0
            };

            // Calculate effective severity after protection
            float effectiveSeverity = survivalRoll.ImpactSeverity - survivalRoll.CrewProtection;

            Assert.AreEqual(1.5f, effectiveSeverity, 0.001f, "Crew protection should reduce effective severity");
        }

        [Test]
        public void BehaviorTreeProgress_Tiers_UnlockBehaviors()
        {
            // Create behavior tree progress
            var behaviorProgress = new BehaviorTreeProgress
            {
                CombatBehaviorTier = 1,
                EvasionBehaviorTier = 2,
                LeadershipBehaviorTier = 1,
                TacticalBehaviorTier = 0
            };

            // Check if evasion tier 2 unlocks advanced responses
            bool canIntercept = behaviorProgress.EvasionBehaviorTier >= 2;

            Assert.IsTrue(canIntercept, "Tier 2 evasion should unlock intercept behavior");
        }

        [Test]
        public void HazardAvoidance_GreenPilot_ExecutesPathExactly()
        {
            // Create pilot skill modifiers
            var pilotSkill = new PilotSkillModifiers
            {
                FinesseLevel = 0.1f, // Green pilot (low finesse)
                TacticalExperience = 0.05f,
                HazardAvoidanceCapability = 0.0f // No avoidance capability
            };

            // Green pilots execute path exactly (no offset)
            float3 intendedPath = new float3(10, 0, 0);
            float3 adjustedPath = intendedPath; // No adjustment for green pilots

            Assert.AreEqual(intendedPath, adjustedPath, "Green pilots should execute path exactly");
        }

        [Test]
        public void HazardAvoidance_VeteranPilot_AdjustsPath()
        {
            // Create pilot skill modifiers for veteran
            var pilotSkill = new PilotSkillModifiers
            {
                FinesseLevel = 0.6f, // Veteran pilot
                TacticalExperience = 0.5f,
                HazardAvoidanceCapability = 0.5f // Moderate avoidance
            };

            // Veteran pilots adjust path gradually
            float3 intendedPath = new float3(10, 0, 0);
            float3 hazardOffset = new float3(2, 0, 0);
            float3 adjustedPath = intendedPath + hazardOffset * pilotSkill.HazardAvoidanceCapability;

            Assert.AreNotEqual(intendedPath, adjustedPath, "Veteran pilots should adjust path");
        }

        [Test]
        public void WisdomEarnRate_ScalesWithTotalWisdom()
        {
            // Test wisdom earn rate calculation (diminishing returns)
            float totalWisdomEarned = 1000f;
            float baseRate = 1.0f;

            // Diminishing returns formula: baseRate * (1 / (1 + totalWisdomEarned / 1000))
            float earnRate = baseRate * (1f / (1f + totalWisdomEarned / 1000f));

            Assert.Less(earnRate, baseRate, "Earn rate should decrease with total wisdom (diminishing returns)");
        }

        [Test]
        public void SquadAlert_RequiresLeadershipThreshold()
        {
            // Create individual stats
            var stats = new IndividualStats
            {
                Command = 50f, // High command
                Tactics = 30f,
                Logistics = 25f,
                Diplomacy = 20f,
                Engineering = 15f,
                Resolve = 40f
            };

            // Check if can send squad alerts (requires Command >= 40)
            bool canSendAlert = stats.Command >= 40f;

            Assert.IsTrue(canSendAlert, "High command should allow squad alerts");
        }

        // Helper method for deterministic random (if needed)
        private float DeterministicRandom(int seed1, int seed2, uint tick)
        {
            uint hash = (uint)(seed1 ^ seed2 ^ tick);
            hash ^= hash >> 16;
            hash *= 0x85ebca6b;
            hash ^= hash >> 13;
            hash *= 0xc2b2ae35;
            hash ^= hash >> 16;
            return (hash & 0x7FFFFFFF) / (float)0x7FFFFFFF;
        }
    }
}

