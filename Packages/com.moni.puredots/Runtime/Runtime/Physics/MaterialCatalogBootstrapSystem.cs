using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.Physics
{
    /// <summary>
    /// Bootstraps the MaterialCatalog singleton with default material specifications.
    /// Creates a default catalog with common material categories if none exists.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    public partial struct MaterialCatalogBootstrapSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            EnsureCatalog(ref state);
            state.Enabled = false; // Only run once
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // No-op after initial bootstrap
        }

        [BurstCompile]
        private static void EnsureCatalog(ref SystemState state)
        {
            var query = state.EntityManager.CreateEntityQuery(typeof(MaterialCatalog));
            if (query.CalculateEntityCount() > 0)
            {
                query.Dispose();
                return;
            }
            query.Dispose();

            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<MaterialCatalogBlob>();

            // Create default materials
            var materials = new NativeList<MaterialSpec>(16, Allocator.Temp);

            // Metals
            materials.Add(new MaterialSpec
            {
                MaterialId = new FixedString64Bytes("iron"),
                Name = new FixedString64Bytes("Iron"),
                Category = MaterialCategory.Metal,
                Density = 7870f, // kg/m³
                YoungsModulus = 200e9f, // Pa (200 GPa)
                YieldStrength = 250e6f, // Pa (250 MPa)
                Flexibility = 0.1f,
                HeatCapacity = 449f // J/(kg·K)
            });

            materials.Add(new MaterialSpec
            {
                MaterialId = new FixedString64Bytes("steel"),
                Name = new FixedString64Bytes("Steel"),
                Category = MaterialCategory.Metal,
                Density = 7850f,
                YoungsModulus = 210e9f, // Pa
                YieldStrength = 400e6f, // Pa
                Flexibility = 0.05f,
                HeatCapacity = 500f
            });

            materials.Add(new MaterialSpec
            {
                MaterialId = new FixedString64Bytes("aluminum"),
                Name = new FixedString64Bytes("Aluminum"),
                Category = MaterialCategory.Metal,
                Density = 2700f,
                YoungsModulus = 70e9f, // Pa
                YieldStrength = 275e6f, // Pa
                Flexibility = 0.15f,
                HeatCapacity = 900f
            });

            // Alloys
            materials.Add(new MaterialSpec
            {
                MaterialId = new FixedString64Bytes("titanium_alloy"),
                Name = new FixedString64Bytes("Titanium Alloy"),
                Category = MaterialCategory.Alloy,
                Density = 4500f,
                YoungsModulus = 110e9f, // Pa
                YieldStrength = 900e6f, // Pa
                Flexibility = 0.08f,
                HeatCapacity = 520f
            });

            materials.Add(new MaterialSpec
            {
                MaterialId = new FixedString64Bytes("carbon_fiber"),
                Name = new FixedString64Bytes("Carbon Fiber"),
                Category = MaterialCategory.Composite,
                Density = 1600f,
                YoungsModulus = 230e9f, // Pa
                YieldStrength = 600e6f, // Pa
                Flexibility = 0.2f,
                HeatCapacity = 710f
            });

            // Organic materials
            materials.Add(new MaterialSpec
            {
                MaterialId = new FixedString64Bytes("wood"),
                Name = new FixedString64Bytes("Wood"),
                Category = MaterialCategory.Organic,
                Density = 600f,
                YoungsModulus = 10e9f, // Pa
                YieldStrength = 40e6f, // Pa
                Flexibility = 0.4f,
                HeatCapacity = 1700f
            });

            materials.Add(new MaterialSpec
            {
                MaterialId = new FixedString64Bytes("organic_tissue"),
                Name = new FixedString64Bytes("Organic Tissue"),
                Category = MaterialCategory.Organic,
                Density = 1000f,
                YoungsModulus = 1e6f, // Pa (very flexible)
                YieldStrength = 1e5f, // Pa
                Flexibility = 0.8f,
                HeatCapacity = 3500f
            });

            // Composite materials
            materials.Add(new MaterialSpec
            {
                MaterialId = new FixedString64Bytes("ceramic"),
                Name = new FixedString64Bytes("Ceramic"),
                Category = MaterialCategory.Composite,
                Density = 2400f,
                YoungsModulus = 300e9f, // Pa
                YieldStrength = 300e6f, // Pa
                Flexibility = 0.02f,
                HeatCapacity = 800f
            });

            var materialsArray = builder.Allocate(ref root.Materials, materials.Length);
            for (int i = 0; i < materials.Length; i++)
            {
                materialsArray[i] = materials[i];
            }

            materials.Dispose();

            var blob = builder.CreateBlobAssetReference<MaterialCatalogBlob>(Allocator.Persistent);
            builder.Dispose();

            var entity = state.EntityManager.CreateEntity(typeof(MaterialCatalog));
            state.EntityManager.SetComponentData(entity, new MaterialCatalog { Catalog = blob });
        }
    }
}

