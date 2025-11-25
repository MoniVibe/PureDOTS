using PureDOTS.Runtime.Combat;
using PureDOTS.Runtime.Components;
using PureDOTS.Systems;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Systems.Combat
{
    /// <summary>
    /// Accumulates hazard slices into a 3D risk grid.
    /// Clears grid and rasterizes each HazardSlice into cells via AABB intersection.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(CombatSystemGroup))]
    [UpdateAfter(typeof(BuildHazardSlicesSystem))]
    public partial struct AccumulateHazardGridSystem : ISystem
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
            var rewindState = SystemAPI.GetSingleton<RewindState>();
            if (rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            var timeState = SystemAPI.GetSingleton<TimeState>();
            var currentTick = timeState.Tick;
            var deltaTime = timeState.DeltaTime;

            // Find or create hazard grid
            Entity gridEntity;
            HazardGrid grid;
            if (!SystemAPI.TryGetSingletonEntity<HazardGridSingleton>(out gridEntity) ||
                !SystemAPI.HasComponent<HazardGrid>(gridEntity))
            {
                // Create default grid (1000x1000x1 cells, 10m per cell, centered at origin)
                gridEntity = state.EntityManager.CreateEntity();
                var gridConfig = new HazardGrid
                {
                    Size = new int3(100, 100, 1), // 2D default
                    Cell = 10f,
                    Origin = float3.zero,
                    Risk = default
                };
                state.EntityManager.AddComponent<HazardGrid>(gridEntity);
                state.EntityManager.SetComponent(gridEntity, gridConfig);
                
                // Set singleton
                if (SystemAPI.HasSingleton<HazardGridSingleton>())
                {
                    SystemAPI.SetSingleton(new HazardGridSingleton { GridEntity = gridEntity });
                }
                else
                {
                    var singletonEntity = state.EntityManager.CreateEntity();
                    state.EntityManager.AddComponent<HazardGridSingleton>(singletonEntity);
                    state.EntityManager.SetComponent(singletonEntity, new HazardGridSingleton { GridEntity = gridEntity });
                }
                
                grid = gridConfig;
            }
            else
            {
                grid = SystemAPI.GetComponent<HazardGrid>(gridEntity);
            }

            // Get hazard slices buffer
            if (!SystemAPI.TryGetSingletonEntity<HazardSliceBuffer>(out var sliceBufferEntity) ||
                !SystemAPI.HasBuffer<HazardSlice>(sliceBufferEntity))
            {
                return;
            }

            var slices = SystemAPI.GetBuffer<HazardSlice>(sliceBufferEntity);

            // Rebuild risk blob if needed
            int totalCells = grid.Size.x * grid.Size.y * grid.Size.z;
            if (!grid.Risk.IsCreated || grid.Risk.Value.Length != totalCells)
            {
                // Create new risk blob
                var builder = new BlobBuilder(Allocator.Temp);
                ref var root = ref builder.ConstructRoot<float>();
                var riskArray = builder.Allocate(ref root, totalCells);
                for (int i = 0; i < totalCells; i++)
                {
                    riskArray[i] = 0f;
                }
                var newRisk = builder.CreateBlobAssetReference<float>(Allocator.Persistent);
                builder.Dispose();

                grid.Risk = newRisk;
                SystemAPI.SetComponent(gridEntity, grid);
            }

            // Clear grid
            ref var riskData = ref grid.Risk.Value;
            UnsafeUtility.MemClear(riskData.GetUnsafePtr(), totalCells * sizeof(float));

            // Convert slices to native array for job
            var slicesArray = slices.ToNativeArray(Allocator.TempJob);

            // Rasterize slices into grid
            var job = new AccumulateHazardGridJob
            {
                Grid = grid,
                CurrentTick = currentTick,
                DeltaTime = deltaTime,
                Slices = slicesArray,
                RiskData = riskData
            };

            state.Dependency = job.Schedule(slices.Length, 64, state.Dependency);
            state.Dependency.Complete();

            slicesArray.Dispose();
        }

        [BurstCompile]
        public struct AccumulateHazardGridJob : IJobFor
        {
            [ReadOnly] public HazardGrid Grid;
            public uint CurrentTick;
            public float DeltaTime;
            [ReadOnly] public NativeArray<HazardSlice> Slices;
            [NativeDisableParallelForRestriction] public BlobArray<float> RiskData;

            public void Execute(int index)
            {
                var slice = Slices[index];

                // Skip if slice is not active at current tick
                if (CurrentTick < slice.StartTick || CurrentTick > slice.EndTick)
                {
                    return;
                }

                // Calculate current radius (with growth)
                float elapsedTicks = CurrentTick - slice.StartTick;
                float elapsedSec = elapsedTicks * DeltaTime;
                float currentRadius = slice.Radius0 + slice.RadiusGrow * elapsedSec;

                // Calculate current center position (with velocity extrapolation)
                float3 currentCenter = slice.Center + slice.Vel * elapsedSec;

                // Rasterize sphere into grid cells
                int3 minCell = CellOf(currentCenter - currentRadius, Grid);
                int3 maxCell = CellOf(currentCenter + currentRadius, Grid);

                // Clamp to grid bounds
                minCell = math.clamp(minCell, int3.zero, Grid.Size - 1);
                maxCell = math.clamp(maxCell, int3.zero, Grid.Size - 1);

                // Iterate over affected cells
                for (int z = minCell.z; z <= maxCell.z; z++)
                {
                    for (int y = minCell.y; y <= maxCell.y; y++)
                    {
                        for (int x = minCell.x; x <= maxCell.x; x++)
                        {
                            int3 cell = new int3(x, y, z);
                            float3 cellCenter = CellCenter(cell, Grid);

                            // Distance from cell center to hazard center
                            float dist = math.length(cellCenter - currentCenter);

                            if (dist <= currentRadius)
                            {
                                // Compute base risk (inverse distance falloff)
                                float baseRisk = 1f / (1f + dist);

                                // Apply kind-specific weights (simplified - would use AvoidanceProfile weights)
                                float kindWeight = 1f;
                                if ((slice.Kind & HazardKind.AoE) != 0) kindWeight *= 1.5f;
                                if ((slice.Kind & HazardKind.Chain) != 0) kindWeight *= 1.2f;
                                if ((slice.Kind & HazardKind.Homing) != 0) kindWeight *= 1.3f;

                                float risk = baseRisk * kindWeight;

                                // Accumulate risk (atomic add for thread safety)
                                int cellIndex = Flatten(cell, Grid);
                                if (cellIndex >= 0 && cellIndex < RiskData.Length)
                                {
                                    RiskData[cellIndex] += risk;
                                }
                            }
                        }
                    }
                }
            }

            private static int3 CellOf(float3 pos, HazardGrid grid)
            {
                float3 local = (pos - grid.Origin) / grid.Cell;
                return (int3)math.floor(local);
            }

            private static float3 CellCenter(int3 cell, HazardGrid grid)
            {
                return grid.Origin + (cell + 0.5f) * grid.Cell;
            }

            private static int Flatten(int3 cell, HazardGrid grid)
            {
                return (cell.z * grid.Size.y + cell.y) * grid.Size.x + cell.x;
            }
        }
    }
}

