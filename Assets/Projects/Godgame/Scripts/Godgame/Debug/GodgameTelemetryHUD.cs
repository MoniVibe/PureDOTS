using System.Text;
using Godgame.Registry;
using PureDOTS.Runtime.Miracles;
using PureDOTS.Runtime.Telemetry;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace Godgame.Debugging
{
    /// <summary>
    /// Simple runtime bridge that reads <see cref="GodgameRegistrySnapshot"/> and <see cref="TelemetryStream"/>
    /// to display the metrics on a Unity UI Canvas. Intended for validating the Godgame registry bridge.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GodgameTelemetryHUD : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Text villagerSummaryText;
        [SerializeField] private Text storehouseSummaryText;
        [SerializeField] private Text miracleSummaryText;
        [SerializeField] private Text telemetrySummaryText;

        private World _world;
        private EntityManager _entityManager;
        private EntityQuery _snapshotQuery;
        private EntityQuery _telemetryQuery;
        private EntityQuery _chargeDisplayQuery;
        private uint _lastTelemetryVersion;
        private readonly StringBuilder _builder = new StringBuilder(256);

        private void Awake()
        {
            EnsureWorld();
        }

        private void OnEnable()
        {
            EnsureWorld();
        }

        private void OnDisable()
        {
            ResetWorld();
        }

        private void OnDestroy()
        {
            ResetWorld();
        }

        private void Update()
        {
            if (!EnsureWorld())
            {
                ClearText(villagerSummaryText);
                ClearText(storehouseSummaryText);
                ClearText(miracleSummaryText);
                ClearText(telemetrySummaryText);
                return;
            }

            UpdateSnapshotUI();
            UpdateTelemetryUI();
        }

        private bool EnsureWorld()
        {
            if (_world != null && _world.IsCreated)
            {
                return true;
            }

            _world = World.DefaultGameObjectInjectionWorld;
            if (_world == null || !_world.IsCreated)
            {
                return false;
            }

            _entityManager = _world.EntityManager;
            DisposeQueries();
            _snapshotQuery = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<GodgameRegistrySnapshot>());
            _telemetryQuery = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<TelemetryStream>());
            _chargeDisplayQuery = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<MiracleChargeDisplayData>());
            _lastTelemetryVersion = 0;
            return true;
        }

        private void ResetWorld()
        {
            DisposeQueries();
            _entityManager = default;
            _world = null;
        }

        private void DisposeQueries()
        {
            if (_snapshotQuery != default)
            {
                _snapshotQuery.Dispose();
            }

            if (_telemetryQuery != default)
            {
                _telemetryQuery.Dispose();
            }

            if (_chargeDisplayQuery != default)
            {
                _chargeDisplayQuery.Dispose();
            }

            _snapshotQuery = default;
            _telemetryQuery = default;
            _chargeDisplayQuery = default;
        }

        private void UpdateSnapshotUI()
        {
            if (_snapshotQuery == default || _snapshotQuery.IsEmptyIgnoreFilter)
            {
                ClearText(villagerSummaryText);
                ClearText(storehouseSummaryText);
                ClearText(miracleSummaryText);
                return;
            }

            var snapshot = _snapshotQuery.GetSingleton<GodgameRegistrySnapshot>();

            if (villagerSummaryText != null)
            {
                _builder.Clear();
                _builder.Append("Villagers  ");
                _builder.Append(snapshot.VillagerCount);
                _builder.Append(" (Avail:");
                _builder.Append(snapshot.AvailableVillagers);
                _builder.Append(" Idle:");
                _builder.Append(snapshot.IdleVillagers);
                _builder.Append(" Combat:");
                _builder.Append(snapshot.CombatReadyVillagers);
                _builder.Append(")  HP:");
                _builder.Append(snapshot.AverageVillagerHealth.ToString("0.0"));
                _builder.Append(" Morale:");
                _builder.Append(snapshot.AverageVillagerMorale.ToString("0.0"));
                _builder.Append(" Energy:");
                _builder.Append(snapshot.AverageVillagerEnergy.ToString("0.0"));
                villagerSummaryText.text = _builder.ToString();
            }

            if (storehouseSummaryText != null)
            {
                _builder.Clear();
                _builder.Append("Storehouses  ");
                _builder.Append(snapshot.StorehouseCount);
                _builder.Append("  Capacity:");
                _builder.Append(snapshot.TotalStorehouseCapacity.ToString("0"));
                _builder.Append("  Stored:");
                _builder.Append(snapshot.TotalStorehouseStored.ToString("0"));
                _builder.Append("  Reserved:");
                _builder.Append(snapshot.TotalStorehouseReserved.ToString("0"));
                storehouseSummaryText.text = _builder.ToString();
            }

            if (miracleSummaryText != null)
            {
                _builder.Clear();
                _builder.Append("Miracles  ");
                _builder.Append(snapshot.MiracleCount);
                _builder.Append("  Active:");
                _builder.Append(snapshot.ActiveMiracles);
                _builder.Append("  Sustained:");
                _builder.Append(snapshot.SustainedMiracles);
                _builder.Append("  Cooling:");
                _builder.Append(snapshot.CoolingMiracles);
                _builder.Append("  Energy:");
                _builder.Append(snapshot.TotalMiracleEnergyCost.ToString("0.0"));
                _builder.Append("  Cooldown:");
                _builder.Append(snapshot.TotalMiracleCooldownSeconds.ToString("0.0"));

                // Add charge display if available
                if (_chargeDisplayQuery != default && !_chargeDisplayQuery.IsEmptyIgnoreFilter)
                {
                    var chargeDisplay = _chargeDisplayQuery.GetSingleton<MiracleChargeDisplayData>();
                    if (chargeDisplay.IsCharging != 0)
                    {
                        _builder.Append("  Charge:");
                        _builder.Append(chargeDisplay.ChargePercent.ToString("0"));
                        _builder.Append("%");
                        if (chargeDisplay.CurrentTier > 0)
                        {
                            _builder.Append(" Tier:");
                            _builder.Append(chargeDisplay.CurrentTier);
                        }
                    }
                }

                miracleSummaryText.text = _builder.ToString();
            }
        }

        private void UpdateTelemetryUI()
        {
            if (telemetrySummaryText == null || _telemetryQuery == default || _telemetryQuery.IsEmptyIgnoreFilter)
            {
                ClearText(telemetrySummaryText);
                return;
            }

            var telemetryEntity = _telemetryQuery.GetSingletonEntity();
            var stream = _entityManager.GetComponentData<TelemetryStream>(telemetryEntity);
            if (stream.Version == _lastTelemetryVersion)
            {
                return;
            }

            _lastTelemetryVersion = stream.Version;
            var buffer = _entityManager.GetBuffer<TelemetryMetric>(telemetryEntity);
            if (buffer.Length == 0)
            {
                ClearText(telemetrySummaryText);
                return;
            }

            _builder.Clear();
            for (int i = 0; i < buffer.Length; i++)
            {
                var metric = buffer[i];
                _builder.Append(metric.Key.ToString());
                _builder.Append(':');
                _builder.Append(metric.Value.ToString("0.##"));
                if (i < buffer.Length - 1)
                {
                    _builder.Append("  |  ");
                }
            }

            telemetrySummaryText.text = _builder.ToString();
        }

        private static void ClearText(Text text)
        {
            if (text != null)
            {
                text.text = string.Empty;
            }
        }
    }
}
