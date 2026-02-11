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
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    public partial struct ScenarioMetricsCollectorSystem : ISystem
    {
        private static readonly FixedString64Bytes VillagerCountKey = new FixedString64Bytes("villager.count");
        private static readonly FixedString64Bytes ShipyardCountKey = new FixedString64Bytes("shipyard.count");
        private static readonly FixedString64Bytes ShipyardRequestsPendingKey = new FixedString64Bytes("shipyard.requests.pending");
        private static readonly FixedString64Bytes WeaponMountCountKey = new FixedString64Bytes("weapon.mount.count");
        private static readonly FixedString64Bytes WeaponSpawnerCountKey = new FixedString64Bytes("weapon.spawner.count");
        private static readonly FixedString64Bytes ShipyardEquipSuccessKey = new FixedString64Bytes("shipyard.equip.success");
        private static readonly FixedString64Bytes ShipyardEquipScenarioId = new FixedString64Bytes("scenario.puredots.shipyard.equip");
        private static readonly FixedString64Bytes ShipyardEquipMetricKey = new FixedString64Bytes("puredots.q.shipyard.equip");
        private static readonly FixedString64Bytes AmmoStockpileCountKey = new FixedString64Bytes("ammo.stockpile.count");
        private static readonly FixedString64Bytes AmmoMagazineCountKey = new FixedString64Bytes("ammo.magazine.count");
        private static readonly FixedString64Bytes AmmoStockpileCurrentTotalKey = new FixedString64Bytes("ammo.stockpile.current_total");
        private static readonly FixedString64Bytes AmmoStockpileCapacityTotalKey = new FixedString64Bytes("ammo.stockpile.capacity_total");
        private static readonly FixedString64Bytes AmmoMagazineCurrentTotalKey = new FixedString64Bytes("ammo.magazine.current_total");
        private static readonly FixedString64Bytes AmmoMagazineCapacityTotalKey = new FixedString64Bytes("ammo.magazine.capacity_total");
        private static readonly FixedString64Bytes ProjectileTrackingSpawnedTotalKey = new FixedString64Bytes("projectile.tracking.spawned_total");
        private static readonly FixedString64Bytes ProjectileTrackingHitsTotalKey = new FixedString64Bytes("projectile.tracking.hits_total");
        private static readonly FixedString64Bytes ProjectileTrackingDeflectTotalKey = new FixedString64Bytes("projectile.tracking.deflect_total");
        private static readonly FixedString64Bytes ProjectileTrackingRedirectTotalKey = new FixedString64Bytes("projectile.tracking.redirect_total");
        private static readonly FixedString64Bytes ProjectileTrackingControlTotalKey = new FixedString64Bytes("projectile.tracking.control_total");
        private static readonly FixedString64Bytes ProjectileTrackingRetireTotalKey = new FixedString64Bytes("projectile.tracking.retire_total");
        private static readonly FixedString64Bytes ProjectileTrackingExpireTotalKey = new FixedString64Bytes("projectile.tracking.expire_total");
        private static readonly FixedString64Bytes ProjectileTrackingRecycleTotalKey = new FixedString64Bytes("projectile.tracking.recycle_total");
        private static readonly FixedString64Bytes ProjectileTrackingEventsCountKey = new FixedString64Bytes("projectile.tracking.events_count");
        private static readonly FixedString64Bytes ProjectileTrackingSpawnedPrefix = new FixedString64Bytes("projectile.tracking.spawned.");
        private static readonly FixedString64Bytes ProjectileTrackingHitsPrefix = new FixedString64Bytes("projectile.tracking.hits.");
        private static readonly FixedString64Bytes ProjectileTrackingDeflectPrefix = new FixedString64Bytes("projectile.tracking.deflect.");
        private static readonly FixedString64Bytes ProjectileTrackingRedirectPrefix = new FixedString64Bytes("projectile.tracking.redirect.");
        private static readonly FixedString64Bytes ProjectileTrackingControlPrefix = new FixedString64Bytes("projectile.tracking.control.");
        private static readonly FixedString64Bytes ProjectileTrackingRetirePrefix = new FixedString64Bytes("projectile.tracking.retire.");
        private static readonly FixedString64Bytes ProjectileTrackingExpirePrefix = new FixedString64Bytes("projectile.tracking.expire.");
        private static readonly FixedString64Bytes ProjectileTrackingRecyclePrefix = new FixedString64Bytes("projectile.tracking.recycle.");
        private static readonly FixedString64Bytes ProjectileTrackingAuditMicroId = new FixedString64Bytes("puredots_projectile_tracking_audit_micro");
        private static readonly FixedString64Bytes ProjectileTrackingAuditScenarioId = new FixedString64Bytes("scenario.puredots.projectile_tracking.audit");
        private static readonly FixedString64Bytes ProjectileTrackingAuditMetricKey = new FixedString64Bytes("puredots.q.projectile_tracking.audit");
        private static readonly FixedString64Bytes ProjectileLifecycleMicroId = new FixedString64Bytes("puredots_projectile_lifecycle_micro");
        private static readonly FixedString64Bytes ProjectileLifecycleScenarioId = new FixedString64Bytes("scenario.puredots.projectile_lifecycle.audit");
        private static readonly FixedString64Bytes ProjectileLifecycleMetricKey = new FixedString64Bytes("puredots.q.projectile_lifecycle.audit");
        private static readonly FixedString64Bytes DeliveriesCountKey = new FixedString64Bytes("deliveries.count");
        private static readonly FixedString64Bytes StorehouseInventoryKey = new FixedString64Bytes("storehouse.inventory");
        private static readonly FixedString64Bytes ConstraintsRespectedKey = new FixedString64Bytes("constraints.respected");
        private static readonly FixedString64Bytes DeterministicReplayKey = new FixedString64Bytes("deterministic.replay");

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
        }

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
