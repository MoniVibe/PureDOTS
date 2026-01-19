using PureDOTS.Runtime.Telemetry;
using PureDOTS.Systems;
using Unity.Burst;
using Unity.Entities;

namespace PureDOTS.Systems.Telemetry
{
    /// <summary>
    /// Applies scenario-provided telemetry overrides once at startup.
    /// </summary>
    [UpdateInGroup(typeof(TimeSystemGroup))]
    [UpdateAfter(typeof(TelemetryExportBootstrapSystem))]
    public partial struct TelemetryScenarioOverrideSystem : ISystem
    {
        private EntityQuery _overrideQuery;
        private EntityQuery _performanceSettingsQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TelemetryExportConfig>();
            _overrideQuery = state.GetEntityQuery(ComponentType.ReadOnly<TelemetryScenarioOverrideComponent>());
            _performanceSettingsQuery = state.GetEntityQuery(ComponentType.ReadWrite<PerformanceTelemetrySettings>());
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (_overrideQuery.IsEmptyIgnoreFilter)
            {
                state.Enabled = false;
                return;
            }

            var overrideEntity = _overrideQuery.GetSingletonEntity();
            var overrides = state.EntityManager.GetComponentData<TelemetryScenarioOverrideComponent>(overrideEntity).Value;
            var configRW = SystemAPI.GetSingletonRW<TelemetryExportConfig>();
            var config = configRW.ValueRO;

            if (ApplyOverride(ref config, overrides))
            {
                config.Version++;
                configRW.ValueRW = config;
            }

            ApplyPerformanceOverrides(ref state, overrides);

            state.EntityManager.DestroyEntity(overrideEntity);
            state.Enabled = false;
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state) { }

        private static bool ApplyOverride(ref TelemetryExportConfig config, in TelemetryScenarioOverride overrides)
        {
            bool changed = false;

            if (overrides.EnabledOverride >= 0)
            {
                config.Enabled = overrides.EnabledOverride == 1 ? (byte)1 : (byte)0;
                changed = true;
            }

            if (overrides.OutputPath.Length > 0)
            {
                config.OutputPath = overrides.OutputPath;
                changed = true;
            }

            if (overrides.RunId.Length > 0)
            {
                config.RunId = overrides.RunId;
                changed = true;
            }

            if (overrides.Flags != TelemetryExportFlags.None)
            {
                config.Flags = overrides.Flags;
                changed = true;
            }

            if (overrides.CadenceTicks > 0)
            {
                config.CadenceTicks = overrides.CadenceTicks;
                changed = true;
            }

            if (overrides.LodOverride >= 0)
            {
                config.Lod = (TelemetryExportLod)overrides.LodOverride;
                changed = true;
            }

            if (overrides.Loops != TelemetryLoopFlags.None)
            {
                config.Loops = overrides.Loops;
                changed = true;
            }

            if (overrides.MaxEventsPerTick > 0)
            {
                config.MaxEventsPerTick = overrides.MaxEventsPerTick;
                changed = true;
            }

            if (overrides.MaxOutputBytes > 0)
            {
                config.MaxOutputBytes = overrides.MaxOutputBytes;
                changed = true;
            }

            return changed;
        }

        private void ApplyPerformanceOverrides(ref SystemState state, in TelemetryScenarioOverride overrides)
        {
            if (_performanceSettingsQuery.IsEmptyIgnoreFilter)
            {
                return;
            }

            if (overrides.WarmupTicks < 0 && overrides.MeasureTicks < 0)
            {
                return;
            }

            var settingsEntity = _performanceSettingsQuery.GetSingletonEntity();
            var settings = state.EntityManager.GetComponentData<PerformanceTelemetrySettings>(settingsEntity);
            bool changed = false;

            if (overrides.WarmupTicks >= 0)
            {
                settings.WarmupTicks = (uint)overrides.WarmupTicks;
                changed = true;
            }

            if (overrides.MeasureTicks >= 0)
            {
                settings.MeasureTicks = (uint)overrides.MeasureTicks;
                changed = true;
            }

            if (changed)
            {
                state.EntityManager.SetComponentData(settingsEntity, settings);
            }
        }
    }
}
