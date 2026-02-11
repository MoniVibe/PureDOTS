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
        private EntityQuery _villagerQuery;
        private EntityQuery _shipyardQuery;
        private EntityQuery _weaponMountQuery;
        private EntityQuery _weaponSpawnerQuery;
        private EntityQuery _ammoStockpileQuery;
        private EntityQuery _weaponMagazineQuery;

        [BurstCompile]
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
            ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, "villager.count", _villagerQuery.CalculateEntityCount());

            // Shipyard count and pending equip requests.
            ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, "shipyard.count", _shipyardQuery.CalculateEntityCount());

            double pendingRequests = 0;
            foreach (var requests in SystemAPI.Query<DynamicBuffer<ShipyardEquipRequest>>().WithAll<Shipyard>())
            {
                pendingRequests += requests.Length;
            }
            ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, "shipyard.requests.pending", pendingRequests);

            // Weapon counts (installed mounts/spawners).
            var mountCount = _weaponMountQuery.CalculateEntityCount();
            var spawnerCount = _weaponSpawnerQuery.CalculateEntityCount();
            ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, "weapon.mount.count", mountCount);
            ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, "weapon.spawner.count", spawnerCount);

            var equipSuccess = (mountCount + spawnerCount) > 0 ? 1.0 : 0.0;
            ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, "shipyard.equip.success", equipSuccess);
            if (scenarioInfo.ScenarioId.Equals(new FixedString64Bytes("scenario.puredots.shipyard.equip")))
            {
                ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, "puredots.q.shipyard.equip", equipSuccess);
            }

            // Ammo totals (stockpiles and magazines).
            ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, "ammo.stockpile.count", _ammoStockpileQuery.CalculateEntityCount());
            ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, "ammo.magazine.count", _weaponMagazineQuery.CalculateEntityCount());

            double stockpileCurrent = 0;
            double stockpileCapacity = 0;
            foreach (var stockpile in SystemAPI.Query<RefRO<AmmoStockpile>>())
            {
                stockpileCurrent += stockpile.ValueRO.Current;
                stockpileCapacity += stockpile.ValueRO.Capacity;
            }
            ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, "ammo.stockpile.current_total", stockpileCurrent);
            ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, "ammo.stockpile.capacity_total", stockpileCapacity);

            double magazineCurrent = 0;
            double magazineCapacity = 0;
            foreach (var magazine in SystemAPI.Query<RefRO<WeaponMagazine>>())
            {
                magazineCurrent += magazine.ValueRO.Current;
                magazineCapacity += magazine.ValueRO.Capacity;
            }
            ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, "ammo.magazine.current_total", magazineCurrent);
            ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, "ammo.magazine.capacity_total", magazineCapacity);

            // Projectile tracking counters (audit-friendly totals).
            if (SystemAPI.TryGetSingletonEntity<ProjectileTrackingHub>(out var trackingHubEntity))
            {
                var counters = SystemAPI.GetComponent<ProjectileTrackingCounters>(trackingHubEntity);
                ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, "projectile.tracking.spawned_total", counters.Spawned);
                ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, "projectile.tracking.hits_total", counters.Hits);
                ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, "projectile.tracking.deflect_total", counters.Deflections);
                ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, "projectile.tracking.redirect_total", counters.Redirects);
                ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, "projectile.tracking.control_total", counters.Controls);
                ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, "projectile.tracking.retire_total", counters.Retired);
                ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, "projectile.tracking.expire_total", counters.Expired);
                ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, "projectile.tracking.recycle_total", counters.Recycled);

                if (SystemAPI.HasBuffer<ProjectileTrackingEvent>(trackingHubEntity))
                {
                    var events = SystemAPI.GetBuffer<ProjectileTrackingEvent>(trackingHubEntity);
                    ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, "projectile.tracking.events_count", events.Length);
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

                        var spawnedKey = new FixedString64Bytes("projectile.tracking.spawned.");
                        spawnedKey.Append(entry.AmmoId);
                        ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, spawnedKey, entry.Spawned);

                        var hitKey = new FixedString64Bytes("projectile.tracking.hits.");
                        hitKey.Append(entry.AmmoId);
                        ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, hitKey, entry.Hits);

                        var deflectKey = new FixedString64Bytes("projectile.tracking.deflect.");
                        deflectKey.Append(entry.AmmoId);
                        ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, deflectKey, entry.Deflections);

                        var redirectKey = new FixedString64Bytes("projectile.tracking.redirect.");
                        redirectKey.Append(entry.AmmoId);
                        ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, redirectKey, entry.Redirects);

                        var controlKey = new FixedString64Bytes("projectile.tracking.control.");
                        controlKey.Append(entry.AmmoId);
                        ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, controlKey, entry.Controls);

                        var retireKey = new FixedString64Bytes("projectile.tracking.retire.");
                        retireKey.Append(entry.AmmoId);
                        ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, retireKey, entry.Retired);

                        var expireKey = new FixedString64Bytes("projectile.tracking.expire.");
                        expireKey.Append(entry.AmmoId);
                        ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, expireKey, entry.Expired);

                        var recycleKey = new FixedString64Bytes("projectile.tracking.recycle.");
                        recycleKey.Append(entry.AmmoId);
                        ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, recycleKey, entry.Recycled);
                    }
                }

                var auditMicroId = new FixedString64Bytes("puredots_projectile_tracking_audit_micro");
                var auditScenarioId = new FixedString64Bytes("scenario.puredots.projectile_tracking.audit");
                if (scenarioInfo.ScenarioId.Equals(auditMicroId) || scenarioInfo.ScenarioId.Equals(auditScenarioId))
                {
                    var auditPass = (counters.Spawned > 0 && counters.Hits > 0) ? 1.0 : 0.0;
                    ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, "puredots.q.projectile_tracking.audit", auditPass);
                }

                var lifecycleMicroId = new FixedString64Bytes("puredots_projectile_lifecycle_micro");
                var lifecycleScenarioId = new FixedString64Bytes("scenario.puredots.projectile_lifecycle.audit");
                if (scenarioInfo.ScenarioId.Equals(lifecycleMicroId) || scenarioInfo.ScenarioId.Equals(lifecycleScenarioId))
                {
                    var lifecyclePass = (counters.Retired > 0 && counters.Expired > 0 && counters.Recycled > 0) ? 1.0 : 0.0;
                    ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, "puredots.q.projectile_lifecycle.audit", lifecyclePass);
                }
            }

            // Completed deliveries (DeliveryReceipt buffers).
            double totalDeliveries = 0;
            foreach (var receipts in SystemAPI.Query<DynamicBuffer<DeliveryReceipt>>())
            {
                totalDeliveries += receipts.Length;
            }
            ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, "deliveries.count", totalDeliveries);

            // Total storehouse inventory across all storehouses.
            double totalInventory = 0;
            foreach (var inventory in SystemAPI.Query<DynamicBuffer<StorehouseInventoryItem>>())
            {
                for (int i = 0; i < inventory.Length; i++)
                {
                    totalInventory += inventory[i].Amount;
                }
            }
            ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, "storehouse.inventory", totalInventory);

            // Defaults for boolean metrics – systems can override when violations occur.
            ScenarioMetricsUtility.SetMetricIfUnset(ref metricLookup, scenarioEntity, "constraints.respected", 1.0);
            ScenarioMetricsUtility.SetMetricIfUnset(ref metricLookup, scenarioEntity, "deterministic.replay", 1.0);
        }
    }
}
