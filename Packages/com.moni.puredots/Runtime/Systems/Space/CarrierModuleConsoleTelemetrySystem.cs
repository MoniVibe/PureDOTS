using PureDOTS.Runtime.Space;
using PureDOTS.Runtime.Components;
using Unity.Entities;
using UnityEngine;

namespace PureDOTS.Systems.Space
{
    /// <summary>
    /// Emits lightweight console telemetry for module health/repair/power to make ScenarioRunner smoke observable without HUD.
    /// Logs at most once per 60 ticks or when queue/over-budget states change.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(CarrierModuleTelemetrySystem))]
    public partial class CarrierModuleConsoleTelemetrySystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireForUpdate<CarrierModuleTelemetry>();
            RequireForUpdate<TimeState>();
        }

        protected override void OnUpdate()
        {
            var time = SystemAPI.GetSingleton<TimeState>();
            if (time.IsPaused)
            {
                return;
            }

            var telemetry = SystemAPI.GetSingleton<CarrierModuleTelemetry>();

            if (!SystemAPI.TryGetSingletonRW<CarrierModuleTelemetryLogState>(out var logStateRw))
            {
                var entity = EntityManager.CreateEntity(typeof(CarrierModuleTelemetryLogState));
                logStateRw = SystemAPI.GetSingletonRW<CarrierModuleTelemetryLogState>();
            }

            ref var logState = ref logStateRw.ValueRW;
            var tick = time.Tick;
            var shouldLog = telemetry.RepairTicketCount != logState.LastTicketCount
                            || telemetry.AnyOverBudget != logState.LastOverBudget
                            || tick - logState.LastLoggedTick >= 60;

            if (!shouldLog)
            {
                return;
            }

            logState.LastLoggedTick = tick;
            logState.LastTicketCount = telemetry.RepairTicketCount;
            logState.LastOverBudget = telemetry.AnyOverBudget;

            Debug.Log($"[ModuleTelemetry] carriers={telemetry.CarrierCount} modules: damaged={telemetry.DamagedModules} destroyed={telemetry.DestroyedModules} tickets={telemetry.RepairTicketCount} power(draw/gen/net)=({telemetry.TotalPowerDraw:F1}/{telemetry.TotalPowerGeneration:F1}/{telemetry.NetPower:F1}) overBudget={telemetry.AnyOverBudget}");
        }
    }
}
