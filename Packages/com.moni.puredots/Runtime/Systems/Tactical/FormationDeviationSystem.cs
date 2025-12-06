using PureDOTS.Runtime.Bands;
using PureDOTS.Runtime.Combat;
using PureDOTS.Runtime.Components;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Systems.Tactical
{
    /// <summary>
    /// Applies individual deviation to formation members based on BehaviorProfile.
    /// Runs at 60 Hz in VillagerSystemGroup to affect individual movement.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(VillagerSystemGroup))]
    [UpdateAfter(typeof(BandFormationSystem))]
    public partial struct FormationDeviationSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var rewindState = SystemAPI.GetSingleton<RewindState>();
            if (rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            var timeState = SystemAPI.GetSingleton<TimeState>();
            var deltaTime = timeState.FixedDeltaTime;

            // Process entities with FormationMember and BehaviorProfile
            foreach (var (entity, member, behaviorProfile, transform) in SystemAPI
                         .Query<Entity, RefRW<FormationMember>, RefRO<BehaviorProfile>, RefRO<LocalTransform>>()
                         .WithEntityAccess())
            {
                var profile = behaviorProfile.ValueRO;
                var memberValue = member.ValueRO;

                // Get formation entity and its transform
                if (!SystemAPI.Exists(memberValue.FormationEntity) ||
                    !SystemAPI.HasComponent<LocalTransform>(memberValue.FormationEntity) ||
                    !SystemAPI.HasComponent<BandFormation>(memberValue.FormationEntity))
                {
                    continue;
                }

                var formationTransform = SystemAPI.GetComponent<LocalTransform>(memberValue.FormationEntity);
                var formation = SystemAPI.GetComponent<BandFormation>(memberValue.FormationEntity);

                // Compute target position (formation center + offset)
                var targetPosition = formationTransform.Position + memberValue.Offset;

                // Apply deviation based on Chaos value
                // Deterministic noise seeded from EntityId
                var hash = (uint)entity.Index;
                var noise = GenerateDeterministicNoise(hash, timeState.Tick);
                var deviation = noise * profile.Chaos * 2f; // Max 2 units deviation

                // Lerp between perfect target and deviated position
                var deviatedTarget = targetPosition + deviation;
                var steering = math.lerp(targetPosition, deviatedTarget, profile.Chaos);

                // Update alignment based on Discipline
                // Higher discipline = better alignment adherence
                var alignmentDecay = (1f - profile.Discipline) * deltaTime * 0.1f;
                var newAlignment = math.max(0f, memberValue.Alignment - alignmentDecay);

                // If close to target, increase alignment
                var distanceToTarget = math.distance(transform.ValueRO.Position, targetPosition);
                if (distanceToTarget < 0.5f)
                {
                    newAlignment = math.min(1f, newAlignment + profile.Discipline * deltaTime * 0.2f);
                }

                member.ValueRW = new FormationMember
                {
                    FormationEntity = memberValue.FormationEntity,
                    Offset = memberValue.Offset,
                    Alignment = newAlignment
                };

                // Update formation cohesion (will be aggregated by formation system)
                // For now, we just update the member's alignment
            }
        }

        [BurstCompile]
        private static float3 GenerateDeterministicNoise(uint seed, uint tick)
        {
            // Simple deterministic noise using hash
            var hash1 = hash(seed + tick);
            var hash2 = hash(seed + tick + 1);
            var hash3 = hash(seed + tick + 2);

            // Convert to -1 to 1 range
            var x = (hash1 % 2000) / 1000f - 1f;
            var y = 0f; // Keep Y at 0 for ground-based formations
            var z = (hash2 % 2000) / 1000f - 1f;

            return new float3(x, y, z);
        }

        [BurstCompile]
        private static uint hash(uint x)
        {
            x ^= x >> 16;
            x *= 0x85ebca6b;
            x ^= x >> 13;
            x *= 0xc2b2ae35;
            x ^= x >> 16;
            return x;
        }
    }
}

