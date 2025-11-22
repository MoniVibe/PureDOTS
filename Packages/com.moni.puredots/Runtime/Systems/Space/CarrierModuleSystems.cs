using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Space;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Systems.Space
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class CarrierModuleStatAggregationSystem : SystemBase
    {
        private ComponentLookup<ModuleStatModifier> _modifierLookup;
        private ComponentLookup<ModuleHealth> _healthLookup;
        private ComponentLookup<ShipModule> _moduleLookup;
        private ComponentLookup<CarrierPowerBudget> _powerLookup;

        protected override void OnCreate()
        {
            RequireForUpdate<TimeState>();
            RequireForUpdate<RewindState>();
        }

        [BurstCompile]
        protected override void OnUpdate()
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            if (timeState.IsPaused)
            {
                return;
            }

            var rewind = SystemAPI.GetSingleton<RewindState>();
            if (rewind.Mode != RewindMode.Record)
            {
                return;
            }

            _modifierLookup = GetComponentLookup<ModuleStatModifier>(true);
            _healthLookup = GetComponentLookup<ModuleHealth>(true);
            _moduleLookup = GetComponentLookup<ShipModule>(true);
            _powerLookup = GetComponentLookup<CarrierPowerBudget>(false);

            var modifierLookup = _modifierLookup;
            var healthLookup = _healthLookup;
            var moduleLookup = _moduleLookup;
            var powerLookup = _powerLookup;

            Entities
                .WithName("CarrierModuleStatAggregation")
                .WithReadOnly(modifierLookup)
                .WithReadOnly(healthLookup)
                .WithReadOnly(moduleLookup)
                .ForEach((Entity carrier, ref CarrierModuleStatTotals totals, in DynamicBuffer<CarrierModuleSlot> slots) =>
                {
                    var aggregated = new CarrierModuleStatTotals();
                    var power = powerLookup.HasComponent(carrier) ? powerLookup[carrier] : default;

                    for (int i = 0; i < slots.Length; i++)
                    {
                        var slot = slots[i];
                        if (!modifierLookup.HasComponent(slot.InstalledModule))
                        {
                            continue;
                        }

                        var modifier = modifierLookup[slot.InstalledModule];
                        var healthScale = 1f;
                        byte integrity = 100;
                        bool healthBelowThreshold = false;

                        if (healthLookup.HasComponent(slot.InstalledModule))
                        {
                            var health = healthLookup[slot.InstalledModule];
                            integrity = health.Integrity;
                            healthBelowThreshold = integrity <= health.FailureThreshold;
                            healthScale = math.clamp(integrity / 100f, 0f, 1f);
                        }

                        aggregated.TotalMass += modifier.Mass;
                        aggregated.TotalPowerDraw += modifier.PowerDraw * healthScale;
                        aggregated.TotalPowerGeneration += modifier.PowerGeneration * healthScale;
                        aggregated.TotalCargoCapacity += modifier.CargoCapacity * healthScale;
                        aggregated.TotalMiningRate += modifier.MiningRate * healthScale;
                        aggregated.TotalRepairRateBonus += modifier.RepairRateBonus * healthScale;

                        power.CurrentDraw += modifier.PowerDraw * healthScale;
                        power.CurrentGeneration += modifier.PowerGeneration * healthScale;

                        if (moduleLookup.HasComponent(slot.InstalledModule))
                        {
                            var module = moduleLookup[slot.InstalledModule];
                            if (module.State == ModuleState.Destroyed)
                            {
                                aggregated.DestroyedModuleCount++;
                            }
                            else if (module.State == ModuleState.Damaged || healthBelowThreshold)
                            {
                                aggregated.DamagedModuleCount++;
                            }
                        }
                        else if (integrity == 0)
                        {
                            aggregated.DestroyedModuleCount++;
                        }
                    }

                    totals = aggregated;

                    if (powerLookup.HasComponent(carrier))
                    {
                        power.OverBudget = power.MaxPowerOutput > 0f && power.CurrentDraw > power.MaxPowerOutput;
                        powerLookup[carrier] = power;
                    }
                }).ScheduleParallel();

            Dependency.Complete();
        }
    }

    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class ModuleDegradationSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireForUpdate<TimeState>();
            RequireForUpdate<RewindState>();
        }

        [BurstCompile]
        protected override void OnUpdate()
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            if (timeState.IsPaused)
            {
                return;
            }

            var rewind = SystemAPI.GetSingleton<RewindState>();
            if (rewind.Mode != RewindMode.Record)
            {
                return;
            }

            var scaledDelta = timeState.FixedDeltaTime * timeState.CurrentSpeedMultiplier;

            var repairQueueLookup = GetBufferLookup<ModuleRepairTicket>();

            Entities
                .WithName("ModuleDegradation")
                .WithoutBurst()
                .ForEach((Entity entity, ref ModuleHealth health, ref ShipModule module,
                    in ModuleDegradation degradation, in Parent parent) =>
            {
                if (module.State == ModuleState.Destroyed)
                {
                    return;
                }

                var decay = math.max(0f, degradation.PassivePerSecond);
                if (module.State == ModuleState.Active)
                {
                    decay += math.max(0f, degradation.ActivePerSecond);
                }

                var newIntegrity = math.max(0f, health.Integrity - decay * scaledDelta);
                health.Integrity = (byte)newIntegrity;

                if (health.Integrity == 0)
                {
                    module.State = ModuleState.Destroyed;
                }

                if (health.Integrity <= health.FailureThreshold)
                {
                    module.State = ModuleState.Damaged;

                    if (!health.NeedsRepair)
                    {
                        health.MarkRepairRequested();
                    }

                    if (!repairQueueLookup.HasBuffer(parent.Value))
                    {
                        return;
                    }

                    var buffer = repairQueueLookup[parent.Value];
                    if (ContainsTicket(buffer, entity))
                    {
                        return;
                    }

                    buffer.Add(new ModuleRepairTicket
                    {
                        Module = entity,
                        Kind = ModuleRepairKind.Field,
                        Priority = health.RepairPriority,
                        RemainingWork = math.max(0.1f, (100 - health.Integrity) * 0.1f)
                    });
                }
            }).Run();
        }

        private static bool ContainsTicket(DynamicBuffer<ModuleRepairTicket> buffer, Entity module)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].Module == module)
                {
                    return true;
                }
            }

            return false;
        }
    }

    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ModuleDegradationSystem))]
    public partial class CarrierModuleRepairSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireForUpdate<TimeState>();
            RequireForUpdate<RewindState>();
        }

        protected override void OnUpdate()
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            if (timeState.IsPaused)
            {
                return;
            }

            var rewind = SystemAPI.GetSingleton<RewindState>();
            if (rewind.Mode != RewindMode.Record)
            {
                return;
            }

            var delta = timeState.FixedDeltaTime * timeState.CurrentSpeedMultiplier;
            var healthLookup = GetComponentLookup<ModuleHealth>(false);
            var moduleLookup = GetComponentLookup<ShipModule>(false);

            foreach (var (refitState, carrierEntity) in SystemAPI.Query<RefRO<CarrierRefitState>>().WithEntityAccess())
            {
                if (!EntityManager.HasBuffer<ModuleRepairTicket>(carrierEntity))
                {
                    continue;
                }

                var tickets = EntityManager.GetBuffer<ModuleRepairTicket>(carrierEntity);
                if (tickets.IsEmpty)
                {
                    continue;
                }

                var index = FindHighestPriorityIndex(tickets);
                if (index < 0)
                {
                    continue;
                }

                var ticket = tickets[index];
                var repairRate = ticket.Kind == ModuleRepairKind.Station && refitState.ValueRO.AtRefitFacility
                    ? math.max(refitState.ValueRO.StationRefitRate, refitState.ValueRO.FieldRefitRate)
                    : refitState.ValueRO.FieldRefitRate;

                if (refitState.ValueRO.AtRefitFacility)
                {
                    repairRate = math.max(repairRate, refitState.ValueRO.StationRefitRate);
                }

                if (repairRate <= 0f)
                {
                    continue;
                }

                ticket.RemainingWork -= repairRate * delta;
                if (ticket.RemainingWork <= 0f)
                {
                    if (healthLookup.HasComponent(ticket.Module))
                    {
                        var health = healthLookup[ticket.Module];
                        health.Integrity = 100;
                        health.ClearRepairRequested();
                        healthLookup[ticket.Module] = health;
                    }

                    if (moduleLookup.HasComponent(ticket.Module))
                    {
                        var module = moduleLookup[ticket.Module];
                        if (module.State != ModuleState.Destroyed)
                        {
                            module.State = ModuleState.Standby;
                            moduleLookup[ticket.Module] = module;
                        }
                    }

                    tickets.RemoveAtSwapBack(index);
                }
                else
                {
                    tickets[index] = ticket;
                }
            }
        }

        private static int FindHighestPriorityIndex(DynamicBuffer<ModuleRepairTicket> tickets)
        {
            var bestIndex = -1;
            byte bestPriority = 0;

            for (int i = 0; i < tickets.Length; i++)
            {
                if (bestIndex == -1 || tickets[i].Priority > bestPriority)
                {
                    bestIndex = i;
                    bestPriority = tickets[i].Priority;
                }
            }

            return bestIndex;
        }
    }

    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(CarrierModuleRepairSystem))]
    public partial class CarrierModuleRefitSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireForUpdate<TimeState>();
            RequireForUpdate<RewindState>();
        }

        protected override void OnUpdate()
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            if (timeState.IsPaused)
            {
                return;
            }

            var rewind = SystemAPI.GetSingleton<RewindState>();
            if (rewind.Mode != RewindMode.Record)
            {
                return;
            }

            var delta = timeState.FixedDeltaTime * timeState.CurrentSpeedMultiplier;
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            var powerLookup = GetComponentLookup<CarrierPowerBudget>(true);

            foreach (var (refitState, carrierEntity) in SystemAPI.Query<RefRO<CarrierRefitState>>().WithEntityAccess())
            {
                if (!EntityManager.HasBuffer<CarrierModuleSlot>(carrierEntity) || !EntityManager.HasBuffer<CarrierModuleRefitRequest>(carrierEntity))
                {
                    continue;
                }

                var slots = EntityManager.GetBuffer<CarrierModuleSlot>(carrierEntity);
                var requests = EntityManager.GetBuffer<CarrierModuleRefitRequest>(carrierEntity);

                if (requests.IsEmpty)
                {
                    continue;
                }

                var refitRate = refitState.ValueRO.AtRefitFacility
                    ? math.max(refitState.ValueRO.StationRefitRate, refitState.ValueRO.FieldRefitRate)
                    : refitState.ValueRO.FieldRefitRate;

                if (refitRate <= 0f)
                {
                    continue;
                }

                for (int i = requests.Length - 1; i >= 0; i--)
                {
                    var request = requests[i];
                    if (request.RequiresStation && !refitState.ValueRO.AtRefitFacility)
                    {
                        continue;
                    }

                    if (powerLookup.HasComponent(carrierEntity))
                    {
                        var power = powerLookup[carrierEntity];
                        if (power.MaxPowerOutput > 0f && power.CurrentDraw > power.MaxPowerOutput)
                        {
                            // Block refit when over budget to avoid growing the deficit.
                            continue;
                        }
                    }

                    request.WorkRemaining -= refitRate * delta;

                    if (request.WorkRemaining > 0f)
                    {
                        requests[i] = request;
                        continue;
                    }

                    var newModule = Entity.Null;
                    if (request.NewModulePrefab != Entity.Null)
                    {
                        newModule = EntityManager.Instantiate(request.NewModulePrefab);
                        ecb.AddComponent(newModule, new Parent { Value = carrierEntity });
                    }

                    ReplaceSlotModule(slots, request.SlotIndex, request.ExistingModule, newModule, ref ecb);
                    requests.RemoveAtSwapBack(i);
                }
            }

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }

        private static void ReplaceSlotModule(DynamicBuffer<CarrierModuleSlot> slots, byte slotIndex,
            Entity existing, Entity replacement, ref EntityCommandBuffer ecb)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].SlotIndex != slotIndex)
                {
                    continue;
                }

                var slot = slots[i];

                if (existing != Entity.Null)
                {
                    ecb.DestroyEntity(existing);
                }

                slot.InstalledModule = replacement;
                slots[i] = slot;
                return;
            }
        }
    }
}
