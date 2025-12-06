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
    /// Tracks changes to input fields (temperature, moisture, light, chemical) and marks affected biome chunks as dirty.
    /// Processes N dirty chunks per tick to amortize biome recalculation costs.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(EnvironmentSystemGroup), OrderFirst = true)]
    [UpdateBefore(typeof(BiomeDerivationSystem))]
    public partial struct BiomeChunkDirtyTrackingSystem : ISystem
    {
        private TimeAwareController _timeAware;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
            state.RequireForUpdate<BiomeGrid>();
            state.RequireForUpdate<BiomeChunkMetadata>();

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

            var biomeEntity = SystemAPI.GetSingletonEntity<BiomeGrid>();
            if (!SystemAPI.HasComponent<BiomeChunkMetadata>(biomeEntity))
            {
                return;
            }

            var chunkMetadata = SystemAPI.GetComponent<BiomeChunkMetadata>(biomeEntity);
            if (chunkMetadata.TotalChunkCount == 0)
            {
                return;
            }

            // Ensure hash buffer exists
            if (!SystemAPI.HasBuffer<BiomeChunkHash>(biomeEntity))
            {
                var hashBuffer = state.EntityManager.AddBuffer<BiomeChunkHash>(biomeEntity);
                hashBuffer.ResizeUninitialized(chunkMetadata.TotalChunkCount);
                for (int i = 0; i < hashBuffer.Length; i++)
                {
                    hashBuffer[i] = new BiomeChunkHash { Value = 0 };
                }
            }

            var hashBuffer = SystemAPI.GetBuffer<BiomeChunkHash>(biomeEntity);
            if (hashBuffer.Length != chunkMetadata.TotalChunkCount)
            {
                hashBuffer.ResizeUninitialized(chunkMetadata.TotalChunkCount);
            }

            // Use a NativeArray to track dirty chunks (bool per chunk)
            var dirtyFlags = new NativeArray<bool>(chunkMetadata.TotalChunkCount, Allocator.TempJob);

            // Compute hashes for each chunk based on input fields
            var job = new ComputeChunkHashesJob
            {
                ChunkMetadata = chunkMetadata,
                HashBuffer = hashBuffer.AsNativeArray(),
                DirtyFlags = dirtyFlags,
                CurrentTick = timeState.Tick
            };

            // Get input field data
            if (SystemAPI.TryGetSingleton<MoistureGrid>(out var moistureGrid))
            {
                job.MoistureBlob = moistureGrid.Blob;
                if (SystemAPI.TryGetSingletonEntity<MoistureGrid>(out var moistureEntity) &&
                    SystemAPI.HasBuffer<MoistureGridRuntimeCell>(moistureEntity))
                {
                    job.MoistureRuntime = SystemAPI.GetBuffer<MoistureGridRuntimeCell>(moistureEntity).AsNativeArray();
                    job.HasMoistureRuntime = true;
                }
            }

            if (SystemAPI.TryGetSingleton<TemperatureGrid>(out var temperatureGrid))
            {
                job.TemperatureBlob = temperatureGrid.Blob;
            }

            if (SystemAPI.TryGetSingleton<SunlightGrid>(out var sunlightGrid))
            {
                job.SunlightBlob = sunlightGrid.Blob;
            }

            if (SystemAPI.TryGetSingleton<ChemicalField>(out var chemicalField))
            {
                job.ChemicalBlob = chemicalField.Blob;
            }

            state.Dependency = job.Schedule(chunkMetadata.TotalChunkCount, 64, state.Dependency);
            state.Dependency.Complete();

            // Store dirty flags in a component for BiomeDerivationSystem to consume
            if (!SystemAPI.HasBuffer<BiomeChunkDirtyFlag>(biomeEntity))
            {
                var dirtyBuffer = state.EntityManager.AddBuffer<BiomeChunkDirtyFlag>(biomeEntity);
                dirtyBuffer.ResizeUninitialized(chunkMetadata.TotalChunkCount);
            }

            var dirtyBuffer = SystemAPI.GetBuffer<BiomeChunkDirtyFlag>(biomeEntity);
            if (dirtyBuffer.Length != dirtyFlags.Length)
            {
                dirtyBuffer.ResizeUninitialized(dirtyFlags.Length);
            }

            for (int i = 0; i < dirtyFlags.Length; i++)
            {
                dirtyBuffer[i] = new BiomeChunkDirtyFlag { Value = dirtyFlags[i] ? (byte)1 : (byte)0 };
            }

            dirtyFlags.Dispose();
        }

        [BurstCompile]
        private struct ComputeChunkHashesJob : IJobFor
        {
            public BiomeChunkMetadata ChunkMetadata;
            public NativeArray<BiomeChunkHash> HashBuffer;
            public NativeArray<bool> DirtyFlags;
            public uint CurrentTick;

            [ReadOnly] public BlobAssetReference<MoistureGridBlob> MoistureBlob;
            [ReadOnly] public NativeArray<MoistureGridRuntimeCell> MoistureRuntime;
            public bool HasMoistureRuntime;

            [ReadOnly] public BlobAssetReference<TemperatureGridBlob> TemperatureBlob;
            [ReadOnly] public BlobAssetReference<SunlightGridBlob> SunlightBlob;
            [ReadOnly] public BlobAssetReference<ChemicalFieldBlob> ChemicalBlob;

            public void Execute(int chunkIndex)
            {
                if (chunkIndex >= HashBuffer.Length)
                {
                    return;
                }

                ChunkMetadata.GetChunkCellRange(chunkIndex, out var minCell, out var maxCell);

                // Compute hash from input field values in this chunk
                uint hash = 0;
                var hashMultiplier = 31u;

                for (int y = minCell.y; y < maxCell.y; y++)
                {
                    for (int x = minCell.x; x < maxCell.x; x++)
                    {
                        var cellIndex = EnvironmentGridMath.GetCellIndex(ChunkMetadata.GridMetadata, new int2(x, y));

                        // Hash temperature
                        if (TemperatureBlob.IsCreated)
                        {
                            ref var temps = ref TemperatureBlob.Value.TemperatureCelsius;
                            if (cellIndex >= 0 && cellIndex < temps.Length)
                            {
                                var tempBits = math.asuint(temps[cellIndex]);
                                hash = hash * hashMultiplier + tempBits;
                            }
                        }

                        // Hash moisture
                        if (HasMoistureRuntime && MoistureRuntime.IsCreated && cellIndex >= 0 && cellIndex < MoistureRuntime.Length)
                        {
                            var moistBits = math.asuint(MoistureRuntime[cellIndex].Moisture);
                            hash = hash * hashMultiplier + moistBits;
                        }
                        else if (MoistureBlob.IsCreated)
                        {
                            ref var moistures = ref MoistureBlob.Value.Moisture;
                            if (cellIndex >= 0 && cellIndex < moistures.Length)
                            {
                                var moistBits = math.asuint(moistures[cellIndex]);
                                hash = hash * hashMultiplier + moistBits;
                            }
                        }

                        // Hash light (from sunlight)
                        if (SunlightBlob.IsCreated)
                        {
                            ref var samples = ref SunlightBlob.Value.Samples;
                            if (cellIndex >= 0 && cellIndex < samples.Length)
                            {
                                var lightValue = samples[cellIndex].DirectLight + samples[cellIndex].AmbientLight;
                                var lightBits = math.asuint(lightValue);
                                hash = hash * hashMultiplier + lightBits;
                            }
                        }

                        // Hash chemical
                        if (ChemicalBlob.IsCreated)
                        {
                            ref var chemicals = ref ChemicalBlob.Value.Samples;
                            if (cellIndex >= 0 && cellIndex < chemicals.Length)
                            {
                                var chemBits = math.asuint(chemicals[cellIndex].Oxygen + chemicals[cellIndex].CarbonDioxide);
                                hash = hash * hashMultiplier + chemBits;
                            }
                        }
                    }
                }

                // Compare with previous hash
                var previousHash = HashBuffer[chunkIndex].Value;
                if (hash != previousHash)
                {
                    HashBuffer[chunkIndex] = new BiomeChunkHash { Value = hash };
                    DirtyFlags[chunkIndex] = true;
                }
                else
                {
                    DirtyFlags[chunkIndex] = false;
                }
            }
        }
    }
}

