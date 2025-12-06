using NUnit.Framework;
using PureDOTS.Runtime.Cognitive;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Tests.Cognitive
{
    /// <summary>
    /// Unit tests for experience processing systems.
    /// </summary>
    public class ExperienceProcessingTests
    {
        [Test]
        public void ExperienceEvent_Creation_Succeeds()
        {
            var experience = new ExperienceEvent
            {
                Type = ExperienceType.Combat,
                Source = Entity.Null,
                Context = Entity.Null,
                Outcome = 1f,
                CultureId = 1,
                Tick = 100
            };

            Assert.AreEqual(ExperienceType.Combat, experience.Type);
            Assert.AreEqual(1f, experience.Outcome);
            Assert.AreEqual(1, experience.CultureId);
        }

        [Test]
        public void MemoryProfile_DefaultValues_AreValid()
        {
            var profile = new MemoryProfile
            {
                LearningRate = 0.1f,
                Retention = 0.95f,
                Bias = 0f,
                LastUpdateTick = 0
            };

            Assert.GreaterOrEqual(profile.LearningRate, 0f);
            Assert.LessOrEqual(profile.LearningRate, 1f);
            Assert.GreaterOrEqual(profile.Retention, 0f);
            Assert.LessOrEqual(profile.Retention, 1f);
        }
    }
}

