using PureDOTS.Runtime.Spatial;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

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
        public const int LatestSchemaVersion = 2;

        [SerializeField, HideInInspector]
        private int _schemaVersion = LatestSchemaVersion;

        [Header("Bounds")]
        [SerializeField] private Vector3 _center = Vector3.zero;
        [SerializeField] private Vector3 _extent = new Vector3(512f, 128f, 512f);

        [Header("Cell Settings")]
        [SerializeField] private float _cellSize = 8f;
        [SerializeField] private float _minCellSize = 1f;
        [SerializeField] private bool _overrideCellCounts;
        [SerializeField] private Vector3Int _manualCellCounts = new Vector3Int(64, 1, 64);
        [SerializeField] private bool _lockYAxisToOne = true;

        [Header("Providers")]
        [SerializeField] private SpatialProviderType _providerType = SpatialProviderType.HashedGrid;
        [SerializeField] private uint _hashSeed = 0u;

        [Header("Gizmos")]
        [SerializeField] private bool _drawGizmo = true;
        [SerializeField] private Color _gizmoColor = new Color(0f, 0.75f, 1f, 0.15f);

        public bool DrawGizmo => _drawGizmo;
        public Color GizmoColor => _gizmoColor;
        public int SchemaVersion => _schemaVersion;
        public Vector3 WorldMin => _center - _extent;
        public Vector3 WorldMax => _center + _extent;
        public Vector3 Center => _center;
        public Vector3 Extent => _extent;
        public float CellSize => Mathf.Max(_minCellSize, _cellSize);
        public float MinCellSize => _minCellSize;
        public bool OverrideCellCounts => _overrideCellCounts;
        public Vector3Int ManualCellCounts => _manualCellCounts;
        public SpatialProviderType Provider => _providerType;
        public uint HashSeed => _hashSeed;

        private void OnValidate()
        {
            _extent.x = Mathf.Max(_minCellSize, _extent.x);
            _extent.y = Mathf.Max(_minCellSize, _extent.y);
            _extent.z = Mathf.Max(_minCellSize, _extent.z);
            _cellSize = Mathf.Max(_minCellSize, _cellSize);

            _manualCellCounts = SanitizeCellCounts(_manualCellCounts);

            if (_lockYAxisToOne)
            {
                _manualCellCounts.y = 1;
            }

            if (_schemaVersion != LatestSchemaVersion)
            {
                UpgradeSchema(_schemaVersion, LatestSchemaVersion);
            }
        }

        private void UpgradeSchema(int previousVersion, int targetVersion)
        {
            if (previousVersion < 1)
            {
                _minCellSize = 1f;
            }

            if (previousVersion < 2)
            {
                _providerType = SpatialProviderType.HashedGrid;
            }

            _schemaVersion = targetVersion;
        }

        public Bounds ToBounds()
        {
            return new Bounds(_center, _extent * 2f);
        }

        public SpatialGridConfig ToComponent()
        {
            var safeCellSize = math.max(_cellSize, _minCellSize);
            var counts = CalculateCellCounts((float3)(_extent * 2f), safeCellSize);
            var config = new SpatialGridConfig
            {
                WorldMin = (float3)WorldMin,
                WorldMax = (float3)WorldMax,
                CellSize = safeCellSize,
                CellCounts = counts,
                HashSeed = _hashSeed
            };

            switch (_providerType)
            {
                case SpatialProviderType.HashedGrid:
                    config.ProviderId = SpatialGridProviderIds.Hashed;
                    break;
                case SpatialProviderType.UniformGrid:
                    config.ProviderId = SpatialGridProviderIds.Uniform;
                    break;
                default:
                    config.ProviderId = SpatialGridProviderIds.Hashed;
                    break;
            }

            return config;
        }

        public void SetWorldBounds(Vector3 center, Vector3 extent)
        {
            _center = center;
            _extent = new Vector3(
                Mathf.Max(_minCellSize, Mathf.Abs(extent.x)),
                Mathf.Max(_minCellSize, Mathf.Abs(extent.y)),
                Mathf.Max(_minCellSize, Mathf.Abs(extent.z)));
        }

        public void SetCellSize(float value)
        {
            _cellSize = Mathf.Max(_minCellSize, value);
        }

        public void SetManualCellCounts(Vector3Int counts)
        {
            _manualCellCounts = SanitizeCellCounts(counts);
        }

        public void SetOverrideCellCounts(bool enabled)
        {
            _overrideCellCounts = enabled;
        }

        public void SetLockYAxisToOne(bool enabled)
        {
            _lockYAxisToOne = enabled;
        }

        public void SetProviderType(SpatialProviderType providerType)
        {
            _providerType = providerType;
        }

        public void SetMinCellSize(float value)
        {
            _minCellSize = Mathf.Max(0.001f, value);
            _cellSize = Mathf.Max(_minCellSize, _cellSize);
        }

        private int3 CalculateCellCounts(float3 extent, float safeCellSize)
        {
            if (_overrideCellCounts)
            {
                var counts = SanitizeCellCounts(_manualCellCounts);
                if (_lockYAxisToOne)
                {
                    counts.y = 1;
                }

                return new int3(counts.x, counts.y, counts.z);
            }

            var rawCounts = (int3)math.ceil(extent / safeCellSize);
            rawCounts = math.max(rawCounts, new int3(1, 1, 1));

            if (_lockYAxisToOne)
            {
                rawCounts.y = 1;
            }

            return rawCounts;
        }

        private Vector3Int SanitizeCellCounts(Vector3Int counts)
        {
            counts.x = Mathf.Max(1, counts.x);
            counts.y = Mathf.Max(1, counts.y);
            counts.z = Mathf.Max(1, counts.z);
            return counts;
        }
    }

    [DisallowMultipleComponent]
    public sealed class SpatialPartitionAuthoring : MonoBehaviour
    {
        public SpatialPartitionProfile profile;

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (profile == null || !profile.DrawGizmo)
            {
                return;
            }

            var bounds = profile.ToBounds();
            Gizmos.color = profile.GizmoColor;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
            Gizmos.color = new Color(profile.GizmoColor.r, profile.GizmoColor.g, profile.GizmoColor.b, Mathf.Clamp01(profile.GizmoColor.a));
            Gizmos.DrawCube(bounds.center, bounds.size);
        }
#endif
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

            var config = authoring.profile.ToComponent();
            var state = CreateDefaultState();

            if (World.DefaultGameObjectInjectionWorld != null)
            {
                var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
                if (entityManager.World.IsCreated)
                {
                    using var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<SpatialGridConfig>());
                    if (!query.IsEmptyIgnoreFilter)
                    {
                        var existingEntity = query.GetSingletonEntity();
                        entityManager.SetComponentData(existingEntity, config);
                        if (entityManager.HasComponent<SpatialGridState>(existingEntity))
                        {
                            entityManager.SetComponentData(existingEntity, state);
                        }
                        else
                        {
                            entityManager.AddComponentData(existingEntity, state);
                        }

                        EnsureBuffer<SpatialGridCellRange>(entityManager, existingEntity);
                        EnsureBuffer<SpatialGridEntry>(entityManager, existingEntity);
                        EnsureBuffer<SpatialGridStagingEntry>(entityManager, existingEntity);
                        EnsureBuffer<SpatialGridStagingCellRange>(entityManager, existingEntity);
                        EnsureBuffer<SpatialGridEntryLookup>(entityManager, existingEntity);
                        EnsureBuffer<SpatialGridDirtyOp>(entityManager, existingEntity);
                        return;
                    }
                }
            }

            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, config);
            AddComponent(entity, state);
            AddBuffer<SpatialGridCellRange>(entity);
            AddBuffer<SpatialGridEntry>(entity);
            AddBuffer<SpatialGridStagingEntry>(entity);
            AddBuffer<SpatialGridStagingCellRange>(entity);
            AddBuffer<SpatialGridEntryLookup>(entity);
            AddBuffer<SpatialGridDirtyOp>(entity);
        }

        private static SpatialGridState CreateDefaultState()
        {
            return new SpatialGridState
            {
                ActiveBufferIndex = 0,
                TotalEntries = 0,
                Version = 0,
                LastUpdateTick = 0,
                LastDirtyTick = 0,
                DirtyVersion = 0,
                DirtyAddCount = 0,
                DirtyUpdateCount = 0,
                DirtyRemoveCount = 0,
                LastRebuildMilliseconds = 0f,
                LastStrategy = SpatialGridRebuildStrategy.None
            };
        }

        private static void EnsureBuffer<T>(EntityManager entityManager, Entity entity) where T : unmanaged, IBufferElementData
        {
            if (!entityManager.HasBuffer<T>(entity))
            {
                entityManager.AddBuffer<T>(entity);
            }
        }
    }
}
