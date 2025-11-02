using System.Collections.Generic;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Registry;
using PureDOTS.Runtime.Resource;
using Space4X.Runtime;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Space4X.Systems
{
    /// <summary>
    /// Debug system to check vessel and villager entity states and why they might not be moving.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class EntityStateDebugSystem : SystemBase
    {
        private int _frameCount;
        private const int LogInterval = 60; // Log every 60 frames (~1 second at 60fps)

        protected override void OnCreate()
        {
            RequireForUpdate<TimeState>();
        }

        protected override void OnUpdate()
        {
            _frameCount++;
            if (_frameCount % LogInterval != 0)
                return;

            var timeState = SystemAPI.GetSingleton<TimeState>();
            if (timeState.IsPaused)
                return;

            Debug.Log("=== ENTITY STATE DEBUG ===");

            // Check vessels
            var vesselCount = 0;
            var vesselsWithTargets = 0;
            var vesselsMoving = 0;
            foreach (var (transform, aiState, movement, entity) in SystemAPI.Query<RefRO<LocalTransform>, RefRO<VesselAIState>, RefRO<VesselMovement>>()
                .WithEntityAccess())
            {
                vesselCount++;
                var targetEntity = aiState.ValueRO.TargetEntity;
                var targetPos = aiState.ValueRO.TargetPosition;
                var isMoving = movement.ValueRO.IsMoving != 0;

                if (targetEntity != Entity.Null || !targetPos.Equals(float3.zero))
                {
                    vesselsWithTargets++;
                }

                if (isMoving)
                {
                    vesselsMoving++;
                }

                if (vesselCount <= 3) // Log first 3 vessels
                {
                    Debug.Log($"Vessel {vesselCount}: Pos={transform.ValueRO.Position}, State={aiState.ValueRO.CurrentState}, Goal={aiState.ValueRO.CurrentGoal}, " +
                             $"TargetEntity={(targetEntity == Entity.Null ? "NULL" : targetEntity.ToString())}, " +
                             $"TargetPos={targetPos}, IsMoving={isMoving}, Speed={movement.ValueRO.CurrentSpeed}");
                }
            }

            Debug.Log($"Vessels: Total={vesselCount}, WithTargets={vesselsWithTargets}, Moving={vesselsMoving}");

            // Check villagers
            var villagerCount = 0;
            var villagersWithJobs = 0;
            var villagersMoving = 0;
            foreach (var (transform, aiState, movement, job, ticket, entity) in SystemAPI.Query<RefRO<LocalTransform>, RefRO<PureDOTS.Runtime.Components.VillagerAIState>, 
                RefRO<PureDOTS.Runtime.Components.VillagerMovement>, RefRO<PureDOTS.Runtime.Components.VillagerJob>, 
                RefRO<PureDOTS.Runtime.Components.VillagerJobTicket>>()
                .WithEntityAccess())
            {
                villagerCount++;
                var hasJob = job.ValueRO.Type != PureDOTS.Runtime.Components.VillagerJob.JobType.None;
                var ticketPhase = (PureDOTS.Runtime.Components.VillagerJob.JobPhase)ticket.ValueRO.Phase;
                var isMoving = movement.ValueRO.IsMoving != 0;

                if (hasJob && ticketPhase != PureDOTS.Runtime.Components.VillagerJob.JobPhase.Idle)
                {
                    villagersWithJobs++;
                }

                if (isMoving)
                {
                    villagersMoving++;
                }

                if (villagerCount <= 3) // Log first 3 villagers
                {
                    Debug.Log($"Villager {villagerCount}: Pos={transform.ValueRO.Position}, State={aiState.ValueRO.CurrentState}, Goal={aiState.ValueRO.CurrentGoal}, " +
                             $"Job={job.ValueRO.Type}, TicketPhase={ticketPhase}, IsMoving={isMoving}");
                }
            }

            Debug.Log($"Villagers: Total={villagerCount}, WithJobs={villagersWithJobs}, Moving={villagersMoving}");

            // Check resource registry
            if (SystemAPI.HasSingleton<ResourceRegistry>())
            {
                var registry = SystemAPI.GetSingleton<ResourceRegistry>();
                var registryEntity = SystemAPI.GetSingletonEntity<ResourceRegistry>();
                var entries = EntityManager.GetBuffer<PureDOTS.Runtime.Components.ResourceRegistryEntry>(registryEntity);
                Debug.Log($"ResourceRegistry: Total={registry.TotalResources}, Active={registry.TotalActiveResources}, Entries={entries.Length}");

                if (entries.Length > 0 && SystemAPI.HasSingleton<ResourceRecipeSet>() && SystemAPI.HasSingleton<ResourceTypeIndex>())
                {
                    var recipeSet = SystemAPI.GetSingleton<ResourceRecipeSet>();
                    var catalog = SystemAPI.GetSingleton<ResourceTypeIndex>().Catalog;

                    if (recipeSet.Value.IsCreated && catalog.IsCreated)
                    {
                        var tierCounts = new Dictionary<ResourceTier, int>();
                        var familyCounts = new Dictionary<ushort, (int raw, int refined, int composite, int byproduct)>();

                        for (int i = 0; i < entries.Length; i++)
                        {
                            var entry = entries[i];

                            tierCounts.TryGetValue(entry.Tier, out var tierCount);
                            tierCounts[entry.Tier] = tierCount + 1;

                            if (entry.FamilyIndex != ushort.MaxValue)
                            {
                                if (!familyCounts.TryGetValue(entry.FamilyIndex, out var bundle))
                                {
                                    bundle = (0, 0, 0, 0);
                                }

                                switch (entry.Tier)
                                {
                                    case ResourceTier.Raw:
                                        bundle.raw++;
                                        break;
                                    case ResourceTier.Refined:
                                        bundle.refined++;
                                        break;
                                    case ResourceTier.Composite:
                                        bundle.composite++;
                                        break;
                                    case ResourceTier.Byproduct:
                                        bundle.byproduct++;
                                        break;
                                }

                                familyCounts[entry.FamilyIndex] = bundle;
                            }
                        }

                        foreach (var tier in tierCounts)
                        {
                            Debug.Log($"  Tier {tier.Key}: {tier.Value}");
                        }

                        ref var recipeBlob = ref recipeSet.Value.Value;
                        foreach (var kvp in familyCounts)
                        {
                            string label = kvp.Key < recipeBlob.Families.Length
                                ? recipeBlob.Families[kvp.Key].DisplayName.ToString()
                                : $"Family_{kvp.Key}";
                            var counts = kvp.Value;
                            Debug.Log($"  Family {label}: Raw={counts.raw}, Refined={counts.refined}, Composite={counts.composite}, Byproduct={counts.byproduct}");
                        }
                    }
                }
            }
            else
            {
                Debug.LogWarning("ResourceRegistry singleton NOT FOUND!");
            }

            Debug.Log("=== END DEBUG ===");
        }
    }
}

