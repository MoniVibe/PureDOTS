using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Space;
using PureDOTS.Runtime.Telemetry;
using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Systems.Space
{
    internal static class CarrierModuleTelemetryKeys
    {
        public static readonly FixedString64Bytes Active = "carrier.modules.active";
        public static readonly FixedString64Bytes Damaged = "carrier.modules.damaged";
        public static readonly FixedString64Bytes Destroyed = "carrier.modules.destroyed";
        public static readonly FixedString64Bytes RepairTickets = "carrier.modules.repair_tickets";
        public static readonly FixedString64Bytes PowerDraw = "carrier.power.draw";
        public static readonly FixedString64Bytes PowerGeneration = "carrier.power.generation";
        public static readonly FixedString64Bytes PowerNet = "carrier.power.net";
        public static readonly FixedString64Bytes PowerOverbudget = "carrier.power.overbudget";
    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(CarrierModuleStatAggregationSystem))]
    public partial class CarrierModuleTelemetrySystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireForUpdate<TimeState>();
            RequireForUpdate<RewindState>();
            RequireForUpdate<TelemetryStream>();
        }

        protected override void OnUpdate()
        {
            var time = SystemAPI.GetSingleton<TimeState>();
            if (time.IsPaused)
            {
                return;
            }

            var rewind = SystemAPI.GetSingleton<RewindState>();
            if (rewind.Mode != RewindMode.Record)
            {
                return;
            }

            var telemetry = new CarrierModuleTelemetry();

            foreach (var (totals, repairTickets, power) in SystemAPI.Query<CarrierModuleStatTotals, DynamicBuffer<ModuleRepairTicket>, CarrierPowerBudget>())
            {
                telemetry.CarrierCount++;
                telemetry.ActiveModules += totals.DamagedModuleCount + totals.DestroyedModuleCount == 0 ? 1 : 0;
                telemetry.DamagedModules += totals.DamagedModuleCount;
                telemetry.DestroyedModules += totals.DestroyedModuleCount;
                telemetry.RepairTicketCount += repairTickets.Length;
                telemetry.TotalPowerDraw += power.CurrentDraw;
                telemetry.TotalPowerGeneration += power.CurrentGeneration;
                telemetry.NetPower += power.CurrentGeneration - power.CurrentDraw;
                telemetry.AnyOverBudget |= power.OverBudget;
            }

            if (!SystemAPI.HasSingleton<CarrierModuleTelemetry>())
            {
                var entity = EntityManager.CreateEntity(typeof(CarrierModuleTelemetry));
                EntityManager.SetComponentData(entity, telemetry);
            }
            else
            {
                SystemAPI.SetSingleton(telemetry);
            }

            // Emit telemetry metrics via hub with standard keys
            TelemetryHub.Enqueue(new TelemetryMetric { Key = CarrierModuleTelemetryKeys.Active, Value = telemetry.ActiveModules, Unit = TelemetryMetricUnit.Count });
            TelemetryHub.Enqueue(new TelemetryMetric { Key = CarrierModuleTelemetryKeys.Damaged, Value = telemetry.DamagedModules, Unit = TelemetryMetricUnit.Count });
            TelemetryHub.Enqueue(new TelemetryMetric { Key = CarrierModuleTelemetryKeys.Destroyed, Value = telemetry.DestroyedModules, Unit = TelemetryMetricUnit.Count });
            TelemetryHub.Enqueue(new TelemetryMetric { Key = CarrierModuleTelemetryKeys.RepairTickets, Value = telemetry.RepairTicketCount, Unit = TelemetryMetricUnit.Count });
            TelemetryHub.Enqueue(new TelemetryMetric { Key = CarrierModuleTelemetryKeys.PowerDraw, Value = telemetry.TotalPowerDraw, Unit = TelemetryMetricUnit.None });
            TelemetryHub.Enqueue(new TelemetryMetric { Key = CarrierModuleTelemetryKeys.PowerGeneration, Value = telemetry.TotalPowerGeneration, Unit = TelemetryMetricUnit.None });
            TelemetryHub.Enqueue(new TelemetryMetric { Key = CarrierModuleTelemetryKeys.PowerNet, Value = telemetry.NetPower, Unit = TelemetryMetricUnit.None });
            TelemetryHub.Enqueue(new TelemetryMetric { Key = CarrierModuleTelemetryKeys.PowerOverbudget, Value = telemetry.AnyOverBudget ? 1 : 0, Unit = TelemetryMetricUnit.Count });
        }
    }
}
