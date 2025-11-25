using NUnit.Framework;
using PureDOTS.Runtime.Shared;
using Unity.Collections;
using Unity.Entities;

namespace Space4X.Tests.Quality
{
    /// <summary>
    /// EditMode tests for module quality application.
    /// </summary>
    public class ModuleQualityTests
    {
        [Test]
        public void Quality_Affects_Module_Heat_And_Spread_HigherScoreReducesHeatAndSpread()
        {
            // Create a test curve blob
            using var bb = new BlobBuilder(Unity.Collections.Allocator.Temp);
            ref var curves = ref bb.ConstructRoot<QualityCurveBlob>();

            // Heat curve: higher quality = lower heat (better)
            var heatKnots = bb.Allocate(ref curves.Heat.Knots, 8);
            for (int i = 0; i < 8; i++)
            {
                float t = i / 7f;
                heatKnots[i] = 1.05f - (t * 0.15f); // 1.05 at t=0, 0.90 at t=1
            }

            // Reliability curve: higher quality = better reliability (used for spread)
            var reliabilityKnots = bb.Allocate(ref curves.Reliability.Knots, 8);
            for (int i = 0; i < 8; i++)
            {
                float t = i / 7f;
                reliabilityKnots[i] = 0.9f + (t * 0.4f); // 0.9 at t=0, 1.3 at t=1
            }

            // Initialize other curves
            var damageKnots = bb.Allocate(ref curves.Damage.Knots, 8);
            var durabilityKnots = bb.Allocate(ref curves.Durability.Knots, 8);
            for (int i = 0; i < 8; i++)
            {
                damageKnots[i] = 1.0f;
                durabilityKnots[i] = 1.0f;
            }

            var blobRef = bb.CreateBlobAssetReference<QualityCurveBlob>(Unity.Collections.Allocator.Persistent);

            // Test that higher quality reduces heat and spread
            ref var curvesRef = ref blobRef.Value;
            float heatLow = QualityEval.SampleCurve(ref curvesRef.Heat, 0.2f);
            float heatHigh = QualityEval.SampleCurve(ref curvesRef.Heat, 0.8f);

            float spreadLow = QualityEval.SampleCurve(ref curvesRef.Reliability, 0.2f);
            float spreadHigh = QualityEval.SampleCurve(ref curvesRef.Reliability, 0.8f);

            Assert.Greater(heatLow, heatHigh, "Higher quality should reduce heat");
            Assert.Less(spreadLow, spreadHigh, "Higher quality should improve reliability (reduce spread)");
        }
    }
}

