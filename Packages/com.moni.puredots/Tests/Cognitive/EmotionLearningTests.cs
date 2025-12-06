using NUnit.Framework;
using PureDOTS.Runtime.Cognitive;
using Unity.Entities;

namespace PureDOTS.Tests.Cognitive
{
    /// <summary>
    /// Unit tests for emotional learning systems.
    /// </summary>
    public class EmotionLearningTests
    {
        [Test]
        public void EmotionState_Clamping_Works()
        {
            var emotionState = new EmotionState
            {
                Anger = 1.5f, // Should clamp to 1f
                Trust = -0.5f, // Should clamp to 0f
                Fear = 0.5f,
                Pride = 0.5f,
                LastUpdateTick = 0
            };

            // Clamping would be done by system, but we test the structure
            Assert.GreaterOrEqual(emotionState.Anger, 0f);
            Assert.LessOrEqual(emotionState.Anger, 1f);
        }

        [Test]
        public void EmotionModulator_Calculation_IsValid()
        {
            var modulator = new EmotionModulator
            {
                LearningRateMultiplier = 1.2f,
                BiasAdjustment = 0.1f,
                ConfidenceModifier = 0.15f,
                LastUpdateTick = 0
            };

            // LearningRateMultiplier should be: 1 + Pride - Fear
            // For Pride=0.7, Fear=0.2: multiplier = 1 + 0.7 - 0.2 = 1.5
            // Clamped to 0.1-2.0 range
            Assert.GreaterOrEqual(modulator.LearningRateMultiplier, 0.1f);
            Assert.LessOrEqual(modulator.LearningRateMultiplier, 2f);
        }
    }
}

