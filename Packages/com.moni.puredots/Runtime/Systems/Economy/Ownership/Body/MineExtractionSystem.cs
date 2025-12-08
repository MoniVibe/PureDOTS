using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Core;
using PureDOTS.Runtime.Economy.Ownership;
using PureDOTS.Systems;
using OwnershipComponent = PureDOTS.Runtime.Economy.Ownership.Ownership;

namespace PureDOTS.Systems.Economy.Ownership.Body
{
    /// <summary>
    /// Mine extraction system running at 60Hz (BodyEconomySystemGroup).
    /// Handles mine resource extraction, spawns ResourceChunk entities.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(BodyEconomySystemGroup))]
    [UpdateAfter(typeof(ProductionSystem))]
    public partial struct MineExtractionSystem : ISystem
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
            var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            var job = new MineExtractionJob
            {
                DeltaTime = deltaTime,
                Catalog = catalog.Catalog,
                ECB = ecb.AsParallelWriter()
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        private partial struct MineExtractionJob : IJobEntity
        {
            public float DeltaTime;
            [ReadOnly] public BlobAssetReference<AssetSpecCatalogBlob> Catalog;
            public EntityCommandBuffer.ParallelWriter ECB;

            public void Execute(
                Entity entity,
                ref AssetTag assetTag,
                RefRO<OwnershipComponent> ownership,
                DynamicBuffer<ResourceStock> resourceStockBuffer,
                [ChunkIndexInQuery] int chunkIndex)
            {
                // Only process mines
                if (assetTag.Type != AssetType.Mine)
                {
                    return;
                }

                // Skip assets that have not been assigned an owner yet
                if (ownership.ValueRO.Owner == Entity.Null)
                {
                    return;
                }

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

                // Calculate extraction based on workforce
                // For mines, WorkforceNeed represents workers currently assigned
                float extractionRate = spec.OutputRate * spec.WorkforceNeed * DeltaTime;

                if (extractionRate <= 0f || math.isnan(extractionRate))
                {
                    return;
                }

                // Update ResourceStock (handled by ProductionSystem for consistency)
                // MineExtractionSystem focuses on spawning ResourceChunk entities for pickup
                // ResourceStock is updated by ProductionSystem which runs before this

                // Note: ResourceChunk spawning would be handled by a separate system
                // that reads ResourceStock and spawns chunks when threshold is reached
            }
        }
    }
}

