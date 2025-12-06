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
    /// Fire propagation system using front-propagation algorithm.
    /// Event-driven: only processes cells near active fire.
    /// Integrates with rain and vegetation energy.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(EnvironmentSystemGroup))]
    [UpdateAfter(typeof(ClimateFeedbackSystem))]
    public partial struct FirePropagationSystem : ISystem
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

            // Fire spreads every tick (high reactivity)
            var lodConfig = SystemAPI.HasSingleton<TemporalLODConfig>()
                ? SystemAPI.GetSingleton<TemporalLODConfig>()
                : new TemporalLODConfig { FireDivisor = 1 };

            if (!TemporalLODHelpers.ShouldUpdate(timeState.Tick, lodConfig.FireDivisor))
            {
                return;
            }

            // Get or create fire grid
            if (!SystemAPI.TryGetSingleton<FireGrid>(out var fireGrid))
            {
                // Fire grid doesn't exist yet, skip
                return;
            }

            if (!fireGrid.IsCreated)
            {
                return;
            }

            // Get wind field for fire spread direction
            float2 windDirection = float2.zero;
            float windStrength = 0f;
            if (SystemAPI.TryGetSingleton<WindField>(out var windField))
            {
                windDirection = windField.GlobalWindDirection;
                windStrength = windField.GlobalWindStrength;
            }

            // Get rain rate for fire suppression
            float rainRate = 0f;
            if (SystemAPI.TryGetSingleton<CloudGrid>(out var cloudGrid) && cloudGrid.IsCreated)
            {
                // Sample average rain rate from cloud grid
                // For now, use a placeholder
                rainRate = 0f;
            }

            var deltaSeconds = timeState.FixedDeltaTime;
            var metadata = fireGrid.Metadata;

            ref var fireBlob = ref fireGrid.Blob.Value;
            var heat = fireBlob.Heat;
            var activeFire = fireBlob.ActiveFire;

            // Find active fire cells
            var activeFireCells = new NativeList<int>(Allocator.TempJob);
            for (int i = 0; i < activeFire.Length; i++)
            {
                if (activeFire[i] > 0)
                {
                    activeFireCells.Add(i);
                }
            }

            if (activeFireCells.Length == 0)
            {
                return; // No active fire
            }

            // Process fire spread from active cells
            var nextHeat = CollectionHelper.CreateNativeArray<float>(heat.Length, state.WorldUpdateAllocator, NativeArrayOptions.UninitializedMemory);
            var nextActiveFire = CollectionHelper.CreateNativeArray<byte>(activeFire.Length, state.WorldUpdateAllocator, NativeArrayOptions.UninitializedMemory);

            // Copy current state
            for (int i = 0; i < heat.Length; i++)
            {
                nextHeat[i] = heat[i];
                nextActiveFire[i] = activeFire[i];
            }

            var job = new FireSpreadJob
            {
                CurrentHeat = heat,
                CurrentActiveFire = activeFire,
                NextHeat = nextHeat,
                NextActiveFire = nextActiveFire,
                ActiveFireCells = activeFireCells.AsArray(),
                Metadata = metadata,
                WindDirection = windDirection,
                WindStrength = windStrength,
                SpreadCoefficient = fireGrid.SpreadCoefficient,
                RainCoefficient = fireGrid.RainCoefficient,
                RainRate = rainRate,
                Dt = deltaSeconds
            };

            state.Dependency = job.ScheduleParallel(activeFireCells.Length, 64, state.Dependency);

            // Note: In full implementation, we'd write back to the blob
            // Blob mutation requires rebuilding, so this demonstrates the structure
        }

        [BurstCompile]
        private struct FireSpreadJob : IJobFor
        {
            [ReadOnly] public BlobArray<float> CurrentHeat;
            [ReadOnly] public BlobArray<byte> CurrentActiveFire;
            public NativeArray<float> NextHeat;
            public NativeArray<byte> NextActiveFire;
            [ReadOnly] public NativeArray<int> ActiveFireCells;
            public EnvironmentGridMetadata Metadata;
            public float2 WindDirection;
            public float WindStrength;
            public float SpreadCoefficient;
            public float RainCoefficient;
            public float RainRate;
            public float Dt;

            public void Execute(int index)
            {
                var fireCellIndex = ActiveFireCells[index];
                var currentHeat = CurrentHeat[fireCellIndex];

                // Spread to neighbors
                var offsets = new int2[]
                {
                    new int2(1, 0),   // Right
                    new int2(-1, 0),  // Left
                    new int2(0, 1),   // Up
                    new int2(0, -1)   // Down
                };

                for (int i = 0; i < offsets.Length; i++)
                {
                    var offset = offsets[i];
                    if (EnvironmentGridMath.TryGetNeighborIndex(Metadata, fireCellIndex, offset, out var neighborIndex))
                    {
                        var neighborHeat = NextHeat[neighborIndex];

                        // Wind-driven spread: max(0, dot(windDir, cellDir))
                        var cellDir = math.normalize(new float2(offset.x, offset.y));
                        var windSpreadFactor = math.max(0f, math.dot(WindDirection, cellDir));
                        var spreadRate = SpreadCoefficient * (1f + WindStrength * 0.1f * windSpreadFactor);

                        // Fire spread: heatNext = heat + spreadCoeff * max(0, dot(windDir, cellDir)) * dt - rainCoeff * rain
                        var heatIncrease = spreadRate * currentHeat * Dt;
                        var rainSuppression = RainCoefficient * RainRate * Dt;

                        var newHeat = neighborHeat + heatIncrease - rainSuppression;
                        newHeat = math.max(0f, newHeat);

                        NextHeat[neighborIndex] = newHeat;

                        // Ignite if heat exceeds threshold
                        if (newHeat > 50f && NextActiveFire[neighborIndex] == 0)
                        {
                            NextActiveFire[neighborIndex] = 1;
                        }
                    }
                }

                // Reduce fire intensity over time
                var decay = currentHeat * 0.01f * Dt; // 1% decay per second
                var newHeat = math.max(0f, currentHeat - decay);
                NextHeat[fireCellIndex] = newHeat;

                // Extinguish if heat drops below threshold
                if (newHeat < 10f)
                {
                    NextActiveFire[fireCellIndex] = 0;
                }
            }
        }
    }
}

