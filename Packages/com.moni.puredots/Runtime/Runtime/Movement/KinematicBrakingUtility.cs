using Unity.Mathematics;

namespace PureDOTS.Runtime.Movement
{
    public enum KinematicBrakingManeuver : byte
    {
        None = 0,
        RetroBurn = 1,
        FlipAndBurn = 2
    }

    public struct KinematicBrakingDecisionInput
    {
        public float3 Velocity;
        public float3 DesiredDirection;
        public float3 ForwardDirection;
        public float CurrentSpeed;
        public float CurrentSpeedSq;
        public float BaseSpeed;
        public float TurnSpeed;
        public float BaseRotationSpeed;
        public float EngineScale;
        public float RotationMultiplier;
        public float TurnNorm;
        public float Acceleration;
        public float Deceleration;
        public float ThrustAuthority;
        public float TurnAuthority;
        public float EngineVectoring;
        public float EngineResponse;
        public float CapitalShipTurnMultiplier;
        public float FlipBurnMinSpeedRatio;
        public float FlipBurnAdvantageThreshold;
        public float FlipBurnCombatPenalty;
        public float FlipBurnCommitSeconds;
        public float BrakingPressure;
        public float DeltaTime;
        public uint CurrentTick;
        public uint ActiveCommitUntilTick;
        public KinematicBrakingManeuver ActiveManeuver;
        public byte InertialEnabled;
        public byte IdleCoast;
        public byte IsCapitalShip;
        public byte CombatIntent;
    }

    public struct KinematicBrakingDecision
    {
        public KinematicBrakingManeuver SelectedManeuver;
        public KinematicBrakingManeuver ActiveManeuver;
        public uint ActiveCommitUntilTick;
    }

    public static class KinematicBrakingUtility
    {
        public static KinematicBrakingDecision Evaluate(in KinematicBrakingDecisionInput input)
        {
            var decision = new KinematicBrakingDecision
            {
                SelectedManeuver = KinematicBrakingManeuver.None,
                ActiveManeuver = KinematicBrakingManeuver.None,
                ActiveCommitUntilTick = 0u
            };

            if (input.BrakingPressure <= 1e-4f || input.CurrentSpeedSq <= 1e-4f || input.InertialEnabled == 0)
            {
                if (input.ActiveCommitUntilTick != 0u && input.CurrentTick < input.ActiveCommitUntilTick)
                {
                    decision.ActiveManeuver = input.ActiveManeuver;
                    decision.ActiveCommitUntilTick = input.ActiveCommitUntilTick;
                }

                return decision;
            }

            if (input.ActiveManeuver == KinematicBrakingManeuver.FlipAndBurn &&
                input.ActiveCommitUntilTick != 0u &&
                input.CurrentTick < input.ActiveCommitUntilTick)
            {
                decision.SelectedManeuver = KinematicBrakingManeuver.FlipAndBurn;
                decision.ActiveManeuver = KinematicBrakingManeuver.FlipAndBurn;
                decision.ActiveCommitUntilTick = input.ActiveCommitUntilTick;
                return decision;
            }

            var minSpeedRatio = math.saturate(input.FlipBurnMinSpeedRatio);
            var speedRatio = input.CurrentSpeed / math.max(0.1f, input.BaseSpeed);
            var speedPressure = math.saturate((speedRatio - minSpeedRatio) / math.max(0.01f, 1f - minSpeedRatio));

            var turnPressure = math.saturate(math.max(0f, input.TurnAuthority));
            if (input.IsCapitalShip != 0)
            {
                turnPressure *= math.max(0.1f, input.CapitalShipTurnMultiplier);
            }

            var forward = math.normalizesafe(input.ForwardDirection, new float3(0f, 0f, 1f));
            var velocity = math.normalizesafe(input.Velocity, forward);
            var retro = -velocity;
            var facingRetro = math.saturate((math.dot(forward, retro) + 1f) * 0.5f);
            var flipNeed = 1f - facingRetro;

            var combatPenalty = input.CombatIntent != 0 ? math.saturate(input.FlipBurnCombatPenalty) : 0f;
            var flipScore = input.BrakingPressure * speedPressure * turnPressure * flipNeed;
            flipScore *= (1f - combatPenalty);

            var threshold = math.max(0.05f, input.FlipBurnAdvantageThreshold);
            if (flipScore >= threshold)
            {
                decision.SelectedManeuver = KinematicBrakingManeuver.FlipAndBurn;
                decision.ActiveManeuver = KinematicBrakingManeuver.FlipAndBurn;
                var deltaTime = math.max(0.001f, input.DeltaTime);
                var commitSeconds = math.max(0f, input.FlipBurnCommitSeconds);
                var commitTicks = (uint)math.max(1, (int)math.ceil(commitSeconds / deltaTime));
                decision.ActiveCommitUntilTick = input.CurrentTick + commitTicks;
                return decision;
            }

            decision.SelectedManeuver = KinematicBrakingManeuver.RetroBurn;
            decision.ActiveManeuver = KinematicBrakingManeuver.RetroBurn;
            decision.ActiveCommitUntilTick = 0u;
            return decision;
        }
    }
}
