using PureDOTS.Runtime.Platform;
using PureDOTS.Runtime.Platform.Blobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Systems.Platform
{
    /// <summary>
    /// Determines platform death conditions and handles derelict/husk state.
    /// Platform dies if: no core segments OR core segments lack vital modules.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PlatformConnectivitySystem))]
    [UpdateAfter(typeof(PlatformReactorCriticalSystem))]
    public partial struct PlatformDeathSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<HullDefRegistry>();
            state.RequireForUpdate<ModuleDefRegistry>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var hullRegistry = SystemAPI.GetSingleton<HullDefRegistry>();
            var moduleRegistry = SystemAPI.GetSingleton<ModuleDefRegistry>();

            if (!hullRegistry.Registry.IsCreated || !moduleRegistry.Registry.IsCreated)
            {
                return;
            }

            ref var hullRegistryBlob = ref hullRegistry.Registry.Value;
            ref var moduleRegistryBlob = ref moduleRegistry.Registry.Value;

            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (hullRef, segmentStates, moduleSlots, explosionEvents, entity) in SystemAPI.Query<
                RefRO<PlatformHullRef>,
                DynamicBuffer<PlatformSegmentState>,
                DynamicBuffer<PlatformModuleSlot>,
                DynamicBuffer<PlatformExplosionEvent>>().WithEntityAccess())
            {
                if (SystemAPI.HasComponent<DerelictTag>(entity))
                {
                    continue;
                }

                var hullId = hullRef.ValueRO.HullId;
                if (hullId < 0 || hullId >= hullRegistryBlob.Hulls.Length)
                {
                    continue;
                }

                ref var hullDef = ref hullRegistryBlob.Hulls[hullId];

                bool hasMeltdown = explosionEvents.Length > 0;
                bool isDead = CheckDeathCondition(
                    in hullDef,
                    ref segmentStates,
                    in moduleSlots,
                    ref moduleRegistryBlob,
                    ref hullRegistryBlob);

                if (isDead)
                {
                    if (hasMeltdown)
                    {
                        DestroyPlatform(ref ecb, entity);
                    }
                    else
                    {
                        MakeDerelict(ref ecb, entity);
                    }
                }
            }
        }

        [BurstCompile]
        private static bool CheckDeathCondition(
            in HullDef hullDef,
            ref DynamicBuffer<PlatformSegmentState> segmentStates,
            in DynamicBuffer<PlatformModuleSlot> moduleSlots,
            ref ModuleDefRegistryBlob moduleRegistry,
            ref HullDefRegistryBlob hullRegistry)
        {
            if (hullDef.SegmentCount == 0)
            {
                return false;
            }

            var coreSegments = new NativeList<int>(Allocator.Temp);

            for (int i = 0; i < segmentStates.Length; i++)
            {
                var segmentState = segmentStates[i];
                var segmentIndex = segmentState.SegmentIndex;

                if ((segmentState.Status & SegmentStatusFlags.Destroyed) != 0)
                {
                    continue;
                }

                if ((segmentState.Status & SegmentStatusFlags.Detached) != 0)
                {
                    continue;
                }

                var segmentOffset = hullDef.SegmentOffset + segmentIndex;
                if (segmentOffset < 0 || segmentOffset >= hullRegistry.Segments.Length)
                {
                    continue;
                }

                ref var segmentDef = ref hullRegistry.Segments[segmentOffset];
                if (segmentDef.IsCore != 0)
                {
                    coreSegments.Add(segmentIndex);
                }
            }

            if (coreSegments.Length == 0)
            {
                return true;
            }

            for (int i = 0; i < coreSegments.Length; i++)
            {
                var coreSegmentIndex = coreSegments[i];
                if (!HasVitalModules(coreSegmentIndex, in moduleSlots, ref moduleRegistry))
                {
                    return true;
                }
            }

            return false;
        }

        [BurstCompile]
        private static bool HasVitalModules(
            int segmentIndex,
            in DynamicBuffer<PlatformModuleSlot> moduleSlots,
            ref ModuleDefRegistryBlob moduleRegistry)
        {
            bool hasBridge = false;
            bool hasReactor = false;
            bool hasLifeSupport = false;

            for (int i = 0; i < moduleSlots.Length; i++)
            {
                var slot = moduleSlots[i];
                if (slot.SegmentIndex != segmentIndex)
                {
                    continue;
                }

                if (slot.State == ModuleSlotState.Destroyed)
                {
                    continue;
                }

                if (slot.ModuleId < 0 || slot.ModuleId >= moduleRegistry.Modules.Length)
                {
                    continue;
                }

                ref var moduleDef = ref moduleRegistry.Modules[slot.ModuleId];

                if (moduleDef.Category == ModuleCategory.Utility)
                {
                    hasReactor = true;
                }

                if (moduleDef.Category == ModuleCategory.Facility)
                {
                    hasLifeSupport = true;
                }

                if (moduleDef.Category == ModuleCategory.Utility)
                {
                    hasBridge = true;
                }
            }

            return hasBridge && hasReactor && hasLifeSupport;
        }

        [BurstCompile]
        private static void DestroyPlatform(ref EntityCommandBuffer ecb, Entity entity)
        {
            ecb.DestroyEntity(entity);
        }

        [BurstCompile]
        private static void MakeDerelict(ref EntityCommandBuffer ecb, Entity entity)
        {
            if (!ecb.HasComponent<DerelictTag>(entity))
            {
                ecb.AddComponent<DerelictTag>(entity);
            }

            if (!ecb.HasComponent<SalvageableState>(entity))
            {
                ecb.AddComponent(entity, new SalvageableState
                {
                    Infested = 0,
                    HasLootCache = 1,
                    StructuralIntegrity = 0.5f
                });
            }
        }
    }
}

