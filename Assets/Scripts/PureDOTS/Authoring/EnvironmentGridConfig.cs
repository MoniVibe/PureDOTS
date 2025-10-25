using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using PureDOTS.Environment;

namespace PureDOTS.Authoring
{
    [CreateAssetMenu(fileName = "EnvironmentGridConfig", menuName = "PureDOTS/Environment/Grid Config", order = 5)]
    public sealed class EnvironmentGridConfig : ScriptableObject
    {
        [Header("Grid Settings")]
        [SerializeField] GridSettings _moisture = GridSettings.CreateDefault(new Vector2Int(256, 256), 5f);
        [SerializeField] GridSettings _temperature = GridSettings.CreateDefault(new Vector2Int(128, 128), 10f);
        [SerializeField] GridSettings _sunlight = GridSettings.CreateDefault(new Vector2Int(128, 128), 10f);
        [SerializeField] GridSettings _wind = GridSettings.CreateDefault(new Vector2Int(64, 64), 20f);
        [SerializeField] GridSettings _biome = GridSettings.CreateDefault(new Vector2Int(128, 128), 10f, enabled: false);

        [Header("Channel Identifiers")]
        [SerializeField] string _moistureChannelId = "moisture";
        [SerializeField] string _temperatureChannelId = "temperature";
        [SerializeField] string _sunlightChannelId = "sunlight";
        [SerializeField] string _windChannelId = "wind";
        [SerializeField] string _biomeChannelId = "biome";

        [Header("Moisture Defaults")]
        [SerializeField, Min(0f)] float _moistureDiffusion = 0.25f;
        [SerializeField, Min(0f)] float _moistureSeepage = 0.1f;

        [Header("Temperature Defaults")]
        [SerializeField] float _baseSeasonTemperature = 18f;
        [SerializeField, Min(0f)] float _timeOfDaySwing = 6f;
        [SerializeField, Min(0f)] float _seasonalSwing = 12f;

        [Header("Sunlight Defaults")]
        [SerializeField] Vector3 _sunDirection = new Vector3(0.25f, -0.9f, 0.35f);
        [SerializeField, Min(0f)] float _sunIntensity = 1f;

        [Header("Wind Defaults")]
        [SerializeField] Vector2 _globalWindDirection = new Vector2(0.7f, 0.5f);
        [SerializeField, Min(0f)] float _globalWindStrength = 8f;

        public GridSettings Moisture => _moisture;
        public GridSettings Temperature => _temperature;
        public GridSettings Sunlight => _sunlight;
        public GridSettings Wind => _wind;
        public GridSettings Biome => _biome;

        public EnvironmentGridConfigData ToComponent()
        {
            return new EnvironmentGridConfigData
            {
                Moisture = _moisture.ToMetadata(),
                Temperature = _temperature.ToMetadata(),
                Sunlight = _sunlight.ToMetadata(),
                Wind = _wind.ToMetadata(),
                Biome = _biome.ToMetadata(),
                BiomeEnabled = _biome.Enabled ? (byte)1 : (byte)0,
                MoistureChannelId = ToFixedString(_moistureChannelId, "moisture"),
                TemperatureChannelId = ToFixedString(_temperatureChannelId, "temperature"),
                SunlightChannelId = ToFixedString(_sunlightChannelId, "sunlight"),
                WindChannelId = ToFixedString(_windChannelId, "wind"),
                BiomeChannelId = ToFixedString(_biomeChannelId, "biome"),
                MoistureDiffusion = math.max(0f, _moistureDiffusion),
                MoistureSeepage = math.max(0f, _moistureSeepage),
                BaseSeasonTemperature = _baseSeasonTemperature,
                TimeOfDaySwing = math.max(0f, _timeOfDaySwing),
                SeasonalSwing = math.max(0f, _seasonalSwing),
                DefaultSunDirection = NormalizeSunDirection(_sunDirection),
                DefaultSunIntensity = math.max(0f, _sunIntensity),
                DefaultWindDirection = NormalizeWindDirection(_globalWindDirection),
                DefaultWindStrength = math.max(0f, _globalWindStrength)
            };
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _moisture.Sanitize();
            _temperature.Sanitize();
            _sunlight.Sanitize();
            _wind.Sanitize();
            _biome.Sanitize();

            _moistureChannelId = SanitizeChannel(_moistureChannelId, "moisture");
            _temperatureChannelId = SanitizeChannel(_temperatureChannelId, "temperature");
            _sunlightChannelId = SanitizeChannel(_sunlightChannelId, "sunlight");
            _windChannelId = SanitizeChannel(_windChannelId, "wind");
            _biomeChannelId = SanitizeChannel(_biomeChannelId, "biome");
        }
#endif

        static FixedString64Bytes ToFixedString(string value, string fallback)
        {
            var text = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
            FixedString64Bytes fixedString = text;
            return fixedString;
        }

        static string SanitizeChannel(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }

        static float3 NormalizeSunDirection(Vector3 direction)
        {
            var dir = (float3)direction;
            if (math.lengthsq(dir) < 1e-6f)
            {
                dir = new float3(0f, -1f, 0f);
            }
            return math.normalize(dir);
        }

        static float2 NormalizeWindDirection(Vector2 direction)
        {
            var dir = (float2)direction;
            if (math.lengthsq(dir) < 1e-6f)
            {
                dir = new float2(0f, 1f);
            }
            return math.normalize(dir);
        }

        [Serializable]
        public struct GridSettings
        {
            [SerializeField] Vector2Int _resolution;
            [SerializeField, Min(0.1f)] float _cellSize;
            [SerializeField] Vector3 _worldMin;
            [SerializeField] Vector3 _worldMax;
            [SerializeField] bool _enabled;

            public bool Enabled => _enabled;

            public static GridSettings CreateDefault(Vector2Int resolution, float cellSize, bool enabled = true)
            {
                return new GridSettings
                {
                    _resolution = new Vector2Int(math.max(1, resolution.x), math.max(1, resolution.y)),
                    _cellSize = math.max(0.1f, cellSize),
                    _worldMin = new Vector3(-512f, 0f, -512f),
                    _worldMax = new Vector3(512f, 256f, 512f),
                    _enabled = enabled
                };
            }

            public EnvironmentGridMetadata ToMetadata()
            {
                var min = (float3)_worldMin;
                var max = (float3)_worldMax;
                var safeCellSize = math.max(0.1f, _cellSize);
                max = math.max(max, min + new float3(safeCellSize, 0.01f, safeCellSize));

                var resolution = new int2(math.max(1, _resolution.x), math.max(1, _resolution.y));
                return EnvironmentGridMetadata.Create(min, max, safeCellSize, resolution);
            }

            public void Sanitize()
            {
                _resolution.x = math.max(1, _resolution.x);
                _resolution.y = math.max(1, _resolution.y);
                _cellSize = math.max(0.1f, _cellSize);

                if (_worldMax.x <= _worldMin.x)
                {
                    _worldMax.x = _worldMin.x + _cellSize;
                }

                if (_worldMax.y <= _worldMin.y)
                {
                    _worldMax.y = _worldMin.y + 0.01f;
                }

                if (_worldMax.z <= _worldMin.z)
                {
                    _worldMax.z = _worldMin.z + _cellSize;
                }
            }
        }
    }

    [DisallowMultipleComponent]
    public sealed class EnvironmentGridConfigAuthoring : MonoBehaviour
    {
        public EnvironmentGridConfig config;
    }

    public sealed class EnvironmentGridConfigBaker : Baker<EnvironmentGridConfigAuthoring>
    {
        public override void Bake(EnvironmentGridConfigAuthoring authoring)
        {
            if (authoring.config == null)
            {
                Debug.LogWarning("EnvironmentGridConfigAuthoring has no EnvironmentGridConfig assigned.", authoring);
                return;
            }

            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, authoring.config.ToComponent());
        }
    }
}
