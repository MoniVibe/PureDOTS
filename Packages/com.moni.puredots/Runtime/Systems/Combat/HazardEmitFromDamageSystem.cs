using PureDOTS.Runtime.Combat;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Ships;
using PureDOTS.Systems;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Systems.Combat
{
    /// <summary>
    /// Emits HazardSlice entries from ongoing fires/radiation leaks.
    /// Integrates with BuildHazardSlicesSystem.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(CombatSystemGroup))]
    [UpdateAfter(typeof(ModuleCriticalEffectsSystem))]
    [UpdateBefore(typeof(BuildHazardSlicesSystem))]
    public partial struct HazardEmitFromDamageSystem : ISystem
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

            // Find or create hazard slice buffer singleton
            Entity sliceBufferEntity;
            if (!SystemAPI.TryGetSingletonEntity<HazardSliceBuffer>(out sliceBufferEntity))
            {
                sliceBufferEntity = state.EntityManager.CreateEntity();
                state.EntityManager.AddComponent<HazardSliceBuffer>(sliceBufferEntity);
                state.EntityManager.AddBuffer<HazardSlice>(sliceBufferEntity);
            }

            var sliceBuffer = SystemAPI.GetBuffer<HazardSlice>(sliceBufferEntity);

            var transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
            transformLookup.Update(ref state);

            var job = new HazardEmitFromDamageJob
            {
                CurrentTick = currentTick,
                DeltaTime = deltaTime,
                SliceBuffer = sliceBuffer,
                TransformLookup = transformLookup
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
            state.Dependency.Complete(); // Need to complete before BuildHazardSlicesSystem reads buffer
        }

        [BurstCompile]
        public partial struct HazardEmitFromDamageJob : IJobEntity
        {
            public uint CurrentTick;
            public float DeltaTime;
            [NativeDisableParallelForRestriction] public DynamicBuffer<HazardSlice> SliceBuffer;
            [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;

            void Execute(
                Entity entity,
                DynamicBuffer<ModuleRuntimeState> modules,
                in ShipLayoutRef layoutRef,
                in LocalTransform transform)
            {
                if (!layoutRef.Blob.IsCreated || !TransformLookup.HasComponent(entity))
                {
                    return;
                }

                ref var layout = ref layoutRef.Blob.Value;
                float3 shipPos = transform.Position;

                // Check for reactor destruction (radiation hazard)
                for (int i = 0; i < layout.Modules.Length && i < modules.Length; i++)
                {
                    ref var moduleSlot = ref layout.Modules[i];
                    ref var module = ref modules.ElementAt(i);

                    if (moduleSlot.Id.ToString().Contains("reactor") && module.Destroyed != 0)
                    {
                        // Emit radiation hazard
                        var radiationSlice = new HazardSlice
                        {
                            Center = shipPos,
                            Vel = float3.zero, // Stationary radiation leak
                            Radius0 = 50f, // Initial radius
                            RadiusGrow = 10f, // Growing radiation cloud
                            StartTick = CurrentTick,
                            EndTick = CurrentTick + 1000, // Long duration
                            Kind = HazardKind.Plague, // Use Plague for radiation contamination
                            ChainRadius = 0f,
                            ContagionProb = 0.1f, // Radiation spread chance
                            HomingConeCos = 0f,
                            SprayVariance = 0f,
                            TeamMask = 0xFFFFFFFF, // Affects all teams
                            Seed = (uint)entity.Index
                        };

                        SliceBuffer.Add(radiationSlice);
                    }

                    // Check for fire (simplified - would track fire state)
                    if (moduleSlot.Id.ToString().Contains("engine") && module.Destroyed != 0 && module.HP < module.MaxHP * 0.5f)
                    {
                        // Emit fire hazard
                        var fireSlice = new HazardSlice
                        {
                            Center = shipPos,
                            Vel = float3.zero,
                            Radius0 = 20f,
                            RadiusGrow = 5f,
                            StartTick = CurrentTick,
                            EndTick = CurrentTick + 100, // Shorter duration
                            Kind = HazardKind.AoE,
                            ChainRadius = 0f,
                            ContagionProb = 0f,
                            HomingConeCos = 0f,
                            SprayVariance = 0f,
                            TeamMask = 0xFFFFFFFF,
                            Seed = (uint)entity.Index + 1000
                        };

                        SliceBuffer.Add(fireSlice);
                    }
                }
            }
        }
    }
}

