using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Registry;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Systems
{
    /// <summary>
    /// Maintains a registry of all villagers for fast lookup by other systems.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(VillagerSystemGroup), OrderFirst = true)]
    public partial struct VillagerRegistrySystem : ISystem
    {
        private EntityQuery _villagerQuery;
        private ComponentLookup<VillagerJobTicket> _ticketLookup;
        private ComponentLookup<VillagerDisciplineState> _disciplineLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _villagerQuery = SystemAPI.QueryBuilder()
                .WithAll<VillagerId, VillagerJob, VillagerAvailability, LocalTransform>()
                .WithNone<VillagerDeadTag>()
                .Build();

            _ticketLookup = state.GetComponentLookup<VillagerJobTicket>(true);
            _disciplineLookup = state.GetComponentLookup<VillagerDisciplineState>(true);

            state.RequireForUpdate<VillagerRegistry>();
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
            state.RequireForUpdate(_villagerQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            var rewindState = SystemAPI.GetSingleton<RewindState>();
            if (timeState.IsPaused || rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            _ticketLookup.Update(ref state);
            _disciplineLookup.Update(ref state);

            var registryEntity = SystemAPI.GetSingletonEntity<VillagerRegistry>();
            var registry = SystemAPI.GetComponentRW<VillagerRegistry>(registryEntity);
            var entries = state.EntityManager.GetBuffer<VillagerRegistryEntry>(registryEntity);
            ref var registryMetadata = ref SystemAPI.GetComponentRW<RegistryMetadata>(registryEntity).ValueRW;

            var totalVillagers = 0;

            var expectedCount = math.max(32, _villagerQuery.CalculateEntityCount());
            using var builder = new DeterministicRegistryBuilder<VillagerRegistryEntry>(expectedCount, Allocator.Temp);
            foreach (var (villagerId, job, availability, transform, entity) in SystemAPI.Query<RefRO<VillagerId>, RefRO<VillagerJob>, RefRO<VillagerAvailability>, RefRO<LocalTransform>>()
                         .WithNone<VillagerDeadTag, PlaybackGuardTag>()
                         .WithEntityAccess())
            {
                var entry = new VillagerRegistryEntry
                {
                    VillagerEntity = entity,
                    VillagerId = villagerId.ValueRO.Value,
                    FactionId = villagerId.ValueRO.FactionId,
                    Position = transform.ValueRO.Position,
                    JobType = job.ValueRO.Type,
                    JobPhase = job.ValueRO.Phase,
                    ActiveTicketId = job.ValueRO.ActiveTicketId,
                    AvailabilityFlags = VillagerAvailabilityFlags.FromAvailability(availability.ValueRO),
                    CurrentResourceTypeIndex = ushort.MaxValue,
                    Discipline = (byte)VillagerDisciplineType.Unassigned
                };

                if (_ticketLookup.HasComponent(entity))
                {
                    var ticket = _ticketLookup[entity];
                    entry.ActiveTicketId = ticket.TicketId;
                    entry.CurrentResourceTypeIndex = ticket.ResourceTypeIndex;
                }

                if (_disciplineLookup.HasComponent(entity))
                {
                    var discipline = _disciplineLookup[entity];
                    entry.Discipline = (byte)discipline.Value;
                }

                builder.Add(entry);

                totalVillagers++;
            }

            builder.ApplyTo(ref entries);
            registryMetadata.MarkUpdated(entries.Length, timeState.Tick);
            registryMetadata.MarkUpdated(entries.Length, timeState.Tick);

            var availableCount = 0;
            for (var i = 0; i < entries.Length; i++)
            {
                if ((entries[i].AvailabilityFlags & VillagerAvailabilityFlags.Available) != 0)
                {
                    availableCount++;
                }
            }

            registry.ValueRW = new VillagerRegistry
            {
                TotalVillagers = totalVillagers,
                AvailableVillagers = availableCount,
                LastUpdateTick = timeState.Tick
            };
        }
    }
}
