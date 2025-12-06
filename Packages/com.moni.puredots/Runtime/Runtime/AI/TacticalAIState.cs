using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.AI
{
    /// <summary>
    /// Tactical AI state machine states.
    /// </summary>
    public enum TacticalAIStateType : byte
    {
        Idle = 0,
        Advance = 1,
        Engage = 2,
        Evaluate = 3,
        Regroup = 4,
        Pursue = 5
    }

    /// <summary>
    /// Tactical AI state component for formations.
    /// Tracks current state and decision context.
    /// </summary>
    public struct TacticalAIState : IComponentData
    {
        /// <summary>Current state.</summary>
        public TacticalAIStateType State;

        /// <summary>Last state transition tick.</summary>
        public uint DecisionTick;

        /// <summary>Decision context: RelativeMorale, Cohesion, CommanderTraits, BattlefieldContext.</summary>
        public float4 Context;
    }
}

