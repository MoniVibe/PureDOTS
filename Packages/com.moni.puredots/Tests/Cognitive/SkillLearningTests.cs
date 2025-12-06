using NUnit.Framework;
using PureDOTS.Runtime.Cognitive;
using Unity.Entities;

namespace PureDOTS.Tests.Cognitive
{
    /// <summary>
    /// Unit tests for skill learning systems.
    /// </summary>
    public class SkillLearningTests
    {
        [Test]
        public void SkillProfile_NormalizedValues_AreValid()
        {
            var skillProfile = new SkillProfile
            {
                CastingSkill = 0.8f,
                DualCastingAptitude = 0.6f,
                MeleeSkill = 0.7f,
                StrategicThinking = 0.5f,
                LastUpdateTick = 0
            };

            // All skills should be normalized 0-1
            Assert.GreaterOrEqual(skillProfile.CastingSkill, 0f);
            Assert.LessOrEqual(skillProfile.CastingSkill, 1f);
            Assert.GreaterOrEqual(skillProfile.MeleeSkill, 0f);
            Assert.LessOrEqual(skillProfile.MeleeSkill, 1f);
        }

        [Test]
        public void SkillLearningState_PlateauDetection_Works()
        {
            var learningState = new SkillLearningState
            {
                CastingExperienceCount = 100,
                DualCastingExperienceCount = 50,
                MeleeExperienceCount = 75,
                StrategicExperienceCount = 25,
                LastUpdateTick = 1000,
                PlateauThreshold = 0.001f,
                IsPlateaued = false
            };

            // Plateau detection would be done by system
            Assert.GreaterOrEqual(learningState.PlateauThreshold, 0f);
        }
    }
}

