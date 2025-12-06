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
    /// Applies terraforming events as deltas to environment fields.
    /// Uses Gaussian/impulse/linear distribution kernels based on event shape.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(EnvironmentSystemGroup))]
    [UpdateAfter(typeof(BiomeChunkDirtyTrackingSystem))]
    [UpdateBefore(typeof(FieldPropagationSystem))]
    public partial struct TerraformingDeltaSystem : ISystem
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

            // Process terraforming events from all entities that have them
            var eventQuery = SystemAPI.QueryBuilder()
                .WithAll<TerraformingEvent>()
                .Build();

            if (eventQuery.IsEmpty)
            {
                return;
            }

            // Get target grids
            var hasMoistureGrid = SystemAPI.TryGetSingleton<MoistureGrid>(out var moistureGrid);
            var hasTemperatureGrid = SystemAPI.TryGetSingleton<TemperatureGrid>(out var temperatureGrid);
            var hasSunlightGrid = SystemAPI.TryGetSingleton<SunlightGrid>(out var sunlightGrid);
            var hasChemicalField = SystemAPI.TryGetSingleton<ChemicalField>(out var chemicalField);

            foreach (var (events, entity) in SystemAPI.Query<DynamicBuffer<TerraformingEvent>>().WithEntityAccess())
            {
                if (events.Length == 0)
                {
                    continue;
                }

                // Process each event
                for (int i = 0; i < events.Length; i++)
                {
                    var evt = events[i];
                    ApplyTerraformingEvent(ref state, evt, hasMoistureGrid ? moistureGrid : default,
                        hasTemperatureGrid ? temperatureGrid : default,
                        hasSunlightGrid ? sunlightGrid : default,
                        hasChemicalField ? chemicalField : default);
                }

                // Clear processed events
                events.Clear();
            }
        }

        private void ApplyTerraformingEvent(ref SystemState state, TerraformingEvent evt,
            MoistureGrid moistureGrid, TemperatureGrid temperatureGrid,
            SunlightGrid sunlightGrid, ChemicalField chemicalField)
        {
            switch (evt.FieldType)
            {
                case TerraformingFieldType.Temperature:
                    if (temperatureGrid.IsCreated)
                    {
                        ApplyDeltaToTemperatureGrid(ref state, evt, temperatureGrid);
                    }
                    break;

                case TerraformingFieldType.Moisture:
                    if (moistureGrid.IsCreated)
                    {
                        ApplyDeltaToMoistureGrid(ref state, evt, moistureGrid);
                    }
                    break;

                case TerraformingFieldType.Light:
                    if (sunlightGrid.IsCreated)
                    {
                        ApplyDeltaToSunlightGrid(ref state, evt, sunlightGrid);
                    }
                    break;

                case TerraformingFieldType.Chemical:
                    if (chemicalField.IsCreated)
                    {
                        ApplyDeltaToChemicalField(ref state, evt, chemicalField);
                    }
                    break;
            }
        }

        private void ApplyDeltaToTemperatureGrid(ref SystemState state, TerraformingEvent evt, TemperatureGrid grid)
        {
            if (!grid.IsCreated)
            {
                return;
            }

            ref var tempBlob = ref grid.Blob.Value;
            var metadata = grid.Metadata;

            // Find affected cells
            var centerCell = WorldToCell(metadata, evt.Position);
            var radiusCells = (int)math.ceil(evt.Radius / metadata.CellSize);

            var job = new ApplyTerraformingDeltaJob
            {
                CenterCell = centerCell,
                RadiusCells = radiusCells,
                Position = evt.Position,
                Radius = evt.Radius,
                Intensity = evt.Intensity,
                Shape = evt.Shape,
                Metadata = metadata,
                TemperatureArray = tempBlob.TemperatureCelsius
            };

            state.Dependency = job.ScheduleParallel(tempBlob.TemperatureCelsius.Length, 64, state.Dependency);
        }

        private void ApplyDeltaToMoistureGrid(ref SystemState state, TerraformingEvent evt, MoistureGrid grid)
        {
            var gridEntity = SystemAPI.GetSingletonEntity<MoistureGrid>();
            if (!SystemAPI.HasBuffer<MoistureGridRuntimeCell>(gridEntity))
            {
                return;
            }

            var runtimeBuffer = SystemAPI.GetBuffer<MoistureGridRuntimeCell>(gridEntity);
            var metadata = grid.Metadata;

            var centerCell = WorldToCell(metadata, evt.Position);
            var radiusCells = (int)math.ceil(evt.Radius / metadata.CellSize);

            var job = new ApplyMoistureDeltaJob
            {
                CenterCell = centerCell,
                RadiusCells = radiusCells,
                Position = evt.Position,
                Radius = evt.Radius,
                Intensity = evt.Intensity,
                Shape = evt.Shape,
                Metadata = metadata,
                MoistureCells = runtimeBuffer.AsNativeArray()
            };

            state.Dependency = job.ScheduleParallel(runtimeBuffer.Length, 64, state.Dependency);
        }

        private void ApplyDeltaToSunlightGrid(ref SystemState state, TerraformingEvent evt, SunlightGrid grid)
        {
            // Sunlight grid modifications are less common - defer implementation if needed
        }

        private void ApplyDeltaToChemicalField(ref SystemState state, TerraformingEvent evt, ChemicalField field)
        {
            // Chemical field modifications - defer implementation if needed
        }

        private static int2 WorldToCell(EnvironmentGridMetadata metadata, float3 worldPos)
        {
            var local = new float2(
                (worldPos.x - metadata.WorldMin.x) * metadata.InverseCellSize,
                (worldPos.z - metadata.WorldMin.z) * metadata.InverseCellSize);
            return (int2)math.floor(local);
        }

        [BurstCompile]
        private struct ApplyTerraformingDeltaJob : IJobFor
        {
            public int2 CenterCell;
            public int RadiusCells;
            public float3 Position;
            public float Radius;
            public float Intensity;
            public TerraformingShape Shape;
            public EnvironmentGridMetadata Metadata;
            public BlobArray<float> TemperatureArray;

            public void Execute(int index)
            {
                var cellCoord = EnvironmentGridMath.GetCellCoordinates(Metadata, index);
                var cellCenter = EnvironmentGridMath.GetCellCenter(Metadata, index);
                var cellCenterXZ = new float2(cellCenter.x, cellCenter.z);
                var positionXZ = new float2(Position.x, Position.z);

                var distance = math.distance(cellCenterXZ, positionXZ);
                if (distance > Radius)
                {
                    return;
                }

                var weight = ComputeWeight(distance, Radius, Shape);
                var delta = Intensity * weight;

                TemperatureArray[index] += delta;
            }

            private static float ComputeWeight(float distance, float radius, TerraformingShape shape)
            {
                if (distance >= radius)
                {
                    return 0f;
                }

                var normalizedDist = distance / radius;

                return shape switch
                {
                    TerraformingShape.Gaussian => math.exp(-normalizedDist * normalizedDist * 4f), // 4 = sharpness
                    TerraformingShape.Impulse => normalizedDist < 0.1f ? 1f : 0f, // Sharp cutoff
                    TerraformingShape.Linear => 1f - normalizedDist,
                    _ => 0f
                };
            }
        }

        [BurstCompile]
        private struct ApplyMoistureDeltaJob : IJobFor
        {
            public int2 CenterCell;
            public int RadiusCells;
            public float3 Position;
            public float Radius;
            public float Intensity;
            public TerraformingShape Shape;
            public EnvironmentGridMetadata Metadata;
            public NativeArray<MoistureGridRuntimeCell> MoistureCells;

            public void Execute(int index)
            {
                var cellCoord = EnvironmentGridMath.GetCellCoordinates(Metadata, index);
                var cellCenter = EnvironmentGridMath.GetCellCenter(Metadata, index);
                var cellCenterXZ = new float2(cellCenter.x, cellCenter.z);
                var positionXZ = new float2(Position.x, Position.z);

                var distance = math.distance(cellCenterXZ, positionXZ);
                if (distance > Radius)
                {
                    return;
                }

                var weight = ComputeWeight(distance, Radius, Shape);
                var delta = Intensity * weight;

                var cell = MoistureCells[index];
                cell.Moisture = math.clamp(cell.Moisture + delta, 0f, 100f);
                MoistureCells[index] = cell;
            }

            private static float ComputeWeight(float distance, float radius, TerraformingShape shape)
            {
                if (distance >= radius)
                {
                    return 0f;
                }

                var normalizedDist = distance / radius;

                return shape switch
                {
                    TerraformingShape.Gaussian => math.exp(-normalizedDist * normalizedDist * 4f),
                    TerraformingShape.Impulse => normalizedDist < 0.1f ? 1f : 0f,
                    TerraformingShape.Linear => 1f - normalizedDist,
                    _ => 0f
                };
            }
        }
    }
}

