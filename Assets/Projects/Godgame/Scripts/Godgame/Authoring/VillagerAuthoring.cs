using PureDOTS.Runtime.AI;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Spatial;
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

                AddBuffer<VillagerBelonging>(entity);
                AddBuffer<VillagerArchetypeModifier>(entity);

                // Add AI system components for shared AI pipeline
                AddAISystemComponents(entity, authoring);
            }

            private void AddAISystemComponents(Entity entity, VillagerAuthoring authoring)
            {
                // Create default AI utility archetype blob
                var blobBuilder = new BlobBuilder(Allocator.Temp);
                ref var root = ref blobBuilder.ConstructRoot<AIUtilityArchetypeBlob>();
                var actions = blobBuilder.Allocate(ref root.Actions, 4);

                // Action 0: SatisfyHunger
                ref var action0 = ref actions[0];
                var factors0 = blobBuilder.Allocate(ref action0.Factors, 1);
                factors0[0] = new AIUtilityCurveBlob
                {
                    SensorIndex = 0,
                    Threshold = 0.3f,
                    Weight = 2f,
                    ResponsePower = 2f,
                    MaxValue = 1f
                };

                // Action 1: Rest
                ref var action1 = ref actions[1];
                var factors1 = blobBuilder.Allocate(ref action1.Factors, 1);
                factors1[0] = new AIUtilityCurveBlob
                {
                    SensorIndex = 1,
                    Threshold = 0.2f,
                    Weight = 1.5f,
                    ResponsePower = 1.5f,
                    MaxValue = 1f
                };

                // Action 2: ImproveMorale
                ref var action2 = ref actions[2];
                var factors2 = blobBuilder.Allocate(ref action2.Factors, 1);
                factors2[0] = new AIUtilityCurveBlob
                {
                    SensorIndex = 2,
                    Threshold = 0.4f,
                    Weight = 1f,
                    ResponsePower = 1f,
                    MaxValue = 1f
                };

                // Action 3: Work
                ref var action3 = ref actions[3];
                var factors3 = blobBuilder.Allocate(ref action3.Factors, 1);
                factors3[0] = new AIUtilityCurveBlob
                {
                    SensorIndex = 3,
                    Threshold = 0f,
                    Weight = 0.8f,
                    ResponsePower = 1f,
                    MaxValue = 1f
                };

                var utilityBlob = blobBuilder.CreateBlobAssetReference<AIUtilityArchetypeBlob>(Allocator.Temp);
                blobBuilder.Dispose();
                AddBlobAsset(ref utilityBlob, out _);

                // Add AI sensor configuration
                AddComponent(entity, new AISensorConfig
                {
                    UpdateInterval = 0.5f, // Update sensors every 0.5 seconds
                    Range = 30f, // 30 unit detection range
                    MaxResults = 8, // Track up to 8 nearby entities
                    QueryOptions = SpatialQueryOptions.RequireDeterministicSorting,
                    PrimaryCategory = AISensorCategory.ResourceNode, // Primary: look for resources
                    SecondaryCategory = AISensorCategory.Storehouse // Secondary: look for storehouses
                });

                AddComponent(entity, new AISensorState
                {
                    Elapsed = 0f,
                    LastSampleTick = 0
                });

                AddBuffer<AISensorReading>(entity);

                // Add AI behaviour archetype with utility blob
                AddComponent(entity, new AIBehaviourArchetype
                {
                    UtilityBlob = utilityBlob
                });

                AddComponent(entity, new AIUtilityState
                {
                    BestActionIndex = 0,
                    BestScore = 0f,
                    LastEvaluationTick = 0
                });

                AddBuffer<AIActionState>(entity);

                // Add steering configuration
                AddComponent(entity, new AISteeringConfig
                {
                    MaxSpeed = 3f, // Match villager base speed
                    Acceleration = 8f,
                    Responsiveness = 0.5f,
                    DegreesOfFreedom = 2, // 2D movement
                    ObstacleLookAhead = 2f
                });

                AddComponent(entity, new AISteeringState
                {
                    DesiredDirection = float3.zero,
                    LinearVelocity = float3.zero,
                    LastSampledTarget = float3.zero,
                    LastUpdateTick = 0
                });

                AddComponent(entity, new AITargetState
                {
                    TargetEntity = Entity.Null,
                    TargetPosition = float3.zero,
                    ActionIndex = 0,
                    Flags = 0
                });

                // Add utility binding that maps action indices to villager goals
                var binding = new VillagerAIUtilityBinding();
                binding.Goals.Add(VillagerAIState.Goal.SurviveHunger); // Action 0
                binding.Goals.Add(VillagerAIState.Goal.Rest); // Action 1
                binding.Goals.Add(VillagerAIState.Goal.Rest); // Action 2 (social rest)
                binding.Goals.Add(VillagerAIState.Goal.Work); // Action 3
                AddComponent(entity, binding);
            }
        }
    }
}



