using NUnit.Framework;
using PureDOTS.Runtime.Shared;
using Unity.Collections;
using Unity.Entities;

namespace Godgame.Tests.Items
{
    /// <summary>
    /// EditMode tests for quality formula determinism and curve evaluation.
    /// </summary>
    public class QualityFormulaTests
    {
        [Test]
        public void Quality_Formula_Idempotent_SameInputsProduceSameScore01()
        {
            // Create a test formula blob
            using var bb = new BlobBuilder(Unity.Collections.Allocator.Temp);
            ref var formula = ref bb.ConstructRoot<QualityFormulaBlob>();
            formula.WMaterial = 0.4f;
            formula.WSkill = 0.2f;
            formula.WStation = 0.1f;
            formula.WRecipe = 0.3f;
            formula.Bias = 0f;
            formula.ClampMin = 0f;
            formula.ClampMax = 1f;

            var cutoffs = bb.Allocate(ref formula.TierCutoffs01, 4);
            cutoffs[0] = 0.20f;
            cutoffs[1] = 0.45f;
            cutoffs[2] = 0.70f;
            cutoffs[3] = 0.90f;

            var blobRef = bb.CreateBlobAssetReference<QualityFormulaBlob>(Unity.Collections.Allocator.Persistent);

            ref var formulaRef = ref blobRef.Value;

            // Test idempotency: same inputs should produce same results
            float score1 = QualityEval.Score01(ref formulaRef, 0.8f, 0.6f, 0.5f, 0.4f);
            float score2 = QualityEval.Score01(ref formulaRef, 0.8f, 0.6f, 0.5f, 0.4f);

            Assert.AreEqual(score1, score2, 0.001f, "Same inputs should produce same Score01");

            // Test tier assignment
            QualityTier tier1 = QualityEval.Tier(ref formulaRef, score1);
            QualityTier tier2 = QualityEval.Tier(ref formulaRef, score2);
            Assert.AreEqual(tier1, tier2, "Same score should produce same Tier");
        }

        [Test]
        public void Quality_Curves_Bounded_CurveSamplesAreFinite()
        {
            // Create a test curve
            using var bb = new BlobBuilder(Unity.Collections.Allocator.Temp);
            ref var curve = ref bb.ConstructRoot<Curve1D>();
            var knots = bb.Allocate(ref curve.Knots, 8);
            for (int i = 0; i < 8; i++)
            {
                knots[i] = 0.5f + (i * 0.1f); // Values from 0.5 to 1.2
            }

            var blobRef = bb.CreateBlobAssetReference<Curve1D>(Unity.Collections.Allocator.Persistent);

            ref var curveRef = ref blobRef.Value;

            // Test curve sampling at various points
            for (float t = 0f; t <= 1f; t += 0.1f)
            {
                float value = QualityEval.SampleCurve(ref curveRef, t);
                Assert.IsTrue(float.IsFinite(value), $"Curve sample at t={t} should be finite");
                Assert.IsTrue(value >= 0f, $"Curve sample at t={t} should be non-negative");
            }
        }

        [Test]
        public void Quality_TierCutoffs_Sorted_CutoffsAreAscending()
        {
            // Create formula with unsorted cutoffs (should still work, but test validates sorting)
            using var bb = new BlobBuilder(Unity.Collections.Allocator.Temp);
            ref var formula = ref bb.ConstructRoot<QualityFormulaBlob>();
            formula.WMaterial = 0.4f;
            formula.WSkill = 0.2f;
            formula.WStation = 0.1f;
            formula.WRecipe = 0.3f;
            formula.Bias = 0f;
            formula.ClampMin = 0f;
            formula.ClampMax = 1f;

            // Sorted cutoffs
            var cutoffs = bb.Allocate(ref formula.TierCutoffs01, 4);
            cutoffs[0] = 0.20f;
            cutoffs[1] = 0.45f;
            cutoffs[2] = 0.70f;
            cutoffs[3] = 0.90f;

            var blobRef = bb.CreateBlobAssetReference<QualityFormulaBlob>(Unity.Collections.Allocator.Persistent);

            ref var formulaRef2 = ref blobRef.Value;

            // Test tier assignment at various scores
            Assert.AreEqual(QualityTier.Poor, QualityEval.Tier(ref formulaRef2, 0.10f), "Score below first cutoff");
            Assert.AreEqual(QualityTier.Common, QualityEval.Tier(ref formulaRef2, 0.30f), "Score above first cutoff");
            Assert.AreEqual(QualityTier.Uncommon, QualityEval.Tier(ref formulaRef2, 0.50f), "Score above second cutoff");
            Assert.AreEqual(QualityTier.Rare, QualityEval.Tier(ref formulaRef2, 0.75f), "Score above third cutoff");
            Assert.AreEqual(QualityTier.Epic, QualityEval.Tier(ref formulaRef2, 0.95f), "Score above fourth cutoff");
        }
    }
}

