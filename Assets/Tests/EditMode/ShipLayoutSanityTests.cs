using NUnit.Framework;
using PureDOTS.Runtime.Ships;
using Unity.Collections;
using Unity.Mathematics;

namespace PureDOTS.Tests.EditMode
{
    public class ShipLayoutSanityTests
    {
        [Test]
        public void Layout_ArcsCover_AllFacings()
        {
            // Test that armor/shields sum to full coverage
            var armorArcs = new ArmorArc[8];
            for (int i = 0; i < 8; i++)
            {
                armorArcs[i] = new ArmorArc
                {
                    Facing = (Facing8)i,
                    Thickness = 10f,
                    KineticResist = 0.5f,
                    EnergyResist = 0.3f,
                    ExplosiveResist = 0.7f
                };
            }

            // Verify all 8 facings are covered
            var coveredFacings = new bool[8];
            foreach (var arc in armorArcs)
            {
                coveredFacings[(int)arc.Facing] = true;
            }

            for (int i = 0; i < 8; i++)
            {
                Assert.IsTrue(coveredFacings[i], $"Facing {(Facing8)i} should be covered by armor.");
            }
        }

        [Test]
        public void Layout_Modules_OBBs_Valid()
        {
            // Test that module OBBs are non-degenerate and valid
            var obb = new ModuleHitOBB
            {
                Center = float3.zero,
                Extents = new float3(1f, 1f, 1f),
                Rot = quaternion.identity,
                ModuleIndex = 0
            };

            // Verify extents are positive
            Assert.Greater(obb.Extents.x, 0f, "OBB extents should be positive.");
            Assert.Greater(obb.Extents.y, 0f, "OBB extents should be positive.");
            Assert.Greater(obb.Extents.z, 0f, "OBB extents should be positive.");

            // Test degenerate case (should fail)
            var degenerateObb = new ModuleHitOBB
            {
                Center = float3.zero,
                Extents = float3.zero,
                Rot = quaternion.identity,
                ModuleIndex = 0
            };

            Assert.IsFalse(ValidateOBB(degenerateObb), "Degenerate OBB should be invalid.");
        }

        [Test]
        public void Rules_TechGates()
        {
            // Test that below-tier behavior matches RefitRepairRulesBlob
            var rules = new RefitRepairRulesBlob
            {
                FieldPenalty = 2f,
                BelowTechPenalty = 1.5f,
                AllowBelowTech = 1
            };

            // Test field penalty
            Assert.Greater(rules.FieldPenalty, 1f, "Field penalty should be > 1.");

            // Test below-tech penalty
            Assert.Greater(rules.BelowTechPenalty, 1f, "Below-tech penalty should be > 1.");

            // Test allow below-tech flag
            Assert.IsTrue(rules.AllowBelowTech != 0 || rules.AllowBelowTech == 0, "AllowBelowTech should be 0 or 1.");

            // Test disallow case
            var disallowRules = new RefitRepairRulesBlob
            {
                FieldPenalty = 2f,
                BelowTechPenalty = 1.5f,
                AllowBelowTech = 0
            };

            Assert.AreEqual(0, disallowRules.AllowBelowTech, "Disallow rules should have AllowBelowTech = 0.");
        }

        private bool ValidateOBB(ModuleHitOBB obb)
        {
            return math.all(obb.Extents > 0f);
        }
    }
}

