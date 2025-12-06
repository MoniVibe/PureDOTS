using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Math;
using PureDOTS.Runtime.Physics;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Systems.Physics
{
    /// <summary>
    /// Macro collision system for objects > 10km radius (moons, planets).
    /// Uses energy field diffusion (dE/dt = k * ∇²E - loss * E) and thermo state updates.
    /// Runs at ~0.1 Hz (throttled).
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(MacroCollisionSystemGroup))]
    public partial struct MacroCollisionSystem : ISystem
    {
        private ComponentLookup<CollisionProperties> _collisionPropsLookup;
        private ComponentLookup<MacroEnergyFieldConfig> _configLookup;
        private ComponentLookup<MacroThermoState> _thermoStateLookup;
        private BufferLookup<MacroEnergyFieldElement> _energyFieldLookup;
        private BufferLookup<ImpactEvent> _impactEventLookup;

        private uint _lastUpdateTick;
        private const uint UPDATE_INTERVAL_TICKS = 10; // ~0.1 Hz at 60 ticks/sec

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
            _lastUpdateTick = 0;
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var rewindState = SystemAPI.GetSingleton<RewindState>();
            if (rewindState.Mode != RewindMode.Record)
                return;

            var timeState = SystemAPI.GetSingleton<TimeState>();

            // Throttle updates to ~0.1 Hz
            if (timeState.Tick - _lastUpdateTick < UPDATE_INTERVAL_TICKS)
                return;

            _lastUpdateTick = timeState.Tick;

            _collisionPropsLookup.Update(ref state);
            _configLookup.Update(ref state);
            _thermoStateLookup.Update(ref state);
            _energyFieldLookup.Update(ref state);
            _impactEventLookup.Update(ref state);

            var job = new ProcessMacroCollisionsJob
            {
                CollisionPropsLookup = _collisionPropsLookup,
                ConfigLookup = _configLookup,
                ThermoStateLookup = _thermoStateLookup,
                EnergyFieldLookup = _energyFieldLookup,
                ImpactEventLookup = _impactEventLookup,
                CurrentTick = timeState.Tick,
                DeltaTime = SystemAPI.Time.DeltaTime
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(CollisionProperties))]
        partial struct ProcessMacroCollisionsJob : IJobEntity
        {
            [ReadOnly] public ComponentLookup<CollisionProperties> CollisionPropsLookup;
            [ReadOnly] public ComponentLookup<MacroEnergyFieldConfig> ConfigLookup;
            public ComponentLookup<MacroThermoState> ThermoStateLookup;
            public BufferLookup<MacroEnergyFieldElement> EnergyFieldLookup;
            public BufferLookup<ImpactEvent> ImpactEventLookup;

            public uint CurrentTick;
            public float DeltaTime;

            public void Execute(Entity entity, ref CollisionProperties props)
            {
                // Only process macro regime entities
                if (props.Regime != CollisionRegime.Macro)
                    return;

                // Get config
                if (!ConfigLookup.HasComponent(entity))
                    return;

                var config = ConfigLookup[entity];

                // Process impact events and add energy to field
                ProcessImpacts(entity, config);

                // Diffuse energy field
                DiffuseEnergyField(entity, config);

                // Update thermo state from energy field
                UpdateThermoState(entity, config);
            }

            [BurstCompile]
            private void ProcessImpacts(Entity entity, MacroEnergyFieldConfig config)
            {
                if (!ImpactEventLookup.HasBuffer(entity))
                    return;

                var impactEvents = ImpactEventLookup[entity];
                if (!EnergyFieldLookup.HasBuffer(entity))
                    return;

                var energyField = EnergyFieldLookup[entity];

                for (int i = 0; i < impactEvents.Length; i++)
                {
                    var impact = impactEvents[i];
                    if (impact.Regime != CollisionRegime.Macro)
                        continue;

                    // Add energy to field at impact position
                    // For simplicity, distribute energy to nearest voxel cells
                    var cellIndex = GetNearestCellIndex(impact.Pos, config.VoxelResolution);
                    if (cellIndex >= 0 && cellIndex < energyField.Length)
                    {
                        var element = energyField[cellIndex];
                        element.EnergyDensity += impact.Q * config.QToTemperatureFactor;
                        energyField[cellIndex] = element;
                    }
                }
            }

            [BurstCompile]
            private void DiffuseEnergyField(Entity entity, MacroEnergyFieldConfig config)
            {
                if (!EnergyFieldLookup.HasBuffer(entity))
                    return;

                var energyField = EnergyFieldLookup[entity];
                var newField = new NativeArray<float>(energyField.Length, Allocator.Temp);

                // Copy current field
                for (int i = 0; i < energyField.Length; i++)
                {
                    newField[i] = energyField[i].EnergyDensity;
                }

                // Apply diffusion: dE/dt = k * ∇²E - loss * E
                var k = config.DiffusionCoefficient;
                var loss = config.EnergyLossCoefficient;
                var dt = DeltaTime;

                // Simple 1D diffusion approximation (can be extended to 3D)
                var resolution = (int)math.sqrt(config.VoxelResolution);
                for (int i = 0; i < resolution; i++)
                {
                    for (int j = 0; j < resolution; j++)
                    {
                        var idx = i * resolution + j;
                        var laplacian = ComputeLaplacian(newField, i, j, resolution);
                        var newValue = newField[idx] + dt * (k * laplacian - loss * newField[idx]);
                        newField[idx] = math.max(0f, newValue); // Clamp to non-negative
                    }
                }

                // Update energy field
                for (int i = 0; i < energyField.Length; i++)
                {
                    var element = energyField[i];
                    element.EnergyDensity = newField[i];
                    energyField[i] = element;
                }

                newField.Dispose();
            }

            [BurstCompile]
            private float ComputeLaplacian(NativeArray<float> field, int i, int j, int resolution)
            {
                var center = field[i * resolution + j];
                var sum = 0f;
                var count = 0;

                // 4-neighbor Laplacian
                if (i > 0)
                {
                    sum += field[(i - 1) * resolution + j];
                    count++;
                }
                if (i < resolution - 1)
                {
                    sum += field[(i + 1) * resolution + j];
                    count++;
                }
                if (j > 0)
                {
                    sum += field[i * resolution + (j - 1)];
                    count++;
                }
                if (j < resolution - 1)
                {
                    sum += field[i * resolution + (j + 1)];
                    count++;
                }

                return (sum - count * center) / math.max(1f, count);
            }

            [BurstCompile]
            private void UpdateThermoState(Entity entity, MacroEnergyFieldConfig config)
            {
                if (!ThermoStateLookup.HasComponent(entity) || !EnergyFieldLookup.HasBuffer(entity))
                    return;

                var energyField = EnergyFieldLookup[entity];
                var thermoState = ThermoStateLookup[entity];

                // Aggregate energy field data
                var totalEnergy = 0f;
                var maxEnergy = 0f;

                for (int i = 0; i < energyField.Length; i++)
                {
                    var energy = energyField[i].EnergyDensity;
                    totalEnergy += energy;
                    maxEnergy = math.max(maxEnergy, energy);
                }

                // Update temperature from total energy
                thermoState.Temperature = config.QToTemperatureFactor * totalEnergy / math.max(1f, energyField.Length);

                // Update melt percentage from energy density
                thermoState.MeltPercentage = math.min(1f, maxEnergy * config.EnergyToMeltFactor);

                // Update atmosphere loss from total energy
                thermoState.AtmosphereLossPercentage = math.min(1f, totalEnergy * config.EnergyToAtmosphereLossFactor);

                thermoState.TotalEnergy = totalEnergy;
                thermoState.LastUpdateTick = CurrentTick;

                ThermoStateLookup[entity] = thermoState;
            }

            [BurstCompile]
            private int GetNearestCellIndex(float3 position, int resolution)
            {
                // Simple mapping: assume entity is at origin, map position to cell
                // In real implementation, would use entity's transform and bounds
                var cellSize = 1f / resolution;
                var x = (int)math.clamp(position.x / cellSize, 0, resolution - 1);
                var z = (int)math.clamp(position.z / cellSize, 0, resolution - 1);
                return x * resolution + z;
            }
        }
    }
}

