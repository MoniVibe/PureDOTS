using PureDOTS.Runtime.Combat;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Resources;
using PureDOTS.Runtime.Scenarios;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Systems.Scenarios
{
    /// <summary>
    /// Collects common scenario metrics so assertions/telemetry can reference them.
    /// Scans gameplay state each LateSimulation tick and writes values into ScenarioMetricsUtility.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    public partial struct ScenarioMetricsCollectorSystem : ISystem
    {
        private FixedString64Bytes VillagerCountKey;
        private FixedString64Bytes ShipyardCountKey;
        private FixedString64Bytes ShipyardRequestsPendingKey;
        private FixedString64Bytes WeaponMountCountKey;
        private FixedString64Bytes WeaponSpawnerCountKey;
        private FixedString64Bytes ShipyardEquipSuccessKey;
        private FixedString64Bytes ShipyardEquipScenarioId;
        private FixedString64Bytes ShipyardEquipMetricKey;
        private FixedString64Bytes AmmoStockpileCountKey;
        private FixedString64Bytes AmmoMagazineCountKey;
        private FixedString64Bytes AmmoStockpileCurrentTotalKey;
        private FixedString64Bytes AmmoStockpileCapacityTotalKey;
        private FixedString64Bytes AmmoMagazineCurrentTotalKey;
        private FixedString64Bytes AmmoMagazineCapacityTotalKey;
        private FixedString64Bytes ProjectileTrackingSpawnedTotalKey;
        private FixedString64Bytes ProjectileTrackingHitsTotalKey;
        private FixedString64Bytes ProjectileTrackingDeflectTotalKey;
        private FixedString64Bytes ProjectileTrackingRedirectTotalKey;
        private FixedString64Bytes ProjectileTrackingControlTotalKey;
        private FixedString64Bytes ProjectileTrackingRetireTotalKey;
        private FixedString64Bytes ProjectileTrackingExpireTotalKey;
        private FixedString64Bytes ProjectileTrackingRecycleTotalKey;
        private FixedString64Bytes ProjectileTrackingEventsCountKey;
        private FixedString64Bytes ProjectileTrackingSpawnedPrefix;
        private FixedString64Bytes ProjectileTrackingHitsPrefix;
        private FixedString64Bytes ProjectileTrackingDeflectPrefix;
        private FixedString64Bytes ProjectileTrackingRedirectPrefix;
        private FixedString64Bytes ProjectileTrackingControlPrefix;
        private FixedString64Bytes ProjectileTrackingRetirePrefix;
        private FixedString64Bytes ProjectileTrackingExpirePrefix;
        private FixedString64Bytes ProjectileTrackingRecyclePrefix;
        private FixedString64Bytes ProjectileTrackingAuditMicroId;
        private FixedString64Bytes ProjectileTrackingAuditScenarioId;
        private FixedString64Bytes ProjectileTrackingAuditMetricKey;
        private FixedString64Bytes ProjectileLifecycleMicroId;
        private FixedString64Bytes ProjectileLifecycleScenarioId;
        private FixedString64Bytes ProjectileLifecycleMetricKey;
        private FixedString64Bytes DeliveriesCountKey;
        private FixedString64Bytes StorehouseInventoryKey;
        private FixedString64Bytes ConstraintsRespectedKey;
        private FixedString64Bytes DeterministicReplayKey;

        private EntityQuery _villagerQuery;
        private EntityQuery _shipyardQuery;
        private EntityQuery _weaponMountQuery;
        private EntityQuery _weaponSpawnerQuery;
        private EntityQuery _ammoStockpileQuery;
        private EntityQuery _weaponMagazineQuery;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ScenarioInfo>();
            _villagerQuery = state.GetEntityQuery(ComponentType.ReadOnly<VillagerId>());
            _shipyardQuery = state.GetEntityQuery(ComponentType.ReadOnly<Shipyard>());
            _weaponMountQuery = state.GetEntityQuery(ComponentType.ReadOnly<WeaponMount>());
            _weaponSpawnerQuery = state.GetEntityQuery(ComponentType.ReadOnly<WeaponSpawner>());
            _ammoStockpileQuery = state.GetEntityQuery(ComponentType.ReadOnly<AmmoStockpile>());
            _weaponMagazineQuery = state.GetEntityQuery(ComponentType.ReadOnly<WeaponMagazine>());
            VillagerCountKey = new FixedString64Bytes("villager.count");
            ShipyardCountKey = new FixedString64Bytes("shipyard.count");
            ShipyardRequestsPendingKey = new FixedString64Bytes("shipyard.requests.pending");
            WeaponMountCountKey = new FixedString64Bytes("weapon.mount.count");
            WeaponSpawnerCountKey = new FixedString64Bytes("weapon.spawner.count");
            ShipyardEquipSuccessKey = new FixedString64Bytes("shipyard.equip.success");
            ShipyardEquipScenarioId = new FixedString64Bytes("scenario.puredots.shipyard.equip");
            ShipyardEquipMetricKey = new FixedString64Bytes("puredots.q.shipyard.equip");
            AmmoStockpileCountKey = new FixedString64Bytes("ammo.stockpile.count");
            AmmoMagazineCountKey = new FixedString64Bytes("ammo.magazine.count");
            AmmoStockpileCurrentTotalKey = new FixedString64Bytes("ammo.stockpile.current_total");
            AmmoStockpileCapacityTotalKey = new FixedString64Bytes("ammo.stockpile.capacity_total");
            AmmoMagazineCurrentTotalKey = new FixedString64Bytes("ammo.magazine.current_total");
            AmmoMagazineCapacityTotalKey = new FixedString64Bytes("ammo.magazine.capacity_total");
            ProjectileTrackingSpawnedTotalKey = new FixedString64Bytes("projectile.tracking.spawned_total");
            ProjectileTrackingHitsTotalKey = new FixedString64Bytes("projectile.tracking.hits_total");
            ProjectileTrackingDeflectTotalKey = new FixedString64Bytes("projectile.tracking.deflect_total");
            ProjectileTrackingRedirectTotalKey = new FixedString64Bytes("projectile.tracking.redirect_total");
            ProjectileTrackingControlTotalKey = new FixedString64Bytes("projectile.tracking.control_total");
            ProjectileTrackingRetireTotalKey = new FixedString64Bytes("projectile.tracking.retire_total");
            ProjectileTrackingExpireTotalKey = new FixedString64Bytes("projectile.tracking.expire_total");
            ProjectileTrackingRecycleTotalKey = new FixedString64Bytes("projectile.tracking.recycle_total");
            ProjectileTrackingEventsCountKey = new FixedString64Bytes("projectile.tracking.events_count");
            ProjectileTrackingSpawnedPrefix = new FixedString64Bytes("projectile.tracking.spawned.");
            ProjectileTrackingHitsPrefix = new FixedString64Bytes("projectile.tracking.hits.");
            ProjectileTrackingDeflectPrefix = new FixedString64Bytes("projectile.tracking.deflect.");
            ProjectileTrackingRedirectPrefix = new FixedString64Bytes("projectile.tracking.redirect.");
            ProjectileTrackingControlPrefix = new FixedString64Bytes("projectile.tracking.control.");
            ProjectileTrackingRetirePrefix = new FixedString64Bytes("projectile.tracking.retire.");
            ProjectileTrackingExpirePrefix = new FixedString64Bytes("projectile.tracking.expire.");
            ProjectileTrackingRecyclePrefix = new FixedString64Bytes("projectile.tracking.recycle.");
            ProjectileTrackingAuditMicroId = new FixedString64Bytes("puredots_projectile_tracking_audit_micro");
            ProjectileTrackingAuditScenarioId = new FixedString64Bytes("scenario.puredots.projectile_tracking.audit");
            ProjectileTrackingAuditMetricKey = new FixedString64Bytes("puredots.q.projectile_tracking.audit");
            ProjectileLifecycleMicroId = new FixedString64Bytes("puredots_projectile_lifecycle_micro");
            ProjectileLifecycleScenarioId = new FixedString64Bytes("scenario.puredots.projectile_lifecycle.audit");
            ProjectileLifecycleMetricKey = new FixedString64Bytes("puredots.q.projectile_lifecycle.audit");
            DeliveriesCountKey = new FixedString64Bytes("deliveries.count");
            StorehouseInventoryKey = new FixedString64Bytes("storehouse.inventory");
            ConstraintsRespectedKey = new FixedString64Bytes("constraints.respected");
            DeterministicReplayKey = new FixedString64Bytes("deterministic.replay");
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var scenarioInfo = SystemAPI.GetSingleton<ScenarioInfo>();
            // Resolve scenario entity
            Entity scenarioEntity = Entity.Null;
            if (SystemAPI.TryGetSingleton<ScenarioEntitySingleton>(out var scenarioSingleton))
            {
                scenarioEntity = scenarioSingleton.Value;
            }
            else if (SystemAPI.HasSingleton<ScenarioInfo>())
            {
                scenarioEntity = SystemAPI.GetSingletonEntity<ScenarioInfo>();
            }

            if (scenarioEntity == Entity.Null)
            {
                return;
            }

            // Get buffer lookup
            var metricLookup = SystemAPI.GetBufferLookup<ScenarioMetricSample>(isReadOnly: false);
            metricLookup.Update(ref state);

            if (!metricLookup.HasBuffer(scenarioEntity))
            {
                return;
            }

            // Villager count (generic VillagerId component).
            ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, VillagerCountKey, _villagerQuery.CalculateEntityCount());

            // Shipyard count and pending equip requests.
            ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, ShipyardCountKey, _shipyardQuery.CalculateEntityCount());

            double pendingRequests = 0;
            foreach (var requests in SystemAPI.Query<DynamicBuffer<ShipyardEquipRequest>>().WithAll<Shipyard>())
            {
                pendingRequests += requests.Length;
            }
            ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, ShipyardRequestsPendingKey, pendingRequests);

            // Weapon counts (installed mounts/spawners).
            var mountCount = _weaponMountQuery.CalculateEntityCount();
            var spawnerCount = _weaponSpawnerQuery.CalculateEntityCount();
            ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, WeaponMountCountKey, mountCount);
            ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, WeaponSpawnerCountKey, spawnerCount);

            var equipSuccess = (mountCount + spawnerCount) > 0 ? 1.0 : 0.0;
            ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, ShipyardEquipSuccessKey, equipSuccess);
            if (scenarioInfo.ScenarioId.Equals(ShipyardEquipScenarioId))
            {
                ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, ShipyardEquipMetricKey, equipSuccess);
            }

            // Ammo totals (stockpiles and magazines).
            ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, AmmoStockpileCountKey, _ammoStockpileQuery.CalculateEntityCount());
            ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, AmmoMagazineCountKey, _weaponMagazineQuery.CalculateEntityCount());

            double stockpileCurrent = 0;
            double stockpileCapacity = 0;
            foreach (var stockpile in SystemAPI.Query<RefRO<AmmoStockpile>>())
            {
                stockpileCurrent += stockpile.ValueRO.Current;
                stockpileCapacity += stockpile.ValueRO.Capacity;
            }
            ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, AmmoStockpileCurrentTotalKey, stockpileCurrent);
            ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, AmmoStockpileCapacityTotalKey, stockpileCapacity);

            double magazineCurrent = 0;
            double magazineCapacity = 0;
            foreach (var magazine in SystemAPI.Query<RefRO<WeaponMagazine>>())
            {
                magazineCurrent += magazine.ValueRO.Current;
                magazineCapacity += magazine.ValueRO.Capacity;
            }
            ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, AmmoMagazineCurrentTotalKey, magazineCurrent);
            ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, AmmoMagazineCapacityTotalKey, magazineCapacity);

            // Projectile tracking counters (audit-friendly totals).
            if (SystemAPI.TryGetSingletonEntity<ProjectileTrackingHub>(out var trackingHubEntity))
            {
                var counters = SystemAPI.GetComponent<ProjectileTrackingCounters>(trackingHubEntity);
                ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, ProjectileTrackingSpawnedTotalKey, counters.Spawned);
                ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, ProjectileTrackingHitsTotalKey, counters.Hits);
                ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, ProjectileTrackingDeflectTotalKey, counters.Deflections);
                ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, ProjectileTrackingRedirectTotalKey, counters.Redirects);
                ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, ProjectileTrackingControlTotalKey, counters.Controls);
                ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, ProjectileTrackingRetireTotalKey, counters.Retired);
                ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, ProjectileTrackingExpireTotalKey, counters.Expired);
                ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, ProjectileTrackingRecycleTotalKey, counters.Recycled);

                if (SystemAPI.HasBuffer<ProjectileTrackingEvent>(trackingHubEntity))
                {
                    var events = SystemAPI.GetBuffer<ProjectileTrackingEvent>(trackingHubEntity);
                    ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, ProjectileTrackingEventsCountKey, events.Length);
                }

                if (SystemAPI.HasBuffer<ProjectileTrackingAmmoCounter>(trackingHubEntity))
                {
                    var ammoCounters = SystemAPI.GetBuffer<ProjectileTrackingAmmoCounter>(trackingHubEntity);
                    for (int i = 0; i < ammoCounters.Length; i++)
                    {
                        var entry = ammoCounters[i];
                        if (entry.AmmoId.Length == 0)
                        {
                            continue;
                        }

                        var spawnedKey = ProjectileTrackingSpawnedPrefix;
                        spawnedKey.Append(entry.AmmoId);
                        ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, spawnedKey, entry.Spawned);

                        var hitKey = ProjectileTrackingHitsPrefix;
                        hitKey.Append(entry.AmmoId);
                        ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, hitKey, entry.Hits);

                        var deflectKey = ProjectileTrackingDeflectPrefix;
                        deflectKey.Append(entry.AmmoId);
                        ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, deflectKey, entry.Deflections);

                        var redirectKey = ProjectileTrackingRedirectPrefix;
                        redirectKey.Append(entry.AmmoId);
                        ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, redirectKey, entry.Redirects);

                        var controlKey = ProjectileTrackingControlPrefix;
                        controlKey.Append(entry.AmmoId);
                        ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, controlKey, entry.Controls);

                        var retireKey = ProjectileTrackingRetirePrefix;
                        retireKey.Append(entry.AmmoId);
                        ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, retireKey, entry.Retired);

                        var expireKey = ProjectileTrackingExpirePrefix;
                        expireKey.Append(entry.AmmoId);
                        ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, expireKey, entry.Expired);

                        var recycleKey = ProjectileTrackingRecyclePrefix;
                        recycleKey.Append(entry.AmmoId);
                        ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, recycleKey, entry.Recycled);
                    }
                }

                if (scenarioInfo.ScenarioId.Equals(ProjectileTrackingAuditMicroId) || scenarioInfo.ScenarioId.Equals(ProjectileTrackingAuditScenarioId))
                {
                    var auditPass = (counters.Spawned > 0 && counters.Hits > 0) ? 1.0 : 0.0;
                    ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, ProjectileTrackingAuditMetricKey, auditPass);
                }

                if (scenarioInfo.ScenarioId.Equals(ProjectileLifecycleMicroId) || scenarioInfo.ScenarioId.Equals(ProjectileLifecycleScenarioId))
                {
                    var lifecyclePass = (counters.Retired > 0 && counters.Expired > 0 && counters.Recycled > 0) ? 1.0 : 0.0;
                    ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, ProjectileLifecycleMetricKey, lifecyclePass);
                }
            }

            // Completed deliveries (DeliveryReceipt buffers).
            double totalDeliveries = 0;
            foreach (var receipts in SystemAPI.Query<DynamicBuffer<DeliveryReceipt>>())
            {
                totalDeliveries += receipts.Length;
            }
            ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, DeliveriesCountKey, totalDeliveries);

            // Total storehouse inventory across all storehouses.
            double totalInventory = 0;
            foreach (var inventory in SystemAPI.Query<DynamicBuffer<StorehouseInventoryItem>>())
            {
                for (int i = 0; i < inventory.Length; i++)
                {
                    totalInventory += inventory[i].Amount;
                }
            }
            ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, StorehouseInventoryKey, totalInventory);

            // Defaults for boolean metrics – systems can override when violations occur.
            ScenarioMetricsUtility.SetMetricIfUnset(ref metricLookup, scenarioEntity, ConstraintsRespectedKey, 1.0);
            ScenarioMetricsUtility.SetMetricIfUnset(ref metricLookup, scenarioEntity, DeterministicReplayKey, 1.0);
        }
    }
}

