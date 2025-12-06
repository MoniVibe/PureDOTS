using PureDOTS.Environment;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Time;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace PureDOTS.Systems.Environment
{
    /// <summary>
    /// Updates biodeck climate and atmosphere at reduced tick rate (0.5 Hz default).
    /// Links to ship modules (reactor → heat, hull breaches → pressure, radiation → heat).
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(EnvironmentSystemGroup))]
    public partial struct BiodeckSystem : ISystem
    {
        private TimeAwareController _timeAware;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();

            _timeAware = new TimeAwareController(
                TimeAwareExecutionPhase.Record | TimeAwareExecutionPhase.CatchUp,
                TimeAwareExecutionOptions.SkipWhenPaused);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            var rewindState = SystemAPI.GetSingleton<RewindState>();

            if (!_timeAware.TryBegin(timeState, rewindState, out _))
            {
                return;
            }

            // Process biodecks at reduced cadence (default: every 2 ticks = 0.5 Hz)
            var biodeckQuery = SystemAPI.QueryBuilder()
                .WithAll<BiodeckClimate, BiodeckAtmosphere>()
                .Build();

            if (biodeckQuery.IsEmpty)
            {
                return;
            }

            var job = new UpdateBiodeckJob
            {
                CurrentTick = timeState.Tick,
                DeltaTime = timeState.FixedDeltaTime
            };

            state.Dependency = job.ScheduleParallel(biodeckQuery, state.Dependency);
        }

        [BurstCompile]
        private partial struct UpdateBiodeckJob : IJobEntity
        {
            public uint CurrentTick;
            public float DeltaTime;

            public void Execute(ref BiodeckClimate climate, ref BiodeckAtmosphere atmosphere,
                in BiodeckSimulationConfig config, in BiodeckModuleLink moduleLink)
            {
                // Check tick divisor (update every N ticks)
                var tickDivisor = config.TickDivisor > 0 ? config.TickDivisor : 2u;
                if (CurrentTick % tickDivisor != 0)
                {
                    return;
                }

                // Update climate toward target values
                var tempDelta = (config.TargetTemperature - climate.Temperature) * 0.1f * DeltaTime;
                climate.Temperature = math.lerp(climate.Temperature, config.TargetTemperature, 0.1f * DeltaTime);

                var humidityDelta = (config.TargetHumidity - climate.Humidity) * 0.1f * DeltaTime;
                climate.Humidity = math.clamp(climate.Humidity + humidityDelta, 0f, 100f);

                // Update atmosphere toward target
                var oxygenDelta = (config.TargetOxygen - atmosphere.Oxygen) * 0.05f * DeltaTime;
                atmosphere.Oxygen = math.clamp(atmosphere.Oxygen + oxygenDelta, 0f, 100f);

                // Module effects would be applied here (reactor heat, hull pressure, radiation leaks)
                // For now, simplified update

                climate.LastUpdateTick = CurrentTick;
            }
        }
    }
}

