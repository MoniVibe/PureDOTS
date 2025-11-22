using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Ships;
using PureDOTS.Runtime.Skills;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Systems.Ships
{
    /// <summary>
    /// Processes carrier repair queues, applying skill-based throughput and facility gating.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ModuleDegradationSystem))]
    public partial struct ModuleRepairSystem : ISystem
    {
        private BufferLookup<ModuleRepairTicket> _repairLookup;
        private ComponentLookup<ModuleHealth> _healthLookup;
        private ComponentLookup<ModuleRepairSettings> _settingsLookup;
        private ComponentLookup<CarrierRefitState> _refitStateLookup;
        private ComponentLookup<SkillSet> _skillLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ModuleRepairTicket>();
            state.RequireForUpdate<TimeState>();
            _repairLookup = state.GetBufferLookup<ModuleRepairTicket>(false);
            _healthLookup = state.GetComponentLookup<ModuleHealth>(false);
            _settingsLookup = state.GetComponentLookup<ModuleRepairSettings>(true);
            _refitStateLookup = state.GetComponentLookup<CarrierRefitState>(true);
            _skillLookup = state.GetComponentLookup<SkillSet>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            if (timeState.IsPaused || (SystemAPI.TryGetSingleton(out RewindState rewindState) && rewindState.Mode != RewindMode.Record))
            {
                return;
            }

            _repairLookup.Update(ref state);
            _healthLookup.Update(ref state);
            _settingsLookup.Update(ref state);
            _refitStateLookup.Update(ref state);
            _skillLookup.Update(ref state);

            var repairQuery = SystemAPI.QueryBuilder()
                .WithAll<ModuleRepairTicket>()
                .Build();
            var entities = repairQuery.ToEntityArray(state.WorldUpdateAllocator);

            foreach (var entity in entities)
            {
                var tickets = state.EntityManager.GetBuffer<ModuleRepairTicket>(entity);
                if (tickets.Length == 0)
                {
                    continue;
                }

                var settings = _settingsLookup.HasComponent(entity)
                    ? _settingsLookup[entity]
                    : ModuleRepairSettings.CreateDefaults();

                var refitState = _refitStateLookup.HasComponent(entity)
                    ? _refitStateLookup[entity]
                    : default;

                var skillLevel = _skillLookup.HasComponent(entity)
                    ? _skillLookup[entity].GetLevel(SkillId.ShipRepair)
                    : (byte)0;

                var maxRepairs = math.max(1, settings.MaxConcurrent);
                var repairsPerformed = 0;

                while (tickets.Length > 0 && repairsPerformed < maxRepairs)
                {
                    var index = ModuleMaintenanceUtility.SelectTicketIndex(tickets);
                    if (index < 0 || index >= tickets.Length)
                    {
                        break;
                    }

                    var ticket = tickets[index];
                    if (!_healthLookup.HasComponent(ticket.Module) || !state.EntityManager.Exists(ticket.Module))
                    {
                        tickets.RemoveAt(index);
                        continue;
                    }

                    var health = _healthLookup[ticket.Module];
                    if (health.State == ModuleHealthState.Destroyed)
                    {
                        tickets.RemoveAt(index);
                        continue;
                    }

                    var stationAvailable = refitState.InRefitFacility != 0;
                    if (ticket.Kind == ModuleRepairKind.Station && !stationAvailable)
                    {
                        ticket.Severity = ModuleMaintenanceUtility.CalculateSeverity(health);
                        tickets[index] = ticket;
                        break;
                    }

                    if (ticket.Kind == ModuleRepairKind.Field && settings.AllowFieldRepairs == 0)
                    {
                        ticket.Kind = ModuleRepairKind.Station;
                        tickets[index] = ticket;
                        continue;
                    }

                    var rate = ModuleMaintenanceUtility.CalculateRepairRate(settings, ticket.Kind, skillLevel);
                    var repaired = math.min(rate, health.MaxHealth - health.Health);
                    health.Health += repaired;
                    ModuleMaintenanceUtility.ResolveState(ref health);
                    health.Flags &= ~ModuleHealthFlags.PendingRepairQueue;
                    health.LastProcessedTick = timeState.Tick;
                    _healthLookup[ticket.Module] = health;

                    if (health.State == ModuleHealthState.Nominal)
                    {
                        tickets.RemoveAt(index);
                    }
                    else
                    {
                        ticket.Severity = ModuleMaintenanceUtility.CalculateSeverity(health);
                        tickets[index] = ticket;
                    }

                    repairsPerformed++;
                }
            }
        }
    }
}
