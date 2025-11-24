#if UNITY_EDITOR
using PureDOTS.Runtime.AI;
using PureDOTS.Runtime.Spatial;
using Space4X.Runtime;
using Space4X.Runtime.Transport;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Space4X.Authoring
{
    /// <summary>
    /// Authoring component for mining vessels.
    /// Adds this to mining vessel GameObjects so they have movement and AI components.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MiningVesselAuthoring : MonoBehaviour
    {
        [Header("Movement")]
        [Range(0.1f, 20f)] public float baseSpeed = 5f;

        [Header("Mining")]
        [Range(1f, 100f)] public float capacity = 50f;
        public ushort resourceTypeIndex = 0; // 0 = ore (can be expanded later)
    }

    public sealed class MiningVesselBaker : Baker<MiningVesselAuthoring>
    {
        public override void Bake(MiningVesselAuthoring authoring)
        {
            var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);

            // Add miner vessel component
            AddComponent(entity, new MinerVessel
            {
                ResourceTypeIndex = authoring.resourceTypeIndex,
                Capacity = authoring.capacity,
                Load = 0f,
                Flags = TransportUnitFlags.Idle,
                LastCommandTick = 0
            });

            // Add vessel AI state
            AddComponent(entity, new VesselAIState
            {
                CurrentState = VesselAIState.State.Idle,
                CurrentGoal = VesselAIState.Goal.None,
                TargetEntity = Entity.Null,
                TargetPosition = float3.zero,
                StateTimer = 0f,
                StateStartTick = 0
            });

            // Add vessel movement
            AddComponent(entity, new VesselMovement
            {
                Velocity = float3.zero,
                BaseSpeed = authoring.baseSpeed,
                CurrentSpeed = authoring.baseSpeed,
                DesiredRotation = quaternion.identity,
                IsMoving = 0,
                LastMoveTick = 0
            });

            // Add AI system components for shared AI pipeline
            AddAISystemComponents(entity, authoring);

            // LocalTransform should be added automatically with TransformUsageFlags.Dynamic.
            // It will be synced from the GameObject transform automatically.
            // No need to manually set it here - Unity handles the sync for Dynamic transforms.
        }

        private void AddAISystemComponents(Entity entity, MiningVesselAuthoring authoring)
        {
            // Create default vessel AI utility archetype blob
            // Actions: 0 = Mining, 1 = Returning
            var blobBuilder = new BlobBuilder(Allocator.Temp);
            ref var root = ref blobBuilder.ConstructRoot<AIUtilityArchetypeBlob>();
            var actions = blobBuilder.Allocate(ref root.Actions, 2);

            // Action 0: Mining - prefers nearby resource nodes when not full
            ref var action0 = ref actions[0];
            var factors0 = blobBuilder.Allocate(ref action0.Factors, 1);
            factors0[0] = new AIUtilityCurveBlob
            {
                SensorIndex = 0, // Nearest resource node
                Threshold = 0f, // Always consider mining if resources available
                Weight = 1f,
                ResponsePower = 1f,
                MaxValue = 1f
            };

            // Action 1: Returning - prefers nearby carriers when full
            ref var action1 = ref actions[1];
            var factors1 = blobBuilder.Allocate(ref action1.Factors, 1);
            factors1[0] = new AIUtilityCurveBlob
            {
                SensorIndex = 1, // Nearest carrier/transport unit
                Threshold = 0f,
                Weight = 1f,
                ResponsePower = 1f,
                MaxValue = 1f
            };

            var utilityBlob = blobBuilder.CreateBlobAssetReference<AIUtilityArchetypeBlob>(Allocator.Temp);
            blobBuilder.Dispose();
            AddBlobAsset(ref utilityBlob, out _);

            // Add AI sensor configuration - look for resource nodes and carriers
            AddComponent(entity, new AISensorConfig
            {
                UpdateInterval = 1f, // Update sensors every second
                Range = 100f, // 100 unit detection range (larger for space)
                MaxResults = 10, // Track up to 10 nearby entities
                QueryOptions = SpatialQueryOptions.RequireDeterministicSorting,
                PrimaryCategory = AISensorCategory.ResourceNode, // Primary: look for asteroids/resources
                SecondaryCategory = AISensorCategory.TransportUnit // Secondary: look for carriers
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
                MaxSpeed = authoring.baseSpeed,
                Acceleration = 10f,
                Responsiveness = 0.7f,
                DegreesOfFreedom = 3, // 3D movement in space
                ObstacleLookAhead = 5f
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

            // Add utility binding that maps action indices to vessel goals
            var binding = new VesselAIUtilityBinding();
            binding.Goals.Add(VesselAIState.Goal.Mining); // Action 0
            binding.Goals.Add(VesselAIState.Goal.Returning); // Action 1
            AddComponent(entity, binding);
        }
    }
}
#endif

