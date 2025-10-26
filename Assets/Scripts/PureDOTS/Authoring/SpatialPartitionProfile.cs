using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using PureDOTS.Runtime.Spatial;

namespace PureDOTS.Authoring
{
    public enum SpatialProviderType : byte
    {
        UniformGrid = 0,
        HashedGrid = 1
    }

    [CreateAssetMenu(fileName = "SpatialPartitionProfile", menuName = "PureDOTS/Spatial Partition Profile", order = 20)]
    public sealed class SpatialPartitionProfile : ScriptableObject
    {
        public const int LatestSchemaVersion = 1;

        [SerializeField, HideInInspector]
        private int _schemaVersion = LatestSchemaVersion;

        [Header("World Bounds")]
        [SerializeField] private Vector3 _worldMin = new(-512f, -64f, -512f);
        [SerializeField] private Vector3 _worldMax = new(512f, 64f, 512f);

        [Header("Grid Settings")]
        [SerializeField] private float _cellSize = 4f;
        [SerializeField] private SpatialProviderType _provider = SpatialProviderType.HashedGrid;
        [SerializeField] private uint _hashSeed = 0;

        public Vector3 WorldMin => _worldMin;
        public Vector3 WorldMax => _worldMax;
        public float CellSize => _cellSize;
        public SpatialProviderType Provider => _provider;
        public uint HashSeed => _hashSeed;
        public int SchemaVersion => _schemaVersion;

        public SpatialGridConfig ToComponent()
        {
            var min = (float3)_worldMin;
            var max = (float3)_worldMax;
            max = math.max(max, min + new float3(1f));

            var extent = max - min;
            var safeCellSize = math.max(0.5f, _cellSize);
            var rawCounts = (int3)math.ceil(extent / safeCellSize);
            var cellCounts = math.max(rawCounts, new int3(1, 1, 1));

            return new SpatialGridConfig
            {
                CellSize = safeCellSize,
                WorldMin = min,
                WorldMax = max,
                CellCounts = cellCounts,
                HashSeed = _hashSeed,
                ProviderId = (byte)_provider
            };
        }

#if UNITY_EDITOR
        internal void SetSchemaVersion(int value)
        {
            _schemaVersion = value;
        }

        private void OnValidate()
        {
            _cellSize = Mathf.Max(0.5f, _cellSize);

            if (_worldMax.x <= _worldMin.x) _worldMax.x = _worldMin.x + 1f;
            if (_worldMax.y <= _worldMin.y) _worldMax.y = _worldMin.y + 1f;
            if (_worldMax.z <= _worldMin.z) _worldMax.z = _worldMin.z + 1f;
        }
#endif
    }

    [DisallowMultipleComponent]
    public sealed class SpatialPartitionAuthoring : MonoBehaviour
    {
        public SpatialPartitionProfile profile;
    }

    public sealed class SpatialPartitionBaker : Baker<SpatialPartitionAuthoring>
    {
        public override void Bake(SpatialPartitionAuthoring authoring)
        {
            if (authoring.profile == null)
            {
                Debug.LogWarning("SpatialPartitionAuthoring has no profile asset assigned.", authoring);
                return;
            }

            var entity = GetEntity(TransformUsageFlags.None);
            var config = authoring.profile.ToComponent();

            AddComponent(entity, config);

            AddComponent(entity, new SpatialGridState
            {
                ActiveBufferIndex = 0,
                TotalEntries = 0,
                Version = 0
            });

            AddBuffer<SpatialGridCellRange>(entity);
            AddBuffer<SpatialGridEntry>(entity);
            AddBuffer<SpatialGridStagingEntry>(entity);
            AddBuffer<SpatialGridStagingCellRange>(entity);
        }
    }
}
