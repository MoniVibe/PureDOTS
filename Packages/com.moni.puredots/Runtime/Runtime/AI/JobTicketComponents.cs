using Unity.Entities;

namespace PureDOTS.Runtime.AI
{
    /// <summary>
    /// Generic job ticket type for shared task coordination.
    /// </summary>
    public enum JobTicketType : byte
    {
        None = 0,
        Gather = 1
    }

    /// <summary>
    /// Lifecycle state for a job ticket.
    /// </summary>
    public enum JobTicketState : byte
    {
        Open = 0,
        Claimed = 1,
        InProgress = 2,
        Done = 3,
        Cancelled = 4
    }

    /// <summary>
    /// Shared job ticket describing a single unit of work.
    /// </summary>
    public struct JobTicket : IComponentData
    {
        public JobTicketType Type;
        public JobTicketState State;
        public Entity TargetEntity;
        public Entity Assignee;
        public ushort ResourceTypeIndex;
        public uint ClaimExpiresTick;
        public uint LastStateTick;
        public ulong JobKey;
    }

    /// <summary>
    /// Assignment state for agents participating in job tickets.
    /// </summary>
    public struct JobAssignment : IComponentData
    {
        public Entity Ticket;
        public uint CommitTick;
    }
}
