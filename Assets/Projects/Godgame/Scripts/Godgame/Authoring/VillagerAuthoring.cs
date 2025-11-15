using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Villagers;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Godgame.Authoring
{
    /// <summary>
    /// Authoring component for individual villager entities.
    /// Bakes PureDOTS components required for villager gameplay systems.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class VillagerAuthoring : MonoBehaviour
    {
        [SerializeField]
        private int villagerId = 1;

        [SerializeField]
        private int factionId = 0;

        [SerializeField]
        private VillagerJob.JobType jobType = VillagerJob.JobType.Gatherer;

        [SerializeField]
        private VillagerAIState.Goal aiGoal = VillagerAIState.Goal.Work;

        [SerializeField]
        private float3 spawnPosition = float3.zero;

        [SerializeField]
        private string baseArchetype = "";

        private sealed class Baker : Unity.Entities.Baker<VillagerAuthoring>
        {
            public override void Bake(VillagerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic | TransformUsageFlags.Renderable);
                
                // Add PureDOTS villager components
                AddComponent(entity, new VillagerId
                {
                    Value = authoring.villagerId,
                    FactionId = authoring.factionId
                });

                AddComponent(entity, new VillagerJob
                {
                    Type = authoring.jobType,
                    Phase = VillagerJob.JobPhase.Idle,
                    ActiveTicketId = 0,
                    Productivity = 1f
                });

                AddComponent(entity, new VillagerAIState
                {
                    CurrentState = VillagerAIState.State.Idle,
                    CurrentGoal = authoring.aiGoal,
                    TargetEntity = Entity.Null
                });

                // Set position if specified
                if (math.any(authoring.spawnPosition != float3.zero))
                {
                    var transform = GetComponent<Transform>();
                    if (transform != null)
                    {
                        transform.position = authoring.spawnPosition;
                    }
                }

                if (!string.IsNullOrWhiteSpace(authoring.baseArchetype))
                {
                    AddComponent(entity, new VillagerArchetypeAssignment
                    {
                        ArchetypeName = new FixedString64Bytes(authoring.baseArchetype),
                        CachedIndex = -1
                    });
                }

                VillagerArchetypeDefaults.CreateFallback(out var fallbackData);

                AddComponent(entity, new VillagerArchetypeResolved
                {
                    ArchetypeIndex = -1,
                    Data = fallbackData
                });

                if (!HasBuffer<VillagerBelonging>(entity))
                {
                    AddBuffer<VillagerBelonging>(entity);
                }

                if (!HasBuffer<VillagerArchetypeModifier>(entity))
                {
                    AddBuffer<VillagerArchetypeModifier>(entity);
                }
            }
        }
    }
}



