using Unity.Burst;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Progression
{
    [BurstCompile]
    public static class ProgressionMath
    {
        [BurstCompile]
        public static void AccumulatePositive(ref float value, float delta)
        {
            if (delta > 0f)
            {
                value += delta;
            }
        }

        [BurstCompile]
        public static float ResolveSkill01FromPractice(
            float practiceSeconds,
            float secondsToMastery,
            float wisdom01,
            float aptitude01,
            float wisdomMultiplierMin,
            float wisdomMultiplierMax,
            float aptitudeMultiplierMin,
            float aptitudeMultiplierMax)
        {
            if (secondsToMastery <= 0f)
            {
                return 1f;
            }

            var baseProgress = math.max(0f, practiceSeconds) / secondsToMastery;
            var wisdomMul = math.lerp(wisdomMultiplierMin, wisdomMultiplierMax, math.saturate(wisdom01));
            var aptitudeMul = math.lerp(aptitudeMultiplierMin, aptitudeMultiplierMax, math.saturate(aptitude01));
            return math.saturate(baseProgress * wisdomMul * aptitudeMul);
        }

        [BurstCompile]
        public static int ResolveLinearMilestoneCount(
            float totalValue,
            float baseThreshold,
            float perMilestoneStep,
            int maxMilestones)
        {
            if (maxMilestones <= 0 || baseThreshold <= 0f)
            {
                return 0;
            }

            var step = math.max(0f, perMilestoneStep);
            var count = 0;
            for (var i = 0; i < maxMilestones; i++)
            {
                var threshold = baseThreshold + step * i;
                if (totalValue + 0.0001f < threshold)
                {
                    break;
                }

                count++;
            }

            return count;
        }
    }
}