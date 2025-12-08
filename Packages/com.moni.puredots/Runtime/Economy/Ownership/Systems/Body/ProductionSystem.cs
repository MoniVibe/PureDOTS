using PureDOTS.Systems;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Core;
using PureDOTS.Runtime.Economy.Ownership;

namespace PureDOTS.Systems.Economy.Ownership.Body
{
    /// <summary>
    /// Production system running at 60Hz (BodyEconomySystemGroup).
    /// Calculates production from AssetSpec, updates ResourceStock buffers.
    /// </summary>
    [BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct ProductionSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RewindState>();
            state.RequireForUpdate<AssetSpecCatalog>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var rewind = SystemAPI.GetSingleton<RewindState>();
            if (rewind.Mode != RewindMode.Record)
            {
                return;
            }

            var deltaTime = SystemAPI.Time.DeltaTime;
            var catalog = SystemAPI.GetSingleton<AssetSpecCatalog>();

            var job = new ProductionJob
            {
                DeltaTime = deltaTime,
                Catalog = catalog.Catalog
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        private partial struct ProductionJob : IJobEntity
        {
            public float DeltaTime;
            [ReadOnly] public BlobAssetReference<AssetSpecCatalogBlob> Catalog;

            public void Execute(
                Entity entity,
                ref AssetTag assetTag,
                DynamicBuffer<ResourceStock> resourceStockBuffer)
            {
                if (!Catalog.IsCreated)
                {
                    return;
                }

                ref var catalogBlob = ref Catalog.Value;
                if (catalogBlob.Specs.Length <= (int)assetTag.Type)
                {
                    return;
                }

                ref var spec = ref catalogBlob.Specs[(int)assetTag.Type];

                // Skip if no output type specified
                if (spec.OutputType.Length == 0)
                {
                    return;
                }

                // Calculate production: OutputRate * Efficiency * DeltaTime
                // Efficiency is 1.0 for now (can be modified by workforce, upgrades, etc.)
                float efficiency = 1.0f;
                float production = spec.OutputRate * efficiency * DeltaTime;

                if (production <= 0f || math.isnan(production))
                {
                    return;
                }

                // Find or create ResourceStock entry for this output type
                bool found = false;
                for (int i = 0; i < resourceStockBuffer.Length; i++)
                {
                    var stock = resourceStockBuffer[i];
                    if (stock.ResourceType.Equals(spec.OutputType))
                    {
                        // Update existing stock
                        stock.Quantity += production;
                        if (stock.MaxCapacity > 0f)
                        {
                            stock.Quantity = math.min(stock.Quantity, stock.MaxCapacity);
                        }
                        resourceStockBuffer[i] = stock;
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    // Add new ResourceStock entry
                    resourceStockBuffer.Add(new ResourceStock
                    {
                        ResourceType = spec.OutputType,
                        Quantity = production,
                        MaxCapacity = 0f // Unlimited by default
                    });
                }
            }
        }
    }
}

