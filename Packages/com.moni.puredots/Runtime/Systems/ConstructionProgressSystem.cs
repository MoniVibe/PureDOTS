using PureDOTS.Runtime.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Systems
{
    /// <summary>
    /// Processes construction progress: handles material delivery and completion.
    /// </summary>
    [UpdateInGroup(typeof(ConstructionSystemGroup))]
    [UpdateAfter(typeof(ConstructionRegistrySystem))]
    public partial struct ConstructionProgressSystem : ISystem
    {
        private ComponentLookup<ResourceTypeIndex> _resourceCatalogLookup;
        private ComponentLookup<ConstructionSiteProgress> _progressLookup;
        private ComponentLookup<ConstructionSiteFlags> _flagsLookup;
        private ComponentLookup<ConstructionCompletionPrefab> _completionPrefabLookup;
        private ComponentLookup<LocalTransform> _transformLookup;
        private BufferLookup<ConstructionCostElement> _costBufferLookup;
        private BufferLookup<ConstructionDeliveredElement> _deliveredBufferLookup;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
            state.RequireForUpdate<ResourceTypeIndex>();

            _resourceCatalogLookup = state.GetComponentLookup<ResourceTypeIndex>(true);
            _progressLookup = state.GetComponentLookup<ConstructionSiteProgress>(false);
            _flagsLookup = state.GetComponentLookup<ConstructionSiteFlags>(false);
            _completionPrefabLookup = state.GetComponentLookup<ConstructionCompletionPrefab>(true);
            _transformLookup = state.GetComponentLookup<LocalTransform>(true);
            _costBufferLookup = state.GetBufferLookup<ConstructionCostElement>(true);
            _deliveredBufferLookup = state.GetBufferLookup<ConstructionDeliveredElement>(false);
        }

        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            if (timeState.IsPaused)
            {
                return;
            }

            var rewindState = SystemAPI.GetSingleton<RewindState>();
            if (rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            _resourceCatalogLookup.Update(ref state);
            _progressLookup.Update(ref state);
            _flagsLookup.Update(ref state);
            _completionPrefabLookup.Update(ref state);
            _transformLookup.Update(ref state);
            _costBufferLookup.Update(ref state);
            _deliveredBufferLookup.Update(ref state);

            var catalog = SystemAPI.GetSingleton<ResourceTypeIndex>().Catalog;
            if (!catalog.IsCreated)
            {
                return;
            }

            // Process deposit commands (materials delivered to construction sites)
            foreach (var (deposits, siteEntity) in SystemAPI.Query<DynamicBuffer<ConstructionDepositCommand>>().WithEntityAccess())
            {
                if (!_progressLookup.HasComponent(siteEntity) ||
                    !_costBufferLookup.HasBuffer(siteEntity) ||
                    !_deliveredBufferLookup.HasBuffer(siteEntity))
                {
                    deposits.Clear();
                    continue;
                }

                var progress = _progressLookup[siteEntity];
                var costBuffer = _costBufferLookup[siteEntity];
                var deliveredBuffer = _deliveredBufferLookup[siteEntity];

                // Process each deposit command
                for (int i = deposits.Length - 1; i >= 0; i--)
                {
                    var deposit = deposits[i];
                    bool processed = false;

                    // Find matching cost element
                    for (int j = 0; j < costBuffer.Length; j++)
                    {
                        var cost = costBuffer[j];
                        if (cost.ResourceTypeId.Equals(deposit.ResourceTypeId))
                        {
                            // Find or create delivered element
                            int deliveredIndex = -1;
                            for (int k = 0; k < deliveredBuffer.Length; k++)
                            {
                                if (deliveredBuffer[k].ResourceTypeId.Equals(deposit.ResourceTypeId))
                                {
                                    deliveredIndex = k;
                                    break;
                                }
                            }

                            if (deliveredIndex < 0)
                            {
                                deliveredBuffer.Add(new ConstructionDeliveredElement
                                {
                                    ResourceTypeId = deposit.ResourceTypeId,
                                    UnitsDelivered = 0f
                                });
                                deliveredIndex = deliveredBuffer.Length - 1;
                            }

                            var delivered = deliveredBuffer[deliveredIndex];
                            var newDelivered = math.min(delivered.UnitsDelivered + deposit.Amount, cost.UnitsRequired);
                            delivered.UnitsDelivered = newDelivered;
                            deliveredBuffer[deliveredIndex] = delivered;

                            // Update progress based on material completion
                            float materialProgress = 0f;
                            float totalRequired = 0f;
                            float totalDelivered = 0f;

                            for (int k = 0; k < costBuffer.Length; k++)
                            {
                                totalRequired += costBuffer[k].UnitsRequired;
                                var delIdx = -1;
                                for (int l = 0; l < deliveredBuffer.Length; l++)
                                {
                                    if (deliveredBuffer[l].ResourceTypeId.Equals(costBuffer[k].ResourceTypeId))
                                    {
                                        delIdx = l;
                                        break;
                                    }
                                }
                                if (delIdx >= 0)
                                {
                                    totalDelivered += deliveredBuffer[delIdx].UnitsDelivered;
                                }
                            }

                            if (totalRequired > 0f)
                            {
                                materialProgress = totalDelivered / totalRequired;
                            }

                            // Progress is a combination of materials delivered and work done
                            // For now, materials contribute 50% of progress
                            var materialContribution = materialProgress * 0.5f;
                            var workContribution = math.clamp(progress.CurrentProgress / progress.RequiredProgress, 0f, 0.5f);
                            progress.CurrentProgress = (materialContribution + workContribution) * progress.RequiredProgress;
                            _progressLookup[siteEntity] = progress;

                            processed = true;
                            break;
                        }
                    }

                    if (processed)
                    {
                        deposits.RemoveAt(i);
                    }
                }
            }

            // Process progress commands (work done on construction sites)
            foreach (var (progressCommands, siteEntity) in SystemAPI.Query<DynamicBuffer<ConstructionProgressCommand>>().WithEntityAccess())
            {
                if (!_progressLookup.HasComponent(siteEntity))
                {
                    progressCommands.Clear();
                    continue;
                }

                var progress = _progressLookup[siteEntity];

                for (int i = progressCommands.Length - 1; i >= 0; i--)
                {
                    var cmd = progressCommands[i];
                    progress.CurrentProgress = math.min(
                        progress.CurrentProgress + cmd.Delta,
                        progress.RequiredProgress);
                    progressCommands.RemoveAt(i);
                }

                _progressLookup[siteEntity] = progress;

                // Check for completion
                if (progress.CurrentProgress >= progress.RequiredProgress)
                {
                    // Mark as completed
                    if (_flagsLookup.HasComponent(siteEntity))
                    {
                        var flags = _flagsLookup[siteEntity];
                        flags.Value |= ConstructionSiteFlags.Completed;
                        _flagsLookup[siteEntity] = flags;
                    }

                    // Handle completion prefab spawn
                    if (_completionPrefabLookup.HasComponent(siteEntity))
                    {
                        var completionPrefab = _completionPrefabLookup[siteEntity];
                        if (completionPrefab.Prefab != Entity.Null)
                        {
                            // Spawn completion entity
                            var completedEntity = state.EntityManager.Instantiate(completionPrefab.Prefab);
                            
                            // Copy transform from construction site
                            if (_transformLookup.HasComponent(siteEntity) &&
                                _transformLookup.HasComponent(completedEntity))
                            {
                                var siteTransform = _transformLookup[siteEntity];
                                state.EntityManager.SetComponentData(completedEntity, siteTransform);
                            }

                            // Destroy construction site if requested
                            if (completionPrefab.DestroySiteEntity)
                            {
                                state.EntityManager.DestroyEntity(siteEntity);
                            }
                        }
                    }
                }
            }
        }
    }
}

