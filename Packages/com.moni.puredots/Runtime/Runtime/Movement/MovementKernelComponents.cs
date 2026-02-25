using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Movement
{
    /// <summary>
    /// Marks entities whose movement/pose is owned by the MovementKernel.
    /// For owned entities, gameplay code should emit intents/commands instead of mutating transforms directly.
    /// </summary>
    public struct MovementKernelOwned : IComponentData
    {
    }

    /// <summary>
    /// Canonical movement pose captured by MovementKernel after integration.
    /// Guard systems can use this to detect external transform writes.
    /// </summary>
    public struct MovementKernelPose : IComponentData
    {
        public float3 Position;
        public quaternion Rotation;
        public float Scale;
        public uint CapturedTick;
    }

    /// <summary>
    /// Runtime configuration for movement write-guard behavior.
    /// </summary>
    public struct MovementKernelGuardConfig : IComponentData
    {
        public byte Enabled;
        public byte StrictRollback;
        public byte LogViolations;
        public float PositionEpsilon;
        public float RotationEpsilonDeg;
        public float ScaleEpsilon;
        public uint MaxViolationsPerTick;

        public static MovementKernelGuardConfig Default => new()
        {
            Enabled = 1,
            StrictRollback = 1,
            LogViolations = 1,
            PositionEpsilon = 0.0005f,
            RotationEpsilonDeg = 0.1f,
            ScaleEpsilon = 0.0005f,
            MaxViolationsPerTick = 64
        };
    }

    /// <summary>
    /// Rolling movement guard stats for diagnostics and validation loops.
    /// </summary>
    public struct MovementKernelGuardStats : IComponentData
    {
        public uint LastTick;
        public uint ViolationsThisTick;
        public uint TotalViolations;
    }

    /// <summary>
    /// One violation sample captured during the current tick.
    /// </summary>
    [InternalBufferCapacity(16)]
    public struct MovementKernelViolation : IBufferElementData
    {
        public Entity Entity;
        public uint Tick;
        public float PositionDelta;
        public float RotationDeltaDeg;
        public float ScaleDelta;
    }
}
