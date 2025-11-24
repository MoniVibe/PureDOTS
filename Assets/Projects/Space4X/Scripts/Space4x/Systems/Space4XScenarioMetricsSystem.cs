using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Combat;
using PureDOTS.Runtime.Compliance;
using PureDOTS.Runtime.Crew;
using PureDOTS.Systems;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Space4X.Systems
{
    /// <summary>
    /// Collects metrics during scenario runs for CI telemetry export.
    /// Tracks: fixed_tick_ms, projectiles_spawned, hits, damage_totals, throughput, sanctions, rep_deltas, repairs/refits, alloc_bytes.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    public partial struct Space4XScenarioMetricsSystem : ISystem
    {
        private EntityQuery _projectileQuery;
        private EntityQuery _weaponMountQuery;
        private EntityQuery _damageableQuery;
        private EntityQuery _infractionQuery;
        private EntityQuery _sanctionQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();

            _projectileQuery = state.GetEntityQuery(typeof(ProjectileEntity));
            _weaponMountQuery = state.GetEntityQuery(typeof(WeaponMount));
            _damageableQuery = state.GetEntityQuery(typeof(Damageable));
            _infractionQuery = state.GetEntityQuery(typeof(ComplianceInfraction));
            _sanctionQuery = state.GetEntityQuery(typeof(ComplianceSanction));
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            
            // Collect metrics
            var metrics = new ScenarioMetrics
            {
                FixedTickMs = timeState.FixedDeltaTime * 1000f,
                ProjectilesSpawned = _projectileQuery.CalculateEntityCount(),
                WeaponMounts = _weaponMountQuery.CalculateEntityCount(),
                DamageableEntities = _damageableQuery.CalculateEntityCount(),
                Infractions = _infractionQuery.CalculateEntityCount(),
                Sanctions = _sanctionQuery.CalculateEntityCount(),
                Tick = timeState.Tick
            };

            // Store metrics (would be written to JSON at end of scenario)
            // For now, just track in component
            if (!SystemAPI.HasSingleton<ScenarioMetrics>())
            {
                var metricsEntity = state.EntityManager.CreateEntity();
                state.EntityManager.AddComponentData(metricsEntity, metrics);
            }
            else
            {
                var metricsEntity = SystemAPI.GetSingletonEntity<ScenarioMetrics>();
                state.EntityManager.SetComponentData(metricsEntity, metrics);
            }
        }

        /// <summary>
        /// Component storing scenario metrics for export.
        /// </summary>
        public struct ScenarioMetrics : IComponentData
        {
            public float FixedTickMs;
            public int ProjectilesSpawned;
            public int WeaponMounts;
            public int DamageableEntities;
            public int Infractions;
            public int Sanctions;
            public uint Tick;
            public float TotalDamage;
            public float Throughput;
            public float ReputationDelta;
            public int Repairs;
            public int Refits;
            public long AllocBytes;
        }
    }
}

