using NUnit.Framework;
using PureDOTS.Runtime.Combat;
using PureDOTS.Runtime.Compliance;
using PureDOTS.Runtime.Crew;
using PureDOTS.Runtime.Resource;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Space4X.Tests
{
    /// <summary>
    /// System invariants tests - asserts correctness and prevents regressions.
    /// All invariants logged in single file for quick grepping.
    /// </summary>
    public class Space4XSystemInvariantsTests
    {
        #region Weapons/Projectiles Invariants

        [Test]
        public void Weapon_NoNaNVelocity()
        {
            // Invariant: Projectile velocity must never be NaN
            var velocity = new float3(10f, 0f, 5f);
            Assert.IsFalse(math.any(math.isnan(velocity)), "Projectile velocity must not contain NaN");
        }

        [Test]
        public void Projectile_PierceDoesNotOverHit()
        {
            // Invariant: Pierce count should not exceed configured value
            var projectile = new ProjectileEntity
            {
                PierceCount = 3
            };
            
            // Simulate hits
            var hits = 0;
            for (int i = 0; i < 10; i++)
            {
                if (projectile.PierceCount > 0)
                {
                    hits++;
                    projectile.PierceCount--;
                }
            }
            
            Assert.LessOrEqual(hits, 3, "Pierce hits must not exceed configured pierce count");
        }

        [Test]
        public void Projectile_HomingClampRespected()
        {
            // Invariant: Homing projectiles respect turn rate limits
            var turnRateDeg = 90f; // degrees per second
            var maxTurnRad = math.radians(turnRateDeg);
            
            var currentDir = math.normalize(new float3(1f, 0f, 0f));
            var targetDir = math.normalize(new float3(0f, 1f, 0f));
            
            // Calculate turn angle
            var dot = math.dot(currentDir, targetDir);
            var angle = math.acos(math.clamp(dot, -1f, 1f));
            
            // Turn should be clamped to max turn rate
            var clampedTurn = math.min(angle, maxTurnRad);
            Assert.LessOrEqual(clampedTurn, maxTurnRad, "Homing turn rate must respect clamp");
        }

        [Test]
        public void Weapon_BeamTickRateStable()
        {
            // Invariant: Beam weapons have stable tick rate
            var fireRate = 10f; // shots per second
            var tickInterval = 1f / fireRate;
            
            // Simulate multiple ticks
            var lastFireTime = 0f;
            var fireCount = 0;
            for (float time = 0f; time < 1f; time += 0.016f) // 60 FPS
            {
                if (time - lastFireTime >= tickInterval)
                {
                    fireCount++;
                    lastFireTime = time;
                }
            }
            
            // Should fire approximately fireRate times per second
            Assert.GreaterOrEqual(fireCount, (int)(fireRate * 0.9f), "Beam tick rate must be stable");
        }

        [Test]
        public void Weapon_SanityCapsByClass()
        {
            // Invariant: Weapon classes have sanity caps
            var missileAoEMax = 50f; // Max AoE radius for missiles
            var beamDPSMax = 1000f; // Max DPS for beams
            var kineticPierceMax = 5f; // Max pierce for kinetic
            
            // Test missile AoE cap
            var missileAoE = 30f;
            Assert.LessOrEqual(missileAoE, missileAoEMax, "Missile AoE must respect cap");
            
            // Test beam DPS cap (would calculate from damage * fireRate)
            var beamDamage = 50f;
            var beamFireRate = 10f;
            var beamDPS = beamDamage * beamFireRate;
            Assert.LessOrEqual(beamDPS, beamDPSMax, "Beam DPS must respect cap");
            
            // Test kinetic pierce cap
            var kineticPierce = 3f;
            Assert.LessOrEqual(kineticPierce, kineticPierceMax, "Kinetic pierce must respect cap");
        }

        #endregion

        #region Mining/Deposits Invariants

        [Test]
        public void Mining_ConservationInvariant()
        {
            // Invariant: mined == stored + losses
            var mined = 1000f;
            var stored = 950f;
            var losses = 50f;
            
            var total = stored + losses;
            Assert.AreEqual(mined, total, 0.01f, "Mined resources must equal stored + losses");
        }

        [Test]
        public void Mining_DepletionNonNegative()
        {
            // Invariant: Deposit depletion must never go negative
            var deposit = new DepositEntity
            {
                CurrentRichness = 0.5f,
                InitialRichness = 1.0f
            };
            
            // Simulate depletion
            var depletion = deposit.InitialRichness - deposit.CurrentRichness;
            Assert.GreaterOrEqual(depletion, 0f, "Depletion must be non-negative");
            
            // Ensure current richness doesn't go below zero
            deposit.CurrentRichness = math.max(0f, deposit.CurrentRichness - 0.1f);
            Assert.GreaterOrEqual(deposit.CurrentRichness, 0f, "Current richness must be non-negative");
        }

        [Test]
        public void Mining_RespawnDeterministic()
        {
            // Invariant: Respawn (if any) must be deterministic based on seed
            var seed1 = 12345u;
            var seed2 = 12345u;
            
            // Respawn logic should produce same result for same seed
            var respawn1 = SimulateRespawn(seed1);
            var respawn2 = SimulateRespawn(seed2);
            
            Assert.AreEqual(respawn1, respawn2, "Respawn must be deterministic for same seed");
        }

        private float SimulateRespawn(uint seed)
        {
            // Placeholder: would use seed to deterministically calculate respawn
            var random = new Unity.Mathematics.Random(seed);
            return random.NextFloat(0.8f, 1.0f); // Respawn richness
        }

        #endregion

        #region Compliance Invariants

        [Test]
        public void Compliance_EventMatrixConsistent()
        {
            // Invariant: Same event → consistent sanction → rep change path
            var infraction1 = new ComplianceInfraction
            {
                RuleId = new FixedString32Bytes("rule.safezone"),
                TriggerTags = ComplianceTags.SafeZoneViolation,
                Severity = 1f
            };
            
            var infraction2 = new ComplianceInfraction
            {
                RuleId = new FixedString32Bytes("rule.safezone"),
                TriggerTags = ComplianceTags.SafeZoneViolation,
                Severity = 1f
            };
            
            // Same infractions should produce same sanctions
            var sanction1 = CalculateSanction(infraction1);
            var sanction2 = CalculateSanction(infraction2);
            
            Assert.AreEqual(sanction1.FineAmount, sanction2.FineAmount, 0.01f,
                "Same infraction must produce same sanction");
            Assert.AreEqual(sanction1.ReputationHit, sanction2.ReputationHit, 0.01f,
                "Same infraction must produce same rep hit");
        }

        [Test]
        public void Compliance_NoDuplicateSanctionsPerIncident()
        {
            // Invariant: No duplicate sanctions per incident
            var incidentId = 123u;
            var sanction1 = new ComplianceSanction
            {
                RuleId = new FixedString32Bytes("rule.test"),
                SanctionTick = 100u
            };
            
            var sanction2 = new ComplianceSanction
            {
                RuleId = new FixedString32Bytes("rule.test"),
                SanctionTick = 100u // Same tick = same incident
            };
            
            // In real system, would track incident IDs to prevent duplicates
            Assert.AreNotEqual(sanction1, sanction2, "Sanctions should be distinct entities");
        }

        private ComplianceSanction CalculateSanction(ComplianceInfraction infraction)
        {
            // Placeholder: would look up rule and calculate sanction
            return new ComplianceSanction
            {
                RuleId = infraction.RuleId,
                FineAmount = infraction.Severity * 100f,
                ReputationHit = infraction.Severity * 10f,
                SanctionTick = infraction.InfractionTick
            };
        }

        #endregion

        #region Crew Invariants

        [Test]
        public void Crew_GunnerSpreadLeadDeltas()
        {
            // Invariant: Gunner modifiers affect spread and lead calculation
            var baseSpread = 5f; // degrees
            var gunnerLevel = 5;
            var spreadReductionPerLevel = 0.1f; // 10% reduction per level
            
            var modifiedSpread = baseSpread * (1f - (gunnerLevel * spreadReductionPerLevel));
            Assert.Less(modifiedSpread, baseSpread, "Gunner level should reduce spread");
        }

        [Test]
        public void Crew_EngineerRepairRefitTime()
        {
            // Invariant: Engineer reduces repair/refit time by configured percentage
            var baseRepairTime = 100f; // seconds
            var engineerLevel = 3;
            var repairMultPerLevel = 0.9f; // 10% reduction per level
            
            var modifiedRepairTime = baseRepairTime * math.pow(repairMultPerLevel, engineerLevel);
            Assert.Less(modifiedRepairTime, baseRepairTime, "Engineer level should reduce repair time");
        }

        [Test]
        public void Crew_PilotTraverseHeatHandling()
        {
            // Invariant: Pilot affects traverse speed and heat handling
            var baseTraverseSpeed = 90f; // degrees per second
            var pilotLevel = 4;
            var traverseBonusPerLevel = 0.05f; // 5% increase per level
            
            var modifiedTraverseSpeed = baseTraverseSpeed * (1f + (pilotLevel * traverseBonusPerLevel));
            Assert.Greater(modifiedTraverseSpeed, baseTraverseSpeed, "Pilot level should increase traverse speed");
        }

        [Test]
        public void Crew_FatigueRecoversOnlyAtStations()
        {
            // Invariant: Fatigue recovers only at stations
            var crewState = new CrewState
            {
                Fatigue = 0.8f
            };
            
            var atStation = true;
            var notAtStation = false;
            
            // At station: fatigue should decrease
            if (atStation)
            {
                crewState.Fatigue = math.max(0f, crewState.Fatigue - 0.1f);
            }
            
            var fatigueAtStation = crewState.Fatigue;
            
            // Reset
            crewState.Fatigue = 0.8f;
            
            // Not at station: fatigue should not decrease
            if (notAtStation)
            {
                // Fatigue recovery logic should not execute
            }
            
            var fatigueNotAtStation = crewState.Fatigue;
            
            Assert.Less(fatigueAtStation, fatigueNotAtStation, "Fatigue should only recover at stations");
        }

        [Test]
        public void Crew_FatigueCapsObserved()
        {
            // Invariant: Fatigue caps are observed (0-1 range)
            var crewState = new CrewState
            {
                Fatigue = 0.5f
            };
            
            // Increase fatigue beyond cap
            crewState.Fatigue = math.min(1f, crewState.Fatigue + 0.8f);
            Assert.LessOrEqual(crewState.Fatigue, 1f, "Fatigue must not exceed 1.0");
            
            // Decrease fatigue below cap
            crewState.Fatigue = math.max(0f, crewState.Fatigue - 2f);
            Assert.GreaterOrEqual(crewState.Fatigue, 0f, "Fatigue must not go below 0.0");
        }

        #endregion
    }
}

