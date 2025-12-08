using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Physics;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Combat
{
    /// <summary>
    /// Physics-driven damage calculation system using material properties.
    /// Calculates: Damage = (KineticEnergy / TargetYieldStrength) * MaterialFlexibility
    /// Uses material penetration modifiers from weapon specs.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(StructuralIntegritySystem))]
    public partial struct WeaponDamageSystem : ISystem
    {
        private EntityQuery _damageQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TickTimeState>();
            state.RequireForUpdate<RewindState>();

            _damageQuery = state.GetEntityQuery(
                ComponentType.ReadWrite<StructuralState>(),
                ComponentType.ReadOnly<MaterialId>(),
                ComponentType.ReadOnly<MassComponent>()
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

            var materialLookup = state.GetComponentLookup<MaterialId>(true);
            var massLookup = state.GetComponentLookup<MassComponent>(true);

            var damageJob = new CalculateWeaponDamageJob
            {
                MaterialCatalog = catalogBlob,
                MaterialLookup = materialLookup,
                MassLookup = massLookup,
                StructuralHandle = state.GetComponentTypeHandle<StructuralState>(false),
                MaterialHandle = state.GetComponentTypeHandle<MaterialId>(true)
            };

            state.Dependency = damageJob.ScheduleParallel(_damageQuery, state.Dependency);
        }

        [BurstCompile]
        private struct CalculateWeaponDamageJob : IJobChunk
        {
            [ReadOnly]
            public MaterialCatalogBlob MaterialCatalog;

            [ReadOnly]
            public ComponentLookup<MaterialId> MaterialLookup;

            [ReadOnly]
            public ComponentLookup<MassComponent> MassLookup;

            public ComponentTypeHandle<StructuralState> StructuralHandle;

            [ReadOnly]
            public ComponentTypeHandle<MaterialId> MaterialHandle;

            void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                ExecuteChunk(chunk, unfilteredChunkIndex, useEnabledMask, chunkEnabledMask);
            }

            private void ExecuteChunk(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var structuralStates = chunk.GetNativeArray(ref StructuralHandle);
                var materialIds = chunk.GetNativeArray(ref MaterialHandle);

                for (int i = 0; i < chunk.Count; i++)
                {
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

                    if (!foundMaterial)
                    {
                        structuralStates[i] = structural;
                        continue;
                    }

                    // Calculate kinetic energy from mass and velocity (if available)
                    // For now, use stress as proxy for impact energy
                    var kineticEnergy = structural.Stress * structural.CrossSectionalArea;

                    // Calculate damage: Damage = (KineticEnergy / TargetYieldStrength) * MaterialFlexibility
                    var damage = 0f;
                    if (material.YieldStrength > 0f)
                    {
                        damage = (kineticEnergy / material.YieldStrength) * material.Flexibility;
                    }

                    // Apply damage to structural integrity
                    if (damage > 0f && structural.Integrity > 0f)
                    {
                        structural.Integrity = math.max(0f, structural.Integrity - damage);
                    }

                    structuralStates[i] = structural;
                }
            }
        }

        /// <summary>
        /// Calculates weapon damage using material properties and penetration modifiers.
        /// </summary>
        [BurstCompile]
        public static float CalculateDamage(
            float kineticEnergy,
            in MaterialSpec targetMaterial,
            ref DamageModel damageModel,
            MaterialCategory targetCategory)
        {
            // Base damage calculation: Damage = (KineticEnergy / TargetYieldStrength) * MaterialFlexibility
            var baseDamage = 0f;
            if (targetMaterial.YieldStrength > 0f)
            {
                baseDamage = (kineticEnergy / targetMaterial.YieldStrength) * targetMaterial.Flexibility;
            }

            // Apply material penetration modifier
            var penetrationModifier = 1.0f;
            if (damageModel.MaterialPenetrationModifiers.Length > (int)targetCategory)
            {
                penetrationModifier = damageModel.MaterialPenetrationModifiers[(int)targetCategory];
            }

            return baseDamage * penetrationModifier;
        }
    }
}

