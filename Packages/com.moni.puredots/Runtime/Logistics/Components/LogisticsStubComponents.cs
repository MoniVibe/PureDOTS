using Unity.Entities;

namespace PureDOTS.Runtime.Logistics.Components
{
    // STUB: replace with full routing schema once route planner spec lands.
    public struct LogisticsRoute : IComponentData
    {
        public int RouteId;
        public byte Status; // planning, active, blocked
    }

    // STUB: replace with full haul request payload (volumes/resources) when catalog types are ready.
    public struct HaulRequest : IComponentData
    {
        public int RequestId;
        public Entity Source;
        public Entity Destination;
    }

    // STUB: maintenance task placeholder so scheduling systems can be wired ahead of time.
    public struct MaintenanceTicket : IComponentData
    {
        public int TicketId;
        public Entity Target;
        public byte Severity; // 0 = minor, 1 = blocking
    }
}
