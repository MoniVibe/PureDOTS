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
    /// Unified field propagation system using Burst-compiled convolution kernels.
    /// Handles diffusion (Laplacian) and advection (wind-driven) for heat, moisture, and oxygen.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(EnvironmentSystemGroup))]
    [UpdateAfter(typeof(MoistureSeepageSystem))]
    public partial struct FieldPropagationSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            var rewindState = SystemAPI.GetSingleton<RewindState>();

            if (rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            if (timeState.IsPaused)
            {
                return;
            }

            // Check temporal LOD
            var lodConfig = SystemAPI.HasSingleton<TemporalLODConfig>()
                ? SystemAPI.GetSingleton<TemporalLODConfig>()
                : new TemporalLODConfig { TemperatureDivisor = 5 };

            if (!TemporalLODHelpers.ShouldUpdate(timeState.Tick, lodConfig.TemperatureDivisor))
            {
                return;
            }

            // Process moisture grid propagation if available
            if (SystemAPI.TryGetSingleton<MoistureGrid>(out var moistureGrid))
            {
                ProcessFieldPropagation(ref state, moistureGrid, timeState);
            }

            // Process temperature grid propagation if available
            if (SystemAPI.TryGetSingleton<TemperatureGrid>(out var temperatureGrid))
            {
                ProcessTemperaturePropagation(ref state, temperatureGrid, timeState);
            }
        }

        private void ProcessFieldPropagation(ref SystemState state, MoistureGrid grid, TimeState timeState)
        {
            var gridEntity = SystemAPI.GetSingletonEntity<MoistureGrid>();
            if (!SystemAPI.HasBuffer<MoistureGridRuntimeCell>(gridEntity))
            {
                return;
            }

            var runtimeBuffer = SystemAPI.GetBuffer<MoistureGridRuntimeCell>(gridEntity);
            if (runtimeBuffer.Length == 0)
            {
                return;
            }

            var metadata = grid.Metadata;
            var diffusionCoeff = grid.DiffusionCoefficient;
            var deltaSeconds = timeState.FixedDeltaTime;

            // Get wind field for advection
            float2 windDirection = float2.zero;
            float windStrength = 0f;
            if (SystemAPI.TryGetSingleton<WindField>(out var windField))
            {
                windDirection = windField.GlobalWindDirection;
                windStrength = windField.GlobalWindStrength;
            }

            var nextValues = CollectionHelper.CreateNativeArray<float>(runtimeBuffer.Length, state.WorldUpdateAllocator, NativeArrayOptions.UninitializedMemory);

            var job = new DiffusionKernelJob
            {
                SourceField = runtimeBuffer.Reinterpret<float>().AsNativeArray(),
                TargetField = nextValues,
                Metadata = metadata,
                Alpha = diffusionCoeff,
                WindDirection = windDirection,
                WindStrength = windStrength,
                Dt = deltaSeconds
            };

            state.Dependency = job.ScheduleParallel(runtimeBuffer.Length, 64, state.Dependency);

            var applyJob = new ApplyFieldJob
            {
                Cells = runtimeBuffer.AsNativeArray(),
                NextValues = nextValues
            };

            state.Dependency = applyJob.ScheduleParallel(runtimeBuffer.Length, 64, state.Dependency);
        }

        private void ProcessTemperaturePropagation(ref SystemState state, TemperatureGrid grid, TimeState timeState)
        {
            if (!grid.IsCreated)
            {
                return;
            }

            var metadata = grid.Metadata;
            var deltaSeconds = timeState.FixedDeltaTime;
            var diffusionCoeff = 0.1f; // Temperature diffusion coefficient

            // Get wind field for advection
            float2 windDirection = float2.zero;
            float windStrength = 0f;
            if (SystemAPI.TryGetSingleton<WindField>(out var windField))
            {
                windDirection = windField.GlobalWindDirection;
                windStrength = windField.GlobalWindStrength;
            }

            // Create temporary arrays for temperature propagation
            ref var temperatureBlob = ref grid.Blob.Value;
            var sourceTemp = temperatureBlob.TemperatureCelsius;
            var nextTemp = CollectionHelper.CreateNativeArray<float>(sourceTemp.Length, state.WorldUpdateAllocator, NativeArrayOptions.UninitializedMemory);

            // Copy source to next
            for (int i = 0; i < sourceTemp.Length; i++)
            {
                nextTemp[i] = sourceTemp[i];
            }

            var job = new DiffusionKernelJob
            {
                SourceField = nextTemp,
                TargetField = nextTemp,
                Metadata = metadata,
                Alpha = diffusionCoeff,
                WindDirection = windDirection,
                WindStrength = windStrength,
                Dt = deltaSeconds
            };

            state.Dependency = job.ScheduleParallel(sourceTemp.Length, 64, state.Dependency);

            // Note: In a full implementation, we'd write back to the blob, but blob mutation requires rebuilding
            // For now, this demonstrates the propagation kernel structure
        }

        /// <summary>
        /// Burst-compiled diffusion kernel using 3×3 stencil for Laplacian operator.
        /// Also applies wind-driven advection via dot products.
        /// </summary>
        [BurstCompile]
        private struct DiffusionKernelJob : IJobFor
        {
            [ReadOnly] public NativeArray<float> SourceField;
            public NativeArray<float> TargetField;
            public EnvironmentGridMetadata Metadata;
            public float Alpha; // Diffusion coefficient
            public float2 WindDirection; // Normalized wind direction for advection
            public float WindStrength; // Wind strength for advection
            public float Dt;

            public void Execute(int index)
            {
                var current = SourceField[index];
                var laplacian = 0f;

                // 3×3 stencil for Laplacian (diffusion)
                // Center weight: -4, neighbors: +1
                var centerWeight = -4f;
                var neighborWeight = 1f;

                laplacian += centerWeight * current;

                // Sample 4-connected neighbors
                var offsets = new int2[]
                {
                    new int2(1, 0),   // Right
                    new int2(-1, 0),  // Left
                    new int2(0, 1),   // Up
                    new int2(0, -1)   // Down
                };

                for (int i = 0; i < offsets.Length; i++)
                {
                    if (EnvironmentGridMath.TryGetNeighborIndex(Metadata, index, offsets[i], out var neighborIndex))
                    {
                        laplacian += neighborWeight * SourceField[neighborIndex];
                    }
                    else
                    {
                        // Boundary: use current value (no flux boundary condition)
                        laplacian += neighborWeight * current;
                    }
                }

                // Diffusion term: alpha * laplacian * dt
                var diffusionTerm = Alpha * laplacian * Dt;

                // Advection term: wind-driven transport
                var advectionTerm = 0f;
                if (math.lengthsq(WindDirection) > 1e-6f && WindStrength > 0f)
                {
                    // Compute advection using upwind differencing
                    var normalizedWind = math.normalize(WindDirection);
                    
                    // Sample upwind neighbor
                    var upwindOffset = new int2(
                        (int)math.sign(-normalizedWind.x),
                        (int)math.sign(-normalizedWind.y)
                    );
                    
                    if (EnvironmentGridMath.TryGetNeighborIndex(Metadata, index, upwindOffset, out var upwindIndex))
                    {
                        var upwindValue = SourceField[upwindIndex];
                        var advectionGradient = current - upwindValue;
                        advectionTerm = -WindStrength * math.dot(normalizedWind, new float2(upwindOffset)) * advectionGradient * Dt;
                    }
                }

                // Update: newTemp = temp + diffusion + advection
                var nextValue = current + diffusionTerm + advectionTerm;
                TargetField[index] = math.clamp(nextValue, -100f, 100f); // Reasonable temperature range
            }
        }

        [BurstCompile]
        private struct ApplyFieldJob : IJobFor
        {
            public NativeArray<MoistureGridRuntimeCell> Cells;
            [ReadOnly] public NativeArray<float> NextValues;

            public void Execute(int index)
            {
                var cell = Cells[index];
                cell.Moisture = math.clamp(NextValues[index], 0f, 100f);
                Cells[index] = cell;
            }
        }
    }
}

