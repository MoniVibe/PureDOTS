using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.InputKernel;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Systems.Input
{
    /// <summary>
    /// Clamps and normalizes shared locomotion intents so downstream systems consume stable values.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup), OrderFirst = true)]
    public partial struct InputKernelSanitizationSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<InputKernelRootTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var entityManager = state.EntityManager;
            var tick = SystemAPI.GetSingleton<TimeState>().Tick;
            var root = SystemAPI.GetSingletonEntity<InputKernelRootTag>();
            if (!entityManager.HasComponent<InputKernelState>(root) ||
                !entityManager.HasComponent<InputKernelDiagnostics>(root))
            {
                return;
            }

            uint sanitizedCount = 0u;
            foreach (var intentRef in SystemAPI.Query<RefRW<InputKernelLocomotionIntent>>())
            {
                var intent = intentRef.ValueRO;
                var changed = false;

                changed |= ClampAxis(ref intent.Forward);
                changed |= ClampAxis(ref intent.Strafe);
                changed |= ClampAxis(ref intent.Vertical);
                changed |= ClampAxis(ref intent.Roll);

                changed |= ClampFlag01(ref intent.TranslationBasisOverride);
                changed |= ClampFlag01(ref intent.AutoAlignToTranslation);
                changed |= ClampFlag01(ref intent.CursorSteeringActive);
                changed |= ClampFlag01(ref intent.BoostPressed);
                changed |= ClampFlag01(ref intent.RetroBrakePressed);
                changed |= ClampFlag01(ref intent.ToggleAuxiliaryAction);
                changed |= ClampFlag01(ref intent.MovementEnabled);
                changed |= ClampFlag01(ref intent.KernelModeRequested);

                var steeringMode = intent.SteeringMode;
                intent.SteeringMode = steeringMode != 0 ? (byte)1 : (byte)0;
                changed |= intent.SteeringMode != steeringMode;

                changed |= NormalizeDirection(
                    ref intent.TranslationForward,
                    ref intent.TranslationUp,
                    new float3(0f, 0f, 1f),
                    new float3(0f, 1f, 0f));

                changed |= NormalizeDirection(
                    ref intent.CursorLookDirection,
                    ref intent.CursorUpDirection,
                    new float3(0f, 0f, 1f),
                    new float3(0f, 1f, 0f));

                if (intent.SampleTick != tick)
                {
                    intent.SampleTick = tick;
                    changed = true;
                }

                if (changed)
                {
                    intentRef.ValueRW = intent;
                    sanitizedCount++;
                }
            }

            var kernelState = entityManager.GetComponentData<InputKernelState>(root);
            if (kernelState.Tick != tick || sanitizedCount > 0u)
            {
                kernelState.Tick = tick;
                kernelState.Revision++;
                entityManager.SetComponentData(root, kernelState);
            }

            var diagnostics = entityManager.GetComponentData<InputKernelDiagnostics>(root);
            diagnostics.LastTick = tick;
            diagnostics.EntitiesSanitized = sanitizedCount;
            diagnostics.TotalSanitized += sanitizedCount;
            entityManager.SetComponentData(root, diagnostics);
        }

        private static bool ClampAxis(ref float axis)
        {
            var original = axis;
            axis = math.clamp(axis, -1f, 1f);
            return axis != original;
        }

        private static bool ClampFlag01(ref byte value)
        {
            var original = value;
            value = value != 0 ? (byte)1 : (byte)0;
            return value != original;
        }

        private static bool NormalizeDirection(
            ref float3 forward,
            ref float3 up,
            in float3 fallbackForward,
            in float3 fallbackUp)
        {
            var originalForward = forward;
            var originalUp = up;

            var normalizedForward = math.normalizesafe(forward, fallbackForward);
            var normalizedUp = math.normalizesafe(up, fallbackUp);

            normalizedUp -= normalizedForward * math.dot(normalizedUp, normalizedForward);
            normalizedUp = math.normalizesafe(normalizedUp, fallbackUp);

            if (math.abs(math.dot(normalizedForward, normalizedUp)) > 0.999f)
            {
                normalizedUp = math.abs(normalizedForward.y) < 0.95f
                    ? new float3(0f, 1f, 0f)
                    : new float3(1f, 0f, 0f);
                normalizedUp -= normalizedForward * math.dot(normalizedUp, normalizedForward);
                normalizedUp = math.normalizesafe(normalizedUp, fallbackUp);
            }

            forward = normalizedForward;
            up = normalizedUp;

            return !NearlyEqual(originalForward, normalizedForward) ||
                   !NearlyEqual(originalUp, normalizedUp);
        }

        private static bool NearlyEqual(in float3 a, in float3 b)
        {
            return math.lengthsq(a - b) <= 1e-6f;
        }
    }
}
