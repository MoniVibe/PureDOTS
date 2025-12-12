using PureDOTS.Runtime.Ships;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Systems.Ships
{
    /// <summary>
    /// Ensures carriers that expose module slots also carry the supporting buffers/settings.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    // Removed invalid UpdateAfter: CoreSingletonBootstrapSystem executes in TimeSystemGroup; ordering is handled at group composition.
    public partial struct CarrierModuleBootstrapSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CarrierModuleSlot>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (_, entity) in SystemAPI.Query<DynamicBuffer<CarrierModuleSlot>>().WithNone<CarrierModuleAggregate>().WithEntityAccess())
            {
                ecb.AddComponent(entity, new CarrierModuleAggregate { EfficiencyScalar = 1f });
            }

            foreach (var (_, entity) in SystemAPI.Query<DynamicBuffer<CarrierModuleSlot>>().WithNone<ModuleRepairSettings>().WithEntityAccess())
            {
                ecb.AddComponent(entity, ModuleRepairSettings.CreateDefaults());
            }

            foreach (var (_, entity) in SystemAPI.Query<DynamicBuffer<CarrierModuleSlot>>().WithNone<CarrierRefitSettings>().WithEntityAccess())
            {
                ecb.AddComponent(entity, CarrierRefitSettings.CreateDefaults());
            }

            foreach (var (_, entity) in SystemAPI.Query<DynamicBuffer<CarrierModuleSlot>>().WithNone<CarrierRefitState>().WithEntityAccess())
            {
                ecb.AddComponent(entity, new CarrierRefitState { InRefitFacility = 0, SpeedMultiplier = 1f });
            }

            foreach (var (_, entity) in SystemAPI.Query<DynamicBuffer<CarrierModuleSlot>>().WithNone<ModuleRepairTicket>().WithEntityAccess())
            {
                ecb.AddBuffer<ModuleRepairTicket>(entity);
            }

            foreach (var (_, entity) in SystemAPI.Query<DynamicBuffer<CarrierModuleSlot>>().WithNone<ModuleRefitRequest>().WithEntityAccess())
            {
                ecb.AddBuffer<ModuleRefitRequest>(entity);
            }

            foreach (var (slots, entity) in SystemAPI.Query<DynamicBuffer<CarrierModuleSlot>>().WithEntityAccess())
            {
                for (int i = 0; i < slots.Length; i++)
                {
                    var module = slots[i].InstalledModule;
                    if (module == Entity.Null || !state.EntityManager.HasComponent<ShipModule>(module))
                    {
                        continue;
                    }

                    if (!state.EntityManager.HasComponent<CarrierOwner>(module))
                    {
                        ecb.AddComponent(module, new CarrierOwner { Carrier = entity });
                    }

                    if (!state.EntityManager.HasComponent<ModuleHealth>(module))
                    {
                        ecb.AddComponent(module, new ModuleHealth
                        {
                            MaxHealth = 100f,
                            Health = 100f,
                            DegradationPerTick = 0f,
                            FailureThreshold = 25f,
                            State = ModuleHealthState.Nominal,
                            Flags = ModuleHealthFlags.None,
                            LastProcessedTick = 0
                        });
                    }

                    if (!state.EntityManager.HasComponent<ModuleOperationalState>(module))
                    {
                        ecb.AddComponent(module, new ModuleOperationalState
                        {
                            IsOnline = 1,
                            InCombat = 0,
                            LoadFactor = 0f
                        });
                    }
                }
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}
