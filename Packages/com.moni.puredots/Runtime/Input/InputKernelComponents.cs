using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.InputKernel
{
    /// <summary>
    /// Singleton tag for the shared InputKernel.
    /// </summary>
    public struct InputKernelRootTag : IComponentData
    {
    }

    /// <summary>
    /// Shared per-tick state for InputKernel consumers.
    /// </summary>
    public struct InputKernelState : IComponentData
    {
        public uint Tick;
        public uint Revision;
    }

    /// <summary>
    /// Marks entities that opt into the shared input ownership contract.
    /// </summary>
    public struct InputKernelOwnership : IComponentData
    {
        public byte PlayerId;
    }

    /// <summary>
    /// Canonical locomotion intent that simulation kernels consume.
    /// Adapters map game-specific input onto this shared contract.
    /// </summary>
    public struct InputKernelLocomotionIntent : IComponentData
    {
        public float Forward;
        public float Strafe;
        public float Vertical;
        public float Roll;
        public float3 TranslationForward;
        public float3 TranslationUp;
        public float3 CursorLookDirection;
        public float3 CursorUpDirection;
        public byte TranslationBasisOverride;
        public byte AutoAlignToTranslation;
        public byte CursorSteeringActive;
        public byte SteeringMode;
        public byte BoostPressed;
        public byte RetroBrakePressed;
        public byte ToggleAuxiliaryAction;
        public byte MovementEnabled;
        public byte KernelModeRequested;
        public uint SampleTick;

        public static InputKernelLocomotionIntent Disabled => new InputKernelLocomotionIntent
        {
            Forward = 0f,
            Strafe = 0f,
            Vertical = 0f,
            Roll = 0f,
            TranslationForward = new float3(0f, 0f, 1f),
            TranslationUp = new float3(0f, 1f, 0f),
            CursorLookDirection = new float3(0f, 0f, 1f),
            CursorUpDirection = new float3(0f, 1f, 0f),
            TranslationBasisOverride = 0,
            AutoAlignToTranslation = 0,
            CursorSteeringActive = 0,
            SteeringMode = 0,
            BoostPressed = 0,
            RetroBrakePressed = 0,
            ToggleAuxiliaryAction = 0,
            MovementEnabled = 0,
            KernelModeRequested = 0,
            SampleTick = 0u
        };
    }

    /// <summary>
    /// Rolling diagnostics for InputKernel sanitization.
    /// </summary>
    public struct InputKernelDiagnostics : IComponentData
    {
        public uint LastTick;
        public uint EntitiesSanitized;
        public uint TotalSanitized;
    }
}
