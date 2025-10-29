using NUnit.Framework;
using PureDOTS.Authoring;
using PureDOTS.Runtime.Spatial;
using Unity.Mathematics;
using UnityEngine;

namespace PureDOTS.Tests.EditMode
{
    public class SpatialPartitionProfileTests
    {
        [Test]
        public void ToComponent_RespectOverrides_WhenEnabled()
        {
            var profile = ScriptableObject.CreateInstance<SpatialPartitionProfile>();

            profile.SetWorldBounds(Vector3.zero, new Vector3(256f, 32f, 256f));
            profile.SetCellSize(4f);
            profile.SetManualCellCounts(new Vector3Int(32, 4, 32));
            profile.SetOverrideCellCounts(true);
            profile.SetLockYAxisToOne(false);

            var config = profile.ToComponent();

            Assert.AreEqual(new int3(32, 4, 32), config.CellCounts);
            Assert.AreEqual((byte)SpatialProviderType.HashedGrid, config.ProviderId);

            Object.DestroyImmediate(profile);
        }

        [Test]
        public void ToComponent_LocksYAxis_WhenRequested()
        {
            var profile = ScriptableObject.CreateInstance<SpatialPartitionProfile>();

            profile.SetWorldBounds(new Vector3(-10f, -5f, -10f), new Vector3(10f, 5f, 10f));
            profile.SetCellSize(1f);
            profile.SetOverrideCellCounts(false);
            profile.SetLockYAxisToOne(true);

            var config = profile.ToComponent();

            Assert.AreEqual(1, config.CellCounts.y);
            Assert.Greater(config.CellCounts.x, 0);
            Assert.Greater(config.CellCounts.z, 0);

            Object.DestroyImmediate(profile);
        }

        [Test]
        public void ToComponent_ExpandsBounds_WhenMaxLessThanMin()
        {
            var profile = ScriptableObject.CreateInstance<SpatialPartitionProfile>();

            profile.SetWorldBounds(new Vector3(5f, 5f, 5f), new Vector3(4f, 4f, 4f));
            profile.SetCellSize(2f);
            profile.SetOverrideCellCounts(false);
            profile.SetLockYAxisToOne(true);

            var config = profile.ToComponent();

            var extent = config.WorldMax - config.WorldMin;
            Assert.GreaterOrEqual(extent.x, 1f);
            Assert.GreaterOrEqual(extent.y, 1f);
            Assert.GreaterOrEqual(extent.z, 1f);

            Object.DestroyImmediate(profile);
        }
    }
}


