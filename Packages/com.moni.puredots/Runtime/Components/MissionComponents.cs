using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Shared;

namespace PureDOTS.Runtime.Components
{
    /// <summary>
    /// Mission node representing a single task step (mine, refine, deliver, build).
    /// </summary>
    public struct MissionNode : IBufferElementData
    {
        public int NodeId;                 // Unique identifier within mission
        public MissionNodeType Type;       // Type of task
        public float3 TargetPosition;      // Target location
        public Entity TargetEntity;        // Target entity (if applicable)
        public float EstimatedDuration;    // Estimated time to complete
        public float Value;                // Value/priority of this node
        public int NextNodeId;            // ID of next node (-1 if terminal)
        public MissionNodeStatus Status;   // Current status
    }

    /// <summary>
    /// Types of mission nodes.
    /// </summary>
    public enum MissionNodeType : byte
    {
        None = 0,
        Mine = 1,
        Refine = 2,
        Deliver = 3,
        Build = 4,
        Defend = 5,
        Patrol = 6,
        Harvest = 7
    }

    /// <summary>
    /// Status of a mission node.
    /// </summary>
    public enum MissionNodeStatus : byte
    {
        Pending = 0,
        InProgress = 1,
        Completed = 2,
        Failed = 3,
        Cancelled = 4
    }

    /// <summary>
    /// Execution path representing a linked sequence of mission nodes.
    /// </summary>
    public struct ExecutionPath : IComponentData
    {
        public AgentGuid PathGuid;         // Unique identifier for this path
        public int StartNodeId;            // Starting node ID
        public float TotalEstimatedValue;  // Total value of completing this path
        public float TotalEstimatedDuration; // Total estimated duration
        public ExecutionPathStatus Status; // Current path status
        public uint StartTick;             // When path execution started
    }

    /// <summary>
    /// Status of an execution path.
    /// </summary>
    public enum ExecutionPathStatus : byte
    {
        Planned = 0,
        Active = 1,
        Completed = 2,
        Failed = 3,
        Cancelled = 4
    }

    /// <summary>
    /// Task network representing a graph of available execution paths.
    /// </summary>
    public struct TaskNetwork : IComponentData
    {
        public AgentGuid NetworkGuid;      // Unique identifier for this network
        public int PathCount;               // Number of paths in network
        public float NetworkValue;          // Total value of network
        public uint LastEvaluationTick;     // When network was last evaluated
    }

    /// <summary>
    /// Mission assignment linking an agent to a mission path.
    /// </summary>
    public struct MissionAssignment : IComponentData
    {
        public AgentGuid AgentGuid;        // Assigned agent
        public AgentGuid PathGuid;          // Path assigned to agent
        public int CurrentNodeId;          // Current node being executed
        public float Progress;              // Progress on current node (0-1)
        public uint AssignmentTick;        // When assignment was made
    }

    /// <summary>
    /// Mission value delta for CPU prioritization.
    /// Tracks per-tick value changes to prioritize CPU attention.
    /// </summary>
    public struct MissionValueDelta : IComponentData
    {
        public float ValueDelta;           // Change in mission value this tick
        public float UrgencyScore;          // Urgency score (0-1)
        public uint LastUpdateTick;         // When delta was last computed
    }
}

