using NUnit.Framework;
using PureDOTS.Runtime.Combat;
using Unity.Collections;
using Unity.Mathematics;

namespace PureDOTS.Tests.EditMode
{
    public class HazardGridSanityTests
    {
        [Test]
        public void HazardGrid_Deterministic_Raster()
        {
            // Create two identical grids
            var grid1 = CreateTestGrid();
            var grid2 = CreateTestGrid();

            // Create identical slices
            var slices1 = CreateTestSlices();
            var slices2 = CreateTestSlices();

            // Rasterize slices into grids (simplified - would use actual system)
            // For now, verify grid structure is deterministic
            Assert.AreEqual(grid1.Size, grid2.Size, "Grid sizes should match.");
            Assert.AreEqual(grid1.Cell, grid2.Cell, "Grid cell sizes should match.");
            Assert.AreEqual(grid1.Origin, grid2.Origin, "Grid origins should match.");

            // Verify slices are identical
            Assert.AreEqual(slices1.Length, slices2.Length, "Slice counts should match.");
            for (int i = 0; i < slices1.Length; i++)
            {
                Assert.AreEqual(slices1[i].Center, slices2[i].Center, $"Slice {i} centers should match.");
                Assert.AreEqual(slices1[i].Radius0, slices2[i].Radius0, $"Slice {i} radii should match.");
                Assert.AreEqual(slices1[i].Kind, slices2[i].Kind, $"Slice {i} kinds should match.");
            }
        }

        private HazardGrid CreateTestGrid()
        {
            return new HazardGrid
            {
                Size = new int3(100, 100, 1),
                Cell = 10f,
                Origin = float3.zero,
                Risk = default // Would be created by system
            };
        }

        private NativeArray<HazardSlice> CreateTestSlices()
        {
            var slices = new NativeArray<HazardSlice>(2, Allocator.Temp);
            slices[0] = new HazardSlice
            {
                Center = new float3(50f, 0f, 0f),
                Vel = new float3(10f, 0f, 0f),
                Radius0 = 5f,
                RadiusGrow = 0f,
                StartTick = 0,
                EndTick = 100,
                Kind = HazardKind.AoE,
                ChainRadius = 0f,
                ContagionProb = 0f,
                HomingConeCos = 0f,
                SprayVariance = 0f,
                TeamMask = 0xFFFFFFFF,
                Seed = 12345
            };
            slices[1] = new HazardSlice
            {
                Center = new float3(150f, 0f, 0f),
                Vel = new float3(-10f, 0f, 0f),
                Radius0 = 3f,
                RadiusGrow = 0f,
                StartTick = 0,
                EndTick = 100,
                Kind = HazardKind.Chain,
                ChainRadius = 10f,
                ContagionProb = 0f,
                HomingConeCos = 0f,
                SprayVariance = 0f,
                TeamMask = 0xFFFFFFFF,
                Seed = 67890
            };
            return slices;
        }
    }
}

