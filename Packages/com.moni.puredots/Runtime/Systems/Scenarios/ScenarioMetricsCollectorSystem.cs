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
        // Avoid static FixedString initializers: Burst compilation of type static constructors can fail due to managed-string helpers
        // referenced by Unity.Collections FixedString methods (BC1016/BC1091). These are initialized in OnCreate instead.
        private FixedString64Bytes _villagerCountKey;
        private FixedString64Bytes _shipyardCountKey;
        private FixedString64Bytes _shipyardRequestsPendingKey;
        private FixedString64Bytes _weaponMountCountKey;
        private FixedString64Bytes _weaponSpawnerCountKey;
        private FixedString64Bytes _shipyardEquipSuccessKey;
        private FixedString64Bytes _shipyardEquipScenarioId;
        private FixedString64Bytes _shipyardEquipMetricKey;
        private FixedString64Bytes _ammoStockpileCountKey;
        private FixedString64Bytes _ammoMagazineCountKey;
        private FixedString64Bytes _ammoStockpileCurrentTotalKey;
        private FixedString64Bytes _ammoStockpileCapacityTotalKey;
        private FixedString64Bytes _ammoMagazineCurrentTotalKey;
        private FixedString64Bytes _ammoMagazineCapacityTotalKey;
        private FixedString64Bytes _projectileTrackingSpawnedTotalKey;
        private FixedString64Bytes _projectileTrackingHitsTotalKey;
        private FixedString64Bytes _projectileTrackingDeflectTotalKey;
        private FixedString64Bytes _projectileTrackingRedirectTotalKey;
        private FixedString64Bytes _projectileTrackingControlTotalKey;
        private FixedString64Bytes _projectileTrackingRetireTotalKey;
        private FixedString64Bytes _projectileTrackingExpireTotalKey;
        private FixedString64Bytes _projectileTrackingRecycleTotalKey;
        private FixedString64Bytes _projectileTrackingEventsCountKey;
        private FixedString64Bytes _projectileTrackingSpawnedPrefix;
        private FixedString64Bytes _projectileTrackingHitsPrefix;
        private FixedString64Bytes _projectileTrackingDeflectPrefix;
        private FixedString64Bytes _projectileTrackingRedirectPrefix;
        private FixedString64Bytes _projectileTrackingControlPrefix;
        private FixedString64Bytes _projectileTrackingRetirePrefix;
        private FixedString64Bytes _projectileTrackingExpirePrefix;
        private FixedString64Bytes _projectileTrackingRecyclePrefix;
        private FixedString64Bytes _projectileTrackingAuditMicroId;
        private FixedString64Bytes _projectileTrackingAuditScenarioId;
        private FixedString64Bytes _projectileTrackingAuditMetricKey;
        private FixedString64Bytes _projectileLifecycleMicroId;
        private FixedString64Bytes _projectileLifecycleScenarioId;
        private FixedString64Bytes _projectileLifecycleMetricKey;
        private FixedString64Bytes _deliveriesCountKey;
        private FixedString64Bytes _storehouseInventoryKey;
        private FixedString64Bytes _constraintsRespectedKey;
        private FixedString64Bytes _deterministicReplayKey;

        private static FixedString64Bytes CreateVillagerCountKey()
        {
            FixedString64Bytes s = default;
            s.Append('v'); s.Append('i'); s.Append('l'); s.Append('l'); s.Append('a'); s.Append('g'); s.Append('e'); s.Append('r');
            s.Append('.'); s.Append('c'); s.Append('o'); s.Append('u'); s.Append('n'); s.Append('t');
            return s;
        }

        private static FixedString64Bytes CreateShipyardCountKey()
        {
            FixedString64Bytes s = default;
            s.Append('s'); s.Append('h'); s.Append('i'); s.Append('p'); s.Append('y'); s.Append('a'); s.Append('r'); s.Append('d');
            s.Append('.'); s.Append('c'); s.Append('o'); s.Append('u'); s.Append('n'); s.Append('t');
            return s;
        }

        private static FixedString64Bytes CreateShipyardRequestsPendingKey()
        {
            FixedString64Bytes s = default;
            s.Append('s'); s.Append('h'); s.Append('i'); s.Append('p'); s.Append('y'); s.Append('a'); s.Append('r'); s.Append('d');
            s.Append('.'); s.Append('r'); s.Append('e'); s.Append('q'); s.Append('u'); s.Append('e'); s.Append('s'); s.Append('t');
            s.Append('s'); s.Append('.'); s.Append('p'); s.Append('e'); s.Append('n'); s.Append('d'); s.Append('i'); s.Append('n');
            s.Append('g');
            return s;
        }

        private static FixedString64Bytes CreateWeaponMountCountKey()
        {
            FixedString64Bytes s = default;
            s.Append('w'); s.Append('e'); s.Append('a'); s.Append('p'); s.Append('o'); s.Append('n'); s.Append('.'); s.Append('m');
            s.Append('o'); s.Append('u'); s.Append('n'); s.Append('t'); s.Append('.'); s.Append('c'); s.Append('o'); s.Append('u');
            s.Append('n'); s.Append('t');
            return s;
        }

        private static FixedString64Bytes CreateWeaponSpawnerCountKey()
        {
            FixedString64Bytes s = default;
            s.Append('w'); s.Append('e'); s.Append('a'); s.Append('p'); s.Append('o'); s.Append('n'); s.Append('.'); s.Append('s');
            s.Append('p'); s.Append('a'); s.Append('w'); s.Append('n'); s.Append('e'); s.Append('r'); s.Append('.'); s.Append('c');
            s.Append('o'); s.Append('u'); s.Append('n'); s.Append('t');
            return s;
        }

        private static FixedString64Bytes CreateShipyardEquipSuccessKey()
        {
            FixedString64Bytes s = default;
            s.Append('s'); s.Append('h'); s.Append('i'); s.Append('p'); s.Append('y'); s.Append('a'); s.Append('r'); s.Append('d');
            s.Append('.'); s.Append('e'); s.Append('q'); s.Append('u'); s.Append('i'); s.Append('p'); s.Append('.'); s.Append('s');
            s.Append('u'); s.Append('c'); s.Append('c'); s.Append('e'); s.Append('s'); s.Append('s');
            return s;
        }

        private static FixedString64Bytes CreateShipyardEquipScenarioId()
        {
            FixedString64Bytes s = default;
            s.Append('s'); s.Append('c'); s.Append('e'); s.Append('n'); s.Append('a'); s.Append('r'); s.Append('i'); s.Append('o');
            s.Append('.'); s.Append('p'); s.Append('u'); s.Append('r'); s.Append('e'); s.Append('d'); s.Append('o'); s.Append('t');
            s.Append('s'); s.Append('.'); s.Append('s'); s.Append('h'); s.Append('i'); s.Append('p'); s.Append('y'); s.Append('a');
            s.Append('r'); s.Append('d'); s.Append('.'); s.Append('e'); s.Append('q'); s.Append('u'); s.Append('i'); s.Append('p');
            return s;
        }

        private static FixedString64Bytes CreateShipyardEquipMetricKey()
        {
            FixedString64Bytes s = default;
            s.Append('p'); s.Append('u'); s.Append('r'); s.Append('e'); s.Append('d'); s.Append('o'); s.Append('t'); s.Append('s');
            s.Append('.'); s.Append('q'); s.Append('.'); s.Append('s'); s.Append('h'); s.Append('i'); s.Append('p'); s.Append('y');
            s.Append('a'); s.Append('r'); s.Append('d'); s.Append('.'); s.Append('e'); s.Append('q'); s.Append('u'); s.Append('i');
            s.Append('p');
            return s;
        }

        private static FixedString64Bytes CreateAmmoStockpileCountKey()
        {
            FixedString64Bytes s = default;
            s.Append('a'); s.Append('m'); s.Append('m'); s.Append('o'); s.Append('.'); s.Append('s'); s.Append('t'); s.Append('o');
            s.Append('c'); s.Append('k'); s.Append('p'); s.Append('i'); s.Append('l'); s.Append('e'); s.Append('.'); s.Append('c');
            s.Append('o'); s.Append('u'); s.Append('n'); s.Append('t');
            return s;
        }

        private static FixedString64Bytes CreateAmmoMagazineCountKey()
        {
            FixedString64Bytes s = default;
            s.Append('a'); s.Append('m'); s.Append('m'); s.Append('o'); s.Append('.'); s.Append('m'); s.Append('a'); s.Append('g');
            s.Append('a'); s.Append('z'); s.Append('i'); s.Append('n'); s.Append('e'); s.Append('.'); s.Append('c'); s.Append('o');
            s.Append('u'); s.Append('n'); s.Append('t');
            return s;
        }

        private static FixedString64Bytes CreateAmmoStockpileCurrentTotalKey()
        {
            FixedString64Bytes s = default;
            s.Append('a'); s.Append('m'); s.Append('m'); s.Append('o'); s.Append('.'); s.Append('s'); s.Append('t'); s.Append('o');
            s.Append('c'); s.Append('k'); s.Append('p'); s.Append('i'); s.Append('l'); s.Append('e'); s.Append('.'); s.Append('c');
            s.Append('u'); s.Append('r'); s.Append('r'); s.Append('e'); s.Append('n'); s.Append('t'); s.Append('_'); s.Append('t');
            s.Append('o'); s.Append('t'); s.Append('a'); s.Append('l');
            return s;
        }

        private static FixedString64Bytes CreateAmmoStockpileCapacityTotalKey()
        {
            FixedString64Bytes s = default;
            s.Append('a'); s.Append('m'); s.Append('m'); s.Append('o'); s.Append('.'); s.Append('s'); s.Append('t'); s.Append('o');
            s.Append('c'); s.Append('k'); s.Append('p'); s.Append('i'); s.Append('l'); s.Append('e'); s.Append('.'); s.Append('c');
            s.Append('a'); s.Append('p'); s.Append('a'); s.Append('c'); s.Append('i'); s.Append('t'); s.Append('y'); s.Append('_');
            s.Append('t'); s.Append('o'); s.Append('t'); s.Append('a'); s.Append('l');
            return s;
        }

        private static FixedString64Bytes CreateAmmoMagazineCurrentTotalKey()
        {
            FixedString64Bytes s = default;
            s.Append('a'); s.Append('m'); s.Append('m'); s.Append('o'); s.Append('.'); s.Append('m'); s.Append('a'); s.Append('g');
            s.Append('a'); s.Append('z'); s.Append('i'); s.Append('n'); s.Append('e'); s.Append('.'); s.Append('c'); s.Append('u');
            s.Append('r'); s.Append('r'); s.Append('e'); s.Append('n'); s.Append('t'); s.Append('_'); s.Append('t'); s.Append('o');
            s.Append('t'); s.Append('a'); s.Append('l');
            return s;
        }

        private static FixedString64Bytes CreateAmmoMagazineCapacityTotalKey()
        {
            FixedString64Bytes s = default;
            s.Append('a'); s.Append('m'); s.Append('m'); s.Append('o'); s.Append('.'); s.Append('m'); s.Append('a'); s.Append('g');
            s.Append('a'); s.Append('z'); s.Append('i'); s.Append('n'); s.Append('e'); s.Append('.'); s.Append('c'); s.Append('a');
            s.Append('p'); s.Append('a'); s.Append('c'); s.Append('i'); s.Append('t'); s.Append('y'); s.Append('_'); s.Append('t');
            s.Append('o'); s.Append('t'); s.Append('a'); s.Append('l');
            return s;
        }

        private static FixedString64Bytes CreateProjectileTrackingSpawnedTotalKey()
        {
            FixedString64Bytes s = default;
            s.Append('p'); s.Append('r'); s.Append('o'); s.Append('j'); s.Append('e'); s.Append('c'); s.Append('t'); s.Append('i');
            s.Append('l'); s.Append('e'); s.Append('.'); s.Append('t'); s.Append('r'); s.Append('a'); s.Append('c'); s.Append('k');
            s.Append('i'); s.Append('n'); s.Append('g'); s.Append('.'); s.Append('s'); s.Append('p'); s.Append('a'); s.Append('w');
            s.Append('n'); s.Append('e'); s.Append('d'); s.Append('_'); s.Append('t'); s.Append('o'); s.Append('t'); s.Append('a');
            s.Append('l');
            return s;
        }

        private static FixedString64Bytes CreateProjectileTrackingHitsTotalKey()
        {
            FixedString64Bytes s = default;
            s.Append('p'); s.Append('r'); s.Append('o'); s.Append('j'); s.Append('e'); s.Append('c'); s.Append('t'); s.Append('i');
            s.Append('l'); s.Append('e'); s.Append('.'); s.Append('t'); s.Append('r'); s.Append('a'); s.Append('c'); s.Append('k');
            s.Append('i'); s.Append('n'); s.Append('g'); s.Append('.'); s.Append('h'); s.Append('i'); s.Append('t'); s.Append('s');
            s.Append('_'); s.Append('t'); s.Append('o'); s.Append('t'); s.Append('a'); s.Append('l');
            return s;
        }

        private static FixedString64Bytes CreateProjectileTrackingDeflectTotalKey()
        {
            FixedString64Bytes s = default;
            s.Append('p'); s.Append('r'); s.Append('o'); s.Append('j'); s.Append('e'); s.Append('c'); s.Append('t'); s.Append('i');
            s.Append('l'); s.Append('e'); s.Append('.'); s.Append('t'); s.Append('r'); s.Append('a'); s.Append('c'); s.Append('k');
            s.Append('i'); s.Append('n'); s.Append('g'); s.Append('.'); s.Append('d'); s.Append('e'); s.Append('f'); s.Append('l');
            s.Append('e'); s.Append('c'); s.Append('t'); s.Append('_'); s.Append('t'); s.Append('o'); s.Append('t'); s.Append('a');
            s.Append('l');
            return s;
        }

        private static FixedString64Bytes CreateProjectileTrackingRedirectTotalKey()
        {
            FixedString64Bytes s = default;
            s.Append('p'); s.Append('r'); s.Append('o'); s.Append('j'); s.Append('e'); s.Append('c'); s.Append('t'); s.Append('i');
            s.Append('l'); s.Append('e'); s.Append('.'); s.Append('t'); s.Append('r'); s.Append('a'); s.Append('c'); s.Append('k');
            s.Append('i'); s.Append('n'); s.Append('g'); s.Append('.'); s.Append('r'); s.Append('e'); s.Append('d'); s.Append('i');
            s.Append('r'); s.Append('e'); s.Append('c'); s.Append('t'); s.Append('_'); s.Append('t'); s.Append('o'); s.Append('t');
            s.Append('a'); s.Append('l');
            return s;
        }

        private static FixedString64Bytes CreateProjectileTrackingControlTotalKey()
        {
            FixedString64Bytes s = default;
            s.Append('p'); s.Append('r'); s.Append('o'); s.Append('j'); s.Append('e'); s.Append('c'); s.Append('t'); s.Append('i');
            s.Append('l'); s.Append('e'); s.Append('.'); s.Append('t'); s.Append('r'); s.Append('a'); s.Append('c'); s.Append('k');
            s.Append('i'); s.Append('n'); s.Append('g'); s.Append('.'); s.Append('c'); s.Append('o'); s.Append('n'); s.Append('t');
            s.Append('r'); s.Append('o'); s.Append('l'); s.Append('_'); s.Append('t'); s.Append('o'); s.Append('t'); s.Append('a');
            s.Append('l');
            return s;
        }

        private static FixedString64Bytes CreateProjectileTrackingRetireTotalKey()
        {
            FixedString64Bytes s = default;
            s.Append('p'); s.Append('r'); s.Append('o'); s.Append('j'); s.Append('e'); s.Append('c'); s.Append('t'); s.Append('i');
            s.Append('l'); s.Append('e'); s.Append('.'); s.Append('t'); s.Append('r'); s.Append('a'); s.Append('c'); s.Append('k');
            s.Append('i'); s.Append('n'); s.Append('g'); s.Append('.'); s.Append('r'); s.Append('e'); s.Append('t'); s.Append('i');
            s.Append('r'); s.Append('e'); s.Append('_'); s.Append('t'); s.Append('o'); s.Append('t'); s.Append('a'); s.Append('l');
            return s;
        }

        private static FixedString64Bytes CreateProjectileTrackingExpireTotalKey()
        {
            FixedString64Bytes s = default;
            s.Append('p'); s.Append('r'); s.Append('o'); s.Append('j'); s.Append('e'); s.Append('c'); s.Append('t'); s.Append('i');
            s.Append('l'); s.Append('e'); s.Append('.'); s.Append('t'); s.Append('r'); s.Append('a'); s.Append('c'); s.Append('k');
            s.Append('i'); s.Append('n'); s.Append('g'); s.Append('.'); s.Append('e'); s.Append('x'); s.Append('p'); s.Append('i');
            s.Append('r'); s.Append('e'); s.Append('_'); s.Append('t'); s.Append('o'); s.Append('t'); s.Append('a'); s.Append('l');
            return s;
        }

        private static FixedString64Bytes CreateProjectileTrackingRecycleTotalKey()
        {
            FixedString64Bytes s = default;
            s.Append('p'); s.Append('r'); s.Append('o'); s.Append('j'); s.Append('e'); s.Append('c'); s.Append('t'); s.Append('i');
            s.Append('l'); s.Append('e'); s.Append('.'); s.Append('t'); s.Append('r'); s.Append('a'); s.Append('c'); s.Append('k');
            s.Append('i'); s.Append('n'); s.Append('g'); s.Append('.'); s.Append('r'); s.Append('e'); s.Append('c'); s.Append('y');
            s.Append('c'); s.Append('l'); s.Append('e'); s.Append('_'); s.Append('t'); s.Append('o'); s.Append('t'); s.Append('a');
            s.Append('l');
            return s;
        }

        private static FixedString64Bytes CreateProjectileTrackingEventsCountKey()
        {
            FixedString64Bytes s = default;
            s.Append('p'); s.Append('r'); s.Append('o'); s.Append('j'); s.Append('e'); s.Append('c'); s.Append('t'); s.Append('i');
            s.Append('l'); s.Append('e'); s.Append('.'); s.Append('t'); s.Append('r'); s.Append('a'); s.Append('c'); s.Append('k');
            s.Append('i'); s.Append('n'); s.Append('g'); s.Append('.'); s.Append('e'); s.Append('v'); s.Append('e'); s.Append('n');
            s.Append('t'); s.Append('s'); s.Append('_'); s.Append('c'); s.Append('o'); s.Append('u'); s.Append('n'); s.Append('t');
            return s;
        }

        private static FixedString64Bytes CreateProjectileTrackingSpawnedPrefix()
        {
            FixedString64Bytes s = default;
            s.Append('p'); s.Append('r'); s.Append('o'); s.Append('j'); s.Append('e'); s.Append('c'); s.Append('t'); s.Append('i');
            s.Append('l'); s.Append('e'); s.Append('.'); s.Append('t'); s.Append('r'); s.Append('a'); s.Append('c'); s.Append('k');
            s.Append('i'); s.Append('n'); s.Append('g'); s.Append('.'); s.Append('s'); s.Append('p'); s.Append('a'); s.Append('w');
            s.Append('n'); s.Append('e'); s.Append('d'); s.Append('.');
            return s;
        }

        private static FixedString64Bytes CreateProjectileTrackingHitsPrefix()
        {
            FixedString64Bytes s = default;
            s.Append('p'); s.Append('r'); s.Append('o'); s.Append('j'); s.Append('e'); s.Append('c'); s.Append('t'); s.Append('i');
            s.Append('l'); s.Append('e'); s.Append('.'); s.Append('t'); s.Append('r'); s.Append('a'); s.Append('c'); s.Append('k');
            s.Append('i'); s.Append('n'); s.Append('g'); s.Append('.'); s.Append('h'); s.Append('i'); s.Append('t'); s.Append('s');
            s.Append('.');
            return s;
        }

        private static FixedString64Bytes CreateProjectileTrackingDeflectPrefix()
        {
            FixedString64Bytes s = default;
            s.Append('p'); s.Append('r'); s.Append('o'); s.Append('j'); s.Append('e'); s.Append('c'); s.Append('t'); s.Append('i');
            s.Append('l'); s.Append('e'); s.Append('.'); s.Append('t'); s.Append('r'); s.Append('a'); s.Append('c'); s.Append('k');
            s.Append('i'); s.Append('n'); s.Append('g'); s.Append('.'); s.Append('d'); s.Append('e'); s.Append('f'); s.Append('l');
            s.Append('e'); s.Append('c'); s.Append('t'); s.Append('.');
            return s;
        }

        private static FixedString64Bytes CreateProjectileTrackingRedirectPrefix()
        {
            FixedString64Bytes s = default;
            s.Append('p'); s.Append('r'); s.Append('o'); s.Append('j'); s.Append('e'); s.Append('c'); s.Append('t'); s.Append('i');
            s.Append('l'); s.Append('e'); s.Append('.'); s.Append('t'); s.Append('r'); s.Append('a'); s.Append('c'); s.Append('k');
            s.Append('i'); s.Append('n'); s.Append('g'); s.Append('.'); s.Append('r'); s.Append('e'); s.Append('d'); s.Append('i');
            s.Append('r'); s.Append('e'); s.Append('c'); s.Append('t'); s.Append('.');
            return s;
        }

        private static FixedString64Bytes CreateProjectileTrackingControlPrefix()
        {
            FixedString64Bytes s = default;
            s.Append('p'); s.Append('r'); s.Append('o'); s.Append('j'); s.Append('e'); s.Append('c'); s.Append('t'); s.Append('i');
            s.Append('l'); s.Append('e'); s.Append('.'); s.Append('t'); s.Append('r'); s.Append('a'); s.Append('c'); s.Append('k');
            s.Append('i'); s.Append('n'); s.Append('g'); s.Append('.'); s.Append('c'); s.Append('o'); s.Append('n'); s.Append('t');
            s.Append('r'); s.Append('o'); s.Append('l'); s.Append('.');
            return s;
        }

        private static FixedString64Bytes CreateProjectileTrackingRetirePrefix()
        {
            FixedString64Bytes s = default;
            s.Append('p'); s.Append('r'); s.Append('o'); s.Append('j'); s.Append('e'); s.Append('c'); s.Append('t'); s.Append('i');
            s.Append('l'); s.Append('e'); s.Append('.'); s.Append('t'); s.Append('r'); s.Append('a'); s.Append('c'); s.Append('k');
            s.Append('i'); s.Append('n'); s.Append('g'); s.Append('.'); s.Append('r'); s.Append('e'); s.Append('t'); s.Append('i');
            s.Append('r'); s.Append('e'); s.Append('.');
            return s;
        }

        private static FixedString64Bytes CreateProjectileTrackingExpirePrefix()
        {
            FixedString64Bytes s = default;
            s.Append('p'); s.Append('r'); s.Append('o'); s.Append('j'); s.Append('e'); s.Append('c'); s.Append('t'); s.Append('i');
            s.Append('l'); s.Append('e'); s.Append('.'); s.Append('t'); s.Append('r'); s.Append('a'); s.Append('c'); s.Append('k');
            s.Append('i'); s.Append('n'); s.Append('g'); s.Append('.'); s.Append('e'); s.Append('x'); s.Append('p'); s.Append('i');
            s.Append('r'); s.Append('e'); s.Append('.');
            return s;
        }

        private static FixedString64Bytes CreateProjectileTrackingRecyclePrefix()
        {
            FixedString64Bytes s = default;
            s.Append('p'); s.Append('r'); s.Append('o'); s.Append('j'); s.Append('e'); s.Append('c'); s.Append('t'); s.Append('i');
            s.Append('l'); s.Append('e'); s.Append('.'); s.Append('t'); s.Append('r'); s.Append('a'); s.Append('c'); s.Append('k');
            s.Append('i'); s.Append('n'); s.Append('g'); s.Append('.'); s.Append('r'); s.Append('e'); s.Append('c'); s.Append('y');
            s.Append('c'); s.Append('l'); s.Append('e'); s.Append('.');
            return s;
        }

        private static FixedString64Bytes CreateProjectileTrackingAuditMicroId()
        {
            FixedString64Bytes s = default;
            s.Append('p'); s.Append('u'); s.Append('r'); s.Append('e'); s.Append('d'); s.Append('o'); s.Append('t'); s.Append('s');
            s.Append('_'); s.Append('p'); s.Append('r'); s.Append('o'); s.Append('j'); s.Append('e'); s.Append('c'); s.Append('t');
            s.Append('i'); s.Append('l'); s.Append('e'); s.Append('_'); s.Append('t'); s.Append('r'); s.Append('a'); s.Append('c');
            s.Append('k'); s.Append('i'); s.Append('n'); s.Append('g'); s.Append('_'); s.Append('a'); s.Append('u'); s.Append('d');
            s.Append('i'); s.Append('t'); s.Append('_'); s.Append('m'); s.Append('i'); s.Append('c'); s.Append('r'); s.Append('o');
            return s;
        }

        private static FixedString64Bytes CreateProjectileTrackingAuditScenarioId()
        {
            FixedString64Bytes s = default;
            s.Append('s'); s.Append('c'); s.Append('e'); s.Append('n'); s.Append('a'); s.Append('r'); s.Append('i'); s.Append('o');
            s.Append('.'); s.Append('p'); s.Append('u'); s.Append('r'); s.Append('e'); s.Append('d'); s.Append('o'); s.Append('t');
            s.Append('s'); s.Append('.'); s.Append('p'); s.Append('r'); s.Append('o'); s.Append('j'); s.Append('e'); s.Append('c');
            s.Append('t'); s.Append('i'); s.Append('l'); s.Append('e'); s.Append('_'); s.Append('t'); s.Append('r'); s.Append('a');
            s.Append('c'); s.Append('k'); s.Append('i'); s.Append('n'); s.Append('g'); s.Append('.'); s.Append('a'); s.Append('u');
            s.Append('d'); s.Append('i'); s.Append('t');
            return s;
        }

        private static FixedString64Bytes CreateProjectileTrackingAuditMetricKey()
        {
            FixedString64Bytes s = default;
            s.Append('p'); s.Append('u'); s.Append('r'); s.Append('e'); s.Append('d'); s.Append('o'); s.Append('t'); s.Append('s');
            s.Append('.'); s.Append('q'); s.Append('.'); s.Append('p'); s.Append('r'); s.Append('o'); s.Append('j'); s.Append('e');
            s.Append('c'); s.Append('t'); s.Append('i'); s.Append('l'); s.Append('e'); s.Append('_'); s.Append('t'); s.Append('r');
            s.Append('a'); s.Append('c'); s.Append('k'); s.Append('i'); s.Append('n'); s.Append('g'); s.Append('.'); s.Append('a');
            s.Append('u'); s.Append('d'); s.Append('i'); s.Append('t');
            return s;
        }

        private static FixedString64Bytes CreateProjectileLifecycleMicroId()
        {
            FixedString64Bytes s = default;
            s.Append('p'); s.Append('u'); s.Append('r'); s.Append('e'); s.Append('d'); s.Append('o'); s.Append('t'); s.Append('s');
            s.Append('_'); s.Append('p'); s.Append('r'); s.Append('o'); s.Append('j'); s.Append('e'); s.Append('c'); s.Append('t');
            s.Append('i'); s.Append('l'); s.Append('e'); s.Append('_'); s.Append('l'); s.Append('i'); s.Append('f'); s.Append('e');
            s.Append('c'); s.Append('y'); s.Append('c'); s.Append('l'); s.Append('e'); s.Append('_'); s.Append('m'); s.Append('i');
            s.Append('c'); s.Append('r'); s.Append('o');
            return s;
        }

        private static FixedString64Bytes CreateProjectileLifecycleScenarioId()
        {
            FixedString64Bytes s = default;
            s.Append('s'); s.Append('c'); s.Append('e'); s.Append('n'); s.Append('a'); s.Append('r'); s.Append('i'); s.Append('o');
            s.Append('.'); s.Append('p'); s.Append('u'); s.Append('r'); s.Append('e'); s.Append('d'); s.Append('o'); s.Append('t');
            s.Append('s'); s.Append('.'); s.Append('p'); s.Append('r'); s.Append('o'); s.Append('j'); s.Append('e'); s.Append('c');
            s.Append('t'); s.Append('i'); s.Append('l'); s.Append('e'); s.Append('_'); s.Append('l'); s.Append('i'); s.Append('f');
            s.Append('e'); s.Append('c'); s.Append('y'); s.Append('c'); s.Append('l'); s.Append('e'); s.Append('.'); s.Append('a');
            s.Append('u'); s.Append('d'); s.Append('i'); s.Append('t');
            return s;
        }

        private static FixedString64Bytes CreateProjectileLifecycleMetricKey()
        {
            FixedString64Bytes s = default;
            s.Append('p'); s.Append('u'); s.Append('r'); s.Append('e'); s.Append('d'); s.Append('o'); s.Append('t'); s.Append('s');
            s.Append('.'); s.Append('q'); s.Append('.'); s.Append('p'); s.Append('r'); s.Append('o'); s.Append('j'); s.Append('e');
            s.Append('c'); s.Append('t'); s.Append('i'); s.Append('l'); s.Append('e'); s.Append('_'); s.Append('l'); s.Append('i');
            s.Append('f'); s.Append('e'); s.Append('c'); s.Append('y'); s.Append('c'); s.Append('l'); s.Append('e'); s.Append('.');
            s.Append('a'); s.Append('u'); s.Append('d'); s.Append('i'); s.Append('t');
            return s;
        }

        private static FixedString64Bytes CreateDeliveriesCountKey()
        {
            FixedString64Bytes s = default;
            s.Append('d'); s.Append('e'); s.Append('l'); s.Append('i'); s.Append('v'); s.Append('e'); s.Append('r'); s.Append('i');
            s.Append('e'); s.Append('s'); s.Append('.'); s.Append('c'); s.Append('o'); s.Append('u'); s.Append('n'); s.Append('t');
            return s;
        }

        private static FixedString64Bytes CreateStorehouseInventoryKey()
        {
            FixedString64Bytes s = default;
            s.Append('s'); s.Append('t'); s.Append('o'); s.Append('r'); s.Append('e'); s.Append('h'); s.Append('o'); s.Append('u');
            s.Append('s'); s.Append('e'); s.Append('.'); s.Append('i'); s.Append('n'); s.Append('v'); s.Append('e'); s.Append('n');
            s.Append('t'); s.Append('o'); s.Append('r'); s.Append('y');
            return s;
        }

        private static FixedString64Bytes CreateConstraintsRespectedKey()
        {
            FixedString64Bytes s = default;
            s.Append('c'); s.Append('o'); s.Append('n'); s.Append('s'); s.Append('t'); s.Append('r'); s.Append('a'); s.Append('i');
            s.Append('n'); s.Append('t'); s.Append('s'); s.Append('.'); s.Append('r'); s.Append('e'); s.Append('s'); s.Append('p');
            s.Append('e'); s.Append('c'); s.Append('t'); s.Append('e'); s.Append('d');
            return s;
        }

        private static FixedString64Bytes CreateDeterministicReplayKey()
        {
            FixedString64Bytes s = default;
            s.Append('d'); s.Append('e'); s.Append('t'); s.Append('e'); s.Append('r'); s.Append('m'); s.Append('i'); s.Append('n');
            s.Append('i'); s.Append('s'); s.Append('t'); s.Append('i'); s.Append('c'); s.Append('.'); s.Append('r'); s.Append('e');
            s.Append('p'); s.Append('l'); s.Append('a'); s.Append('y');
            return s;
        }
        private EntityQuery _villagerQuery;
        private EntityQuery _shipyardQuery;
        private EntityQuery _weaponMountQuery;
        private EntityQuery _weaponSpawnerQuery;
        private EntityQuery _ammoStockpileQuery;
        private EntityQuery _weaponMagazineQuery;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ScenarioInfo>();

            _villagerCountKey = CreateVillagerCountKey();
            _shipyardCountKey = CreateShipyardCountKey();
            _shipyardRequestsPendingKey = CreateShipyardRequestsPendingKey();
            _weaponMountCountKey = CreateWeaponMountCountKey();
            _weaponSpawnerCountKey = CreateWeaponSpawnerCountKey();
            _shipyardEquipSuccessKey = CreateShipyardEquipSuccessKey();
            _shipyardEquipScenarioId = CreateShipyardEquipScenarioId();
            _shipyardEquipMetricKey = CreateShipyardEquipMetricKey();
            _ammoStockpileCountKey = CreateAmmoStockpileCountKey();
            _ammoMagazineCountKey = CreateAmmoMagazineCountKey();
            _ammoStockpileCurrentTotalKey = CreateAmmoStockpileCurrentTotalKey();
            _ammoStockpileCapacityTotalKey = CreateAmmoStockpileCapacityTotalKey();
            _ammoMagazineCurrentTotalKey = CreateAmmoMagazineCurrentTotalKey();
            _ammoMagazineCapacityTotalKey = CreateAmmoMagazineCapacityTotalKey();
            _projectileTrackingSpawnedTotalKey = CreateProjectileTrackingSpawnedTotalKey();
            _projectileTrackingHitsTotalKey = CreateProjectileTrackingHitsTotalKey();
            _projectileTrackingDeflectTotalKey = CreateProjectileTrackingDeflectTotalKey();
            _projectileTrackingRedirectTotalKey = CreateProjectileTrackingRedirectTotalKey();
            _projectileTrackingControlTotalKey = CreateProjectileTrackingControlTotalKey();
            _projectileTrackingRetireTotalKey = CreateProjectileTrackingRetireTotalKey();
            _projectileTrackingExpireTotalKey = CreateProjectileTrackingExpireTotalKey();
            _projectileTrackingRecycleTotalKey = CreateProjectileTrackingRecycleTotalKey();
            _projectileTrackingEventsCountKey = CreateProjectileTrackingEventsCountKey();
            _projectileTrackingSpawnedPrefix = CreateProjectileTrackingSpawnedPrefix();
            _projectileTrackingHitsPrefix = CreateProjectileTrackingHitsPrefix();
            _projectileTrackingDeflectPrefix = CreateProjectileTrackingDeflectPrefix();
            _projectileTrackingRedirectPrefix = CreateProjectileTrackingRedirectPrefix();
            _projectileTrackingControlPrefix = CreateProjectileTrackingControlPrefix();
            _projectileTrackingRetirePrefix = CreateProjectileTrackingRetirePrefix();
            _projectileTrackingExpirePrefix = CreateProjectileTrackingExpirePrefix();
            _projectileTrackingRecyclePrefix = CreateProjectileTrackingRecyclePrefix();
            _projectileTrackingAuditMicroId = CreateProjectileTrackingAuditMicroId();
            _projectileTrackingAuditScenarioId = CreateProjectileTrackingAuditScenarioId();
            _projectileTrackingAuditMetricKey = CreateProjectileTrackingAuditMetricKey();
            _projectileLifecycleMicroId = CreateProjectileLifecycleMicroId();
            _projectileLifecycleScenarioId = CreateProjectileLifecycleScenarioId();
            _projectileLifecycleMetricKey = CreateProjectileLifecycleMetricKey();
            _deliveriesCountKey = CreateDeliveriesCountKey();
            _storehouseInventoryKey = CreateStorehouseInventoryKey();
            _constraintsRespectedKey = CreateConstraintsRespectedKey();
            _deterministicReplayKey = CreateDeterministicReplayKey();

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
            ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, _villagerCountKey, _villagerQuery.CalculateEntityCount());

            // Shipyard count and pending equip requests.
            ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, _shipyardCountKey, _shipyardQuery.CalculateEntityCount());

            double pendingRequests = 0;
            foreach (var requests in SystemAPI.Query<DynamicBuffer<ShipyardEquipRequest>>().WithAll<Shipyard>())
            {
                pendingRequests += requests.Length;
            }
            ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, _shipyardRequestsPendingKey, pendingRequests);

            // Weapon counts (installed mounts/spawners).
            var mountCount = _weaponMountQuery.CalculateEntityCount();
            var spawnerCount = _weaponSpawnerQuery.CalculateEntityCount();
            ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, _weaponMountCountKey, mountCount);
            ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, _weaponSpawnerCountKey, spawnerCount);

            var equipSuccess = (mountCount + spawnerCount) > 0 ? 1.0 : 0.0;
            ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, _shipyardEquipSuccessKey, equipSuccess);
            if (scenarioInfo.ScenarioId.Equals(_shipyardEquipScenarioId))
            {
                ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, _shipyardEquipMetricKey, equipSuccess);
            }

            // Ammo totals (stockpiles and magazines).
            ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, _ammoStockpileCountKey, _ammoStockpileQuery.CalculateEntityCount());
            ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, _ammoMagazineCountKey, _weaponMagazineQuery.CalculateEntityCount());

            double stockpileCurrent = 0;
            double stockpileCapacity = 0;
            foreach (var stockpile in SystemAPI.Query<RefRO<AmmoStockpile>>())
            {
                stockpileCurrent += stockpile.ValueRO.Current;
                stockpileCapacity += stockpile.ValueRO.Capacity;
            }
            ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, _ammoStockpileCurrentTotalKey, stockpileCurrent);
            ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, _ammoStockpileCapacityTotalKey, stockpileCapacity);

            double magazineCurrent = 0;
            double magazineCapacity = 0;
            foreach (var magazine in SystemAPI.Query<RefRO<WeaponMagazine>>())
            {
                magazineCurrent += magazine.ValueRO.Current;
                magazineCapacity += magazine.ValueRO.Capacity;
            }
            ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, _ammoMagazineCurrentTotalKey, magazineCurrent);
            ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, _ammoMagazineCapacityTotalKey, magazineCapacity);

            // Projectile tracking counters (audit-friendly totals).
            if (SystemAPI.TryGetSingletonEntity<ProjectileTrackingHub>(out var trackingHubEntity))
            {
                var counters = SystemAPI.GetComponent<ProjectileTrackingCounters>(trackingHubEntity);
                ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, _projectileTrackingSpawnedTotalKey, counters.Spawned);
                ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, _projectileTrackingHitsTotalKey, counters.Hits);
                ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, _projectileTrackingDeflectTotalKey, counters.Deflections);
                ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, _projectileTrackingRedirectTotalKey, counters.Redirects);
                ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, _projectileTrackingControlTotalKey, counters.Controls);
                ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, _projectileTrackingRetireTotalKey, counters.Retired);
                ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, _projectileTrackingExpireTotalKey, counters.Expired);
                ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, _projectileTrackingRecycleTotalKey, counters.Recycled);

                if (SystemAPI.HasBuffer<ProjectileTrackingEvent>(trackingHubEntity))
                {
                    var events = SystemAPI.GetBuffer<ProjectileTrackingEvent>(trackingHubEntity);
                    ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, _projectileTrackingEventsCountKey, events.Length);
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

                        var spawnedKey = _projectileTrackingSpawnedPrefix;
                        spawnedKey.Append(entry.AmmoId);
                        ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, spawnedKey, entry.Spawned);

                        var hitKey = _projectileTrackingHitsPrefix;
                        hitKey.Append(entry.AmmoId);
                        ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, hitKey, entry.Hits);

                        var deflectKey = _projectileTrackingDeflectPrefix;
                        deflectKey.Append(entry.AmmoId);
                        ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, deflectKey, entry.Deflections);

                        var redirectKey = _projectileTrackingRedirectPrefix;
                        redirectKey.Append(entry.AmmoId);
                        ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, redirectKey, entry.Redirects);

                        var controlKey = _projectileTrackingControlPrefix;
                        controlKey.Append(entry.AmmoId);
                        ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, controlKey, entry.Controls);

                        var retireKey = _projectileTrackingRetirePrefix;
                        retireKey.Append(entry.AmmoId);
                        ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, retireKey, entry.Retired);

                        var expireKey = _projectileTrackingExpirePrefix;
                        expireKey.Append(entry.AmmoId);
                        ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, expireKey, entry.Expired);

                        var recycleKey = _projectileTrackingRecyclePrefix;
                        recycleKey.Append(entry.AmmoId);
                        ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, recycleKey, entry.Recycled);
                    }
                }

                if (scenarioInfo.ScenarioId.Equals(_projectileTrackingAuditMicroId) || scenarioInfo.ScenarioId.Equals(_projectileTrackingAuditScenarioId))
                {
                    var auditPass = (counters.Spawned > 0 && counters.Hits > 0) ? 1.0 : 0.0;
                    ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, _projectileTrackingAuditMetricKey, auditPass);
                }

                if (scenarioInfo.ScenarioId.Equals(_projectileLifecycleMicroId) || scenarioInfo.ScenarioId.Equals(_projectileLifecycleScenarioId))
                {
                    var lifecyclePass = (counters.Retired > 0 && counters.Expired > 0 && counters.Recycled > 0) ? 1.0 : 0.0;
                    ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, _projectileLifecycleMetricKey, lifecyclePass);
                }
            }

            // Completed deliveries (DeliveryReceipt buffers).
            double totalDeliveries = 0;
            foreach (var receipts in SystemAPI.Query<DynamicBuffer<DeliveryReceipt>>())
            {
                totalDeliveries += receipts.Length;
            }
            ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, _deliveriesCountKey, totalDeliveries);

            // Total storehouse inventory across all storehouses.
            double totalInventory = 0;
            foreach (var inventory in SystemAPI.Query<DynamicBuffer<StorehouseInventoryItem>>())
            {
                for (int i = 0; i < inventory.Length; i++)
                {
                    totalInventory += inventory[i].Amount;
                }
            }
            ScenarioMetricsUtility.SetMetric(ref metricLookup, scenarioEntity, _storehouseInventoryKey, totalInventory);

            // Defaults for boolean metrics – systems can override when violations occur.
            ScenarioMetricsUtility.SetMetricIfUnset(ref metricLookup, scenarioEntity, _constraintsRespectedKey, 1.0);
            ScenarioMetricsUtility.SetMetricIfUnset(ref metricLookup, scenarioEntity, _deterministicReplayKey, 1.0);
        }
    }
}
