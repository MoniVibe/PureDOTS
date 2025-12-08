using PureDOTS.Runtime.Components;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Physics
{
    /// <summary>
    /// Calculates structural integrity using Burst vectorized math.
    /// Computes Stress = Force / Area, Strain = Stress / YoungsModulus.
    /// Reduces Integrity when Stress > YieldThreshold.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(InertiaCalculationSystem))]
    public partial struct StructuralIntegritySystem : ISystem
    {
        private EntityQuery _structuralQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TickTimeState>();
            state.RequireForUpdate<RewindState>();

            _structuralQuery = state.GetEntityQuery(
                ComponentType.ReadWrite<StructuralState>(),
                ComponentType.ReadOnly<MassComponent>(),
                ComponentType.ReadOnly<MaterialId>()
            );
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var rewindState = SystemAPI.GetSingleton<RewindState>();
            if (rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            if (!SystemAPI.TryGetSingleton<MaterialCatalog>(out var materialCatalog))
            {
                return;
            }

            ref var catalogBlob = ref materialCatalog.Catalog.Value;
            var tickTimeState = SystemAPI.GetSingleton<TickTimeState>();
            var deltaTime = tickTimeState.FixedDeltaTime;

            var materialLookup = state.GetComponentLookup<MaterialId>(true);
            var massLookup = state.GetComponentLookup<MassComponent>(true);

            var integrityJob = new CalculateIntegrityJob
            {
                MaterialCatalog = catalogBlob,
                MaterialLookup = materialLookup,
                MassLookup = massLookup,
                DeltaTime = deltaTime,
                CurrentTick = tickTimeState.Tick,
                StructuralHandle = state.GetComponentTypeHandle<StructuralState>(false),
                MaterialHandle = state.GetComponentTypeHandle<MaterialId>(true),
                EntityHandle = state.GetEntityTypeHandle()
            };

            state.Dependency = integrityJob.ScheduleParallel(_structuralQuery, state.Dependency);
        }

        [BurstCompile]
        private struct CalculateIntegrityJob : IJobChunk
        {
            [ReadOnly]
            public MaterialCatalogBlob MaterialCatalog;

            [ReadOnly]
            public ComponentLookup<MaterialId> MaterialLookup;

            [ReadOnly]
            public ComponentLookup<MassComponent> MassLookup;

            public float DeltaTime;
            public uint CurrentTick;

            public ComponentTypeHandle<StructuralState> StructuralHandle;
            [ReadOnly]
            public ComponentTypeHandle<MaterialId> MaterialHandle;
            [ReadOnly]
            public EntityTypeHandle EntityHandle;

            void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                ExecuteChunk(chunk, unfilteredChunkIndex, useEnabledMask, chunkEnabledMask);
            }

            private void ExecuteChunk(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var structuralStates = chunk.GetNativeArray(StructuralHandle);
                var materialIds = chunk.GetNativeArray(MaterialHandle);
                var entities = chunk.GetNativeArray(EntityHandle);

                for (int i = 0; i < chunk.Count; i++)
                {
                    var entity = entities[i];
                    var structural = structuralStates[i];
                    var materialId = materialIds[i];

                    // Look up material properties
                    MaterialSpec material = default;
                    bool foundMaterial = false;
                    for (int j = 0; j < MaterialCatalog.Materials.Length; j++)
                    {
                        if (MaterialCatalog.Materials[j].MaterialId.Equals(materialId.Value))
                        {
                            material = MaterialCatalog.Materials[j];
                            foundMaterial = true;
                            break;
                        }
                    }

                    if (!foundMaterial || structural.CrossSectionalArea <= 0f)
                    {
                        structuralStates[i] = structural;
                        continue;
                    }

                    // Calculate force from mass and acceleration (simplified: assume gravity + inertia)
                    var mass = MassLookup.HasComponent(entity) ? MassLookup[entity].Mass : 0f;
                    var force = mass * 9.81f; // Gravity force (can be enhanced with actual acceleration)

                    // Calculate stress: Stress = Force / Area
                    var stress = force / structural.CrossSectionalArea;
                    structural.Stress = stress;

                    // Calculate strain: Strain = Stress / YoungsModulus
                    if (material.YoungsModulus > 0f)
                    {
                        structural.Strain = stress / material.YoungsModulus;
                    }
                    else
                    {
                        structural.Strain = 0f;
                    }

                    // Update yield threshold from material
                    structural.YieldThreshold = material.YieldStrength;

                    // Reduce integrity if stress exceeds yield threshold
                    if (stress > material.YieldStrength && structural.Integrity > 0f)
                    {
                        // Damage rate proportional to excess stress
                        var excessStress = stress - material.YieldStrength;
                        var damageRate = excessStress / material.YieldStrength * DeltaTime;
                        structural.Integrity = math.max(0f, structural.Integrity - damageRate);
                    }

                    structural.LastUpdateTick = CurrentTick;
                    structuralStates[i] = structural;
                }
            }
        }
    }
}

