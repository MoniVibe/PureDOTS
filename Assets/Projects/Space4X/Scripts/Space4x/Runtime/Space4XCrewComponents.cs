using PureDOTS.Runtime.Components;
using Unity.Entities;

namespace Space4X.Runtime
{
    public enum Space4XCrewDuty : byte
    {
        Idle = 0,
        Docked = 1,
        Sortie = 2,
        Transfer = 3,
        Combat = 4
    }

    /// <summary>
    /// Additional data tracked for crew aggregates beyond the shared AggregateEntity fields.
    /// </summary>
    public struct Space4XCrewAggregateData : IComponentData
    {
        public Entity HomeCarrier;
        public Entity CurrentCraft;
        public float AverageEnergy;
        public float AverageDisciplineLevel;
        public Space4XCrewDuty Duty;
    }

    /// <summary>
    /// Per-individual assignment data linking villagers to their crew aggregate and role.
    /// </summary>
    public struct Space4XCrewAssignment : IComponentData
    {
        public Entity CrewAggregate;
        public Entity HomeCarrier;
        public Entity CurrentCraft;
        public Space4XCrewDuty Duty;
    }
}
