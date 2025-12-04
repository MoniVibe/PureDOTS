using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Runtime.Components;

namespace PureDOTS.Runtime.IntergroupRelations
{
    /// <summary>
    /// Decays extreme attitudes toward baseline over time.
    /// Rate based on OrgPersona (vengeful orgs decay slower, forgiving faster).
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(OrgRelationInitSystem))]
    public partial struct OrgRelationDecaySystem : ISystem
    {
        private const float DECAY_RATE_PER_TICK = 0.0001f; // Adjust based on tick rate
        private const uint DECAY_CHECK_INTERVAL = 100; // Check every 100 ticks

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var currentTick = SystemAPI.GetSingleton<TimeState>().Tick;

            foreach (var (relation, orgA, orgB) in SystemAPI.Query<RefRW<OrgRelation>>()
                .WithAll<OrgRelationTag>()
                .WithEntityAccess())
            {
                // Only decay periodically
                if (currentTick - relation.ValueRO.LastUpdateTick < DECAY_CHECK_INTERVAL)
                    continue;

                // Get org personas for decay rate calculation
                float decayRate = DECAY_RATE_PER_TICK;
                
                if (SystemAPI.HasComponent<OrgPersona>(relation.ValueRO.OrgA))
                {
                    var personaA = SystemAPI.GetComponent<OrgPersona>(relation.ValueRO.OrgA);
                    // Vengeful orgs decay slower (lower decay rate)
                    // Forgiving orgs decay faster (higher decay rate)
                    float vengefulFactor = 1f - personaA.VengefulForgiving; // Invert: forgiving = higher factor
                    decayRate *= (0.5f + vengefulFactor * 0.5f); // Range: 0.5x to 1.0x
                }

                // Compute baseline attitude (from initial relation or neutral)
                float baselineAttitude = 0f; // Neutral baseline
                
                // Decay toward baseline
                float attitudeDelta = relation.ValueRO.Attitude - baselineAttitude;
                float decayAmount = attitudeDelta * decayRate * DECAY_CHECK_INTERVAL;
                
                relation.ValueRW.Attitude = math.clamp(relation.ValueRO.Attitude - decayAmount, -100f, 100f);
                
                // Also decay trust/fear/respect slightly
                relation.ValueRW.Trust = math.clamp(relation.ValueRO.Trust - 0.001f * DECAY_CHECK_INTERVAL, 0f, 1f);
                relation.ValueRW.Fear = math.clamp(relation.ValueRO.Fear - 0.001f * DECAY_CHECK_INTERVAL, 0f, 1f);
                relation.ValueRW.Respect = math.clamp(relation.ValueRO.Respect - 0.001f * DECAY_CHECK_INTERVAL, 0f, 1f);
                
                relation.ValueRW.LastUpdateTick = currentTick;
            }
        }
    }
}

