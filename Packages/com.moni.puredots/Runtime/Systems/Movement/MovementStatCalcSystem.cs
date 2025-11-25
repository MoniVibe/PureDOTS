using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Movement;
using PureDOTS.Systems;
using Space4X.Individuals;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Systems.Movement
{
    /// <summary>
    /// Computes PilotProficiency hot cache from MovementModelRef + ExpertiseEntry.
    /// Runs on initialization and when skills change (dirty tag).
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    public partial struct MovementStatCalcSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Get movement skill map singleton (if exists)
            BlobAssetReference<MovementSkillMapBlob> skillMap = default;
            bool hasSkillMap = SystemAPI.TryGetSingleton<MovementSkillMap>(out var skillMapRef);
            if (hasSkillMap)
            {
                skillMap = skillMapRef.Map;
            }

            var job = new MovementStatCalcJob
            {
                SkillMap = skillMap,
                HasSkillMap = hasSkillMap
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        public partial struct MovementStatCalcJob : IJobEntity
        {
            [ReadOnly] public BlobAssetReference<MovementSkillMapBlob> SkillMap;
            public bool HasSkillMap;

            void Execute(
                Entity entity,
                ref PilotProficiency proficiency,
                in MovementModelRef modelRef,
                in DynamicBuffer<ExpertiseEntry> expertise)
            {
                if (!modelRef.Blob.IsCreated)
                {
                    // Default proficiency if no model
                    proficiency = new PilotProficiency
                    {
                        ControlMult = 1f,
                        TurnRateMult = 1f,
                        EnergyMult = 1f,
                        Jitter = 0f,
                        ReactionSec = 0.5f
                    };
                    return;
                }

                ref var spec = ref modelRef.Blob.Value;
                byte expertiseType = 0;
                byte tier = 0;

                // Find matching expertise entry via skill map
                if (HasSkillMap && SkillMap.IsCreated)
                {
                    ref var map = ref SkillMap.Value;
                    byte movementKindIndex = (byte)spec.Kind;
                    if (movementKindIndex < map.ExpertiseTypesByKind.Length)
                    {
                        expertiseType = map.ExpertiseTypesByKind[movementKindIndex];

                        // Find expertise entry with matching type
                        for (int i = 0; i < expertise.Length; i++)
                        {
                            if (expertise[i].Type == (ExpertiseType)expertiseType)
                            {
                                tier = expertise[i].Tier;
                                break;
                            }
                        }
                    }
                }

                // Compute proficiency multipliers from tier (0-255)
                // Tier 0 = novice, Tier 255 = master
                float tierNormalized = tier / 255f; // 0.0 to 1.0

                // Control multiplier: 0.5 (novice) to 1.5 (master)
                proficiency.ControlMult = 0.5f + tierNormalized * 1.0f;

                // Turn rate multiplier: 0.7 (novice) to 1.3 (master)
                proficiency.TurnRateMult = 0.7f + tierNormalized * 0.6f;

                // Energy efficiency: 1.5 (novice, wasteful) to 0.7 (master, efficient)
                proficiency.EnergyMult = 1.5f - tierNormalized * 0.8f;

                // Jitter: 0.1 (novice) to 0.0 (master)
                proficiency.Jitter = 0.1f * (1f - tierNormalized);

                // Reaction delay: 1.0s (novice) to 0.1s (master)
                proficiency.ReactionSec = 1.0f - tierNormalized * 0.9f;
            }
        }
    }

    /// <summary>
    /// Singleton component holding the movement skill map blob reference.
    /// </summary>
    public struct MovementSkillMap : IComponentData
    {
        public BlobAssetReference<MovementSkillMapBlob> Map;
    }
}

