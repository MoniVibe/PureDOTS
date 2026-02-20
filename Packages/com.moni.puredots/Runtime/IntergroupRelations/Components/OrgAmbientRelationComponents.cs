using Unity.Entities;

namespace PureDOTS.Runtime.IntergroupRelations
{
    /// <summary>
    /// Configuration for projecting individual relation changes into organization-level ambient drift.
    /// </summary>
    public struct OrgAmbientRelationConfig : IComponentData
    {
        public byte Enabled;
        public uint UpdateIntervalTicks;
        public uint RecentInteractionHorizonTicks;
        public sbyte InternalConflictThreshold;
        public float ExternalAttitudePerUnit;
        public float ExternalStandingPerUnit;
        public float InternalStandingPenaltyPerUnit;
        public float InternalCohesionPenaltyPerUnit;
        public float InternalCorruptionPerUnit;
        public float InternalPurityPenaltyPerUnit;
        public float InternalOrderPenaltyPerUnit;
        public float InternalVengefulShiftPerUnit;
        public float MaxAttitudeDeltaPerTick;
        public float MaxStandingDeltaPerTick;
        public float MaxInternalDeltaPerTick;
        public float MaxCorruptionDeltaPerTick;

        public static OrgAmbientRelationConfig Default => new OrgAmbientRelationConfig
        {
            Enabled = 1,
            UpdateIntervalTicks = 45u,
            RecentInteractionHorizonTicks = 240u,
            InternalConflictThreshold = -35,
            ExternalAttitudePerUnit = 1.5f,
            ExternalStandingPerUnit = 3f,
            InternalStandingPenaltyPerUnit = 4f,
            InternalCohesionPenaltyPerUnit = 0.025f,
            InternalCorruptionPerUnit = 0.04f,
            InternalPurityPenaltyPerUnit = 0.03f,
            InternalOrderPenaltyPerUnit = 0.02f,
            InternalVengefulShiftPerUnit = 0.015f,
            MaxAttitudeDeltaPerTick = 2f,
            MaxStandingDeltaPerTick = 6f,
            MaxInternalDeltaPerTick = 0.05f,
            MaxCorruptionDeltaPerTick = 0.05f
        };
    }

    /// <summary>
    /// Cursor/state for ambient projection cadence.
    /// </summary>
    public struct OrgAmbientRelationState : IComponentData
    {
        public uint LastProjectionTick;
    }
}
