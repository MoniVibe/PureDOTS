using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Godgame.Resources
{
    /// <summary>
    /// Logical state for an aggregate pile based on the design doc.
    /// </summary>
    public enum AggregatePileState : byte
    {
        Growing = 0,
        Stable = 1,
        Merging = 2,
        Splitting = 3
    }

    /// <summary>
    /// Runtime data for a ground pile that represents excess resources.
    /// </summary>
    public struct AggregatePile : IComponentData
    {
        public ushort ResourceTypeIndex;
        public float Amount;
        public float MaxCapacity;
        public float MergeRadius;
        public float LastMutationTime;
        public AggregatePileState State;
    }

    /// <summary>
    /// Visual metadata used to drive renderer scaling without recalculating curves elsewhere.
    /// </summary>
    public struct AggregatePileVisual : IComponentData
    {
        public float CurrentScale;
        public float TargetScale;
    }

    /// <summary>
    /// Shared tuning values for pile behaviour.
    /// </summary>
    public struct AggregatePileConfig : IComponentData
    {
        public float DefaultMaxCapacity;
        public float GlobalMaxCapacity;
        public float MergeRadius;
        public float SplitThreshold;
        public float MergeCheckSeconds;
        public float MinSpawnAmount;
        public float ConservationEpsilon;
        public int MaxActivePiles;

        public static AggregatePileConfig CreateDefault()
        {
            return new AggregatePileConfig
            {
                DefaultMaxCapacity = 2500f,
                GlobalMaxCapacity = 5000f,
                MergeRadius = 2.5f,
                SplitThreshold = 2500f,
                MergeCheckSeconds = 5f,
                MinSpawnAmount = 10f,
                ConservationEpsilon = 0.01f,
                MaxActivePiles = 200
            };
        }
    }

    /// <summary>
    /// Tracks runtime state for the pile system (timers, counters).
    /// </summary>
    public struct AggregatePileRuntimeState : IComponentData
    {
        public float NextMergeTime;
        public int ActivePiles;
    }

    /// <summary>
    /// Singleton marker ensuring command buffers exist.
    /// </summary>
    public struct AggregatePileCommandState : IComponentData { }

    [System.Flags]
    public enum AggregatePileAddFlags : byte
    {
        None = 0,
        ForceNewPile = 1 << 0
    }

    public struct AggregatePileAddCommand : IBufferElementData
    {
        public Entity Requester;
        public float3 Position;
        public ushort ResourceTypeIndex;
        public float Amount;
        public float MergeRadiusOverride;
        public Entity PreferredPile;
        public AggregatePileAddFlags Flags;
    }

    public struct AggregatePileTakeCommand : IBufferElementData
    {
        public Entity Requester;
        public Entity Pile;
        public float Amount;
    }

    public enum AggregatePileCommandResultType : byte
    {
        None = 0,
        AddAccepted = 1,
        AddRejected = 2,
        TakeCompleted = 3,
        TakePartial = 4
    }

    public struct AggregatePileCommandResult : IBufferElementData
    {
        public Entity Requester;
        public Entity Pile;
        public ushort ResourceTypeIndex;
        public float Amount;
        public AggregatePileCommandResultType Type;
    }
}
