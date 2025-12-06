using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Math;
using PureDOTS.Runtime.Physics;
using Unity.Burst;
using Unity.Entities;

namespace PureDOTS.Systems.Physics
{
    /// <summary>
    /// Bridges damage between collision regimes when thresholds are crossed.
    /// Converts between StructuralIntegrity, CraterState, and ThermoState components.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    [UpdateAfter(typeof(MicroCollisionSystemGroup))]
    [UpdateAfter(typeof(MesoCollisionSystemGroup))]
    [UpdateAfter(typeof(MacroCollisionSystemGroup))]
    public partial struct CollisionDamageBridgeSystem : ISystem
    {
        private ComponentLookup<CollisionProperties> _collisionPropsLookup;
        private ComponentLookup<StructuralIntegrity> _integrityLookup;
        private ComponentLookup<CraterState> _craterStateLookup;
        private ComponentLookup<ThermoState> _thermoStateLookup;
        private ComponentLookup<MacroThermoState> _macroThermoStateLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();

            _collisionPropsLookup = state.GetComponentLookup<CollisionProperties>(false);
            _integrityLookup = state.GetComponentLookup<StructuralIntegrity>(false);
            _craterStateLookup = state.GetComponentLookup<CraterState>(false);
            _thermoStateLookup = state.GetComponentLookup<ThermoState>(false);
            _macroThermoStateLookup = state.GetComponentLookup<MacroThermoState>(false);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var rewindState = SystemAPI.GetSingleton<RewindState>();
            if (rewindState.Mode != RewindMode.Record)
                return;

            _collisionPropsLookup.Update(ref state);
            _integrityLookup.Update(ref state);
            _craterStateLookup.Update(ref state);
            _thermoStateLookup.Update(ref state);
            _macroThermoStateLookup.Update(ref state);

            var job = new BridgeDamageJob
            {
                CollisionPropsLookup = _collisionPropsLookup,
                IntegrityLookup = _integrityLookup,
                CraterStateLookup = _craterStateLookup,
                ThermoStateLookup = _thermoStateLookup,
                MacroThermoStateLookup = _macroThermoStateLookup,
                Ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter()
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(CollisionProperties))]
        [WithChangeFilter(typeof(CollisionProperties))]
        partial struct BridgeDamageJob : IJobEntity
        {
            public ComponentLookup<CollisionProperties> CollisionPropsLookup;
            public ComponentLookup<StructuralIntegrity> IntegrityLookup;
            public ComponentLookup<CraterState> CraterStateLookup;
            public ComponentLookup<ThermoState> ThermoStateLookup;
            public ComponentLookup<MacroThermoState> MacroThermoStateLookup;
            public EntityCommandBuffer.ParallelWriter Ecb;

            public void Execute(Entity entity, [EntityIndexInQuery] int entityInQueryIndex, ref CollisionProperties props)
            {
                var oldRegime = props.Regime;
                var newRegime = DetermineRegime(props.Radius);

                // If regime changed, convert damage components
                if (oldRegime != newRegime)
                {
                    ConvertDamageComponents(entity, entityInQueryIndex, oldRegime, newRegime);
                    props.Regime = newRegime;
                }
            }

            [BurstCompile]
            private CollisionRegime DetermineRegime(float radius)
            {
                if (radius < CollisionMath.REGIME_MICRO_MAX)
                    return CollisionRegime.Micro;
                
                if (radius < CollisionMath.REGIME_MACRO_MIN)
                    return CollisionRegime.Meso;
                
                return CollisionRegime.Macro;
            }

            [BurstCompile]
            private void ConvertDamageComponents(Entity entity, int entityInQueryIndex, CollisionRegime oldRegime, CollisionRegime newRegime)
            {
                // Micro -> Meso: Convert StructuralIntegrity to CraterState
                if (oldRegime == CollisionRegime.Micro && newRegime == CollisionRegime.Meso)
                {
                    if (IntegrityLookup.HasComponent(entity))
                    {
                        var integrity = IntegrityLookup[entity];
                        var damage = 1f - integrity.Value;

                        // Create initial crater state based on damage
                        if (!CraterStateLookup.HasComponent(entity))
                        {
                            Ecb.AddComponent(entityInQueryIndex, entity, new CraterState
                            {
                                Radius = damage * 10f, // Scale damage to radius
                                EjectaMass = damage * 1000f,
                                ImpactPosition = float3.zero,
                                FormationTick = 0
                            });
                        }

                        // Remove integrity component
                        Ecb.RemoveComponent<StructuralIntegrity>(entityInQueryIndex, entity);
                    }
                }

                // Meso -> Macro: Convert CraterState to ThermoState
                if (oldRegime == CollisionRegime.Meso && newRegime == CollisionRegime.Macro)
                {
                    if (CraterStateLookup.HasComponent(entity))
                    {
                        var crater = CraterStateLookup[entity];
                        var energyFromCrater = crater.EjectaMass * 1000f; // Rough energy estimate

                        // Create thermo state from crater
                        if (!ThermoStateLookup.HasComponent(entity))
                        {
                            Ecb.AddComponent(entityInQueryIndex, entity, new ThermoState
                            {
                                Temperature = 300f + energyFromCrater * 0.001f, // Base temp + energy
                                MeltPercentage = math.min(1f, crater.Radius / 1000f), // Scale crater to melt %
                                AtmosphereMass = 1e15f * (1f - crater.Radius / 10000f), // Reduce atmosphere with crater size
                                BaseTemperature = 300f
                            });
                        }

                        // Remove crater state
                        Ecb.RemoveComponent<CraterState>(entityInQueryIndex, entity);
                    }
                }

                // Micro -> Macro: Convert StructuralIntegrity directly to ThermoState
                if (oldRegime == CollisionRegime.Micro && newRegime == CollisionRegime.Macro)
                {
                    if (IntegrityLookup.HasComponent(entity))
                    {
                        var integrity = IntegrityLookup[entity];
                        var damage = 1f - integrity.Value;

                        // Create thermo state
                        if (!ThermoStateLookup.HasComponent(entity))
                        {
                            Ecb.AddComponent(entityInQueryIndex, entity, new ThermoState
                            {
                                Temperature = 300f + damage * 1000f,
                                MeltPercentage = damage,
                                AtmosphereMass = 1e15f * (1f - damage),
                                BaseTemperature = 300f
                            });
                        }

                        // Remove integrity component
                        Ecb.RemoveComponent<StructuralIntegrity>(entityInQueryIndex, entity);
                    }
                }
            }
        }
    }
}

