using Unity.Burst;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using PureDOTS.Runtime.Aggregate;
using PureDOTS.Runtime.Villagers;
using PureDOTS.Runtime.Components;
using Unity.Collections.LowLevel.Unsafe;

namespace PureDOTS.Runtime.IntergroupRelations
{
    /// <summary>
    /// Creates relation edges when organizations first interact.
    /// Computes baseline Attitude/Trust/Fear from alignment/outlook compatibility.
    /// </summary>
    /// <summary>
    /// COLD path: Creates/destroys OrgRelation edges (sparse graph).
    /// Only for active pairs: bordering territories, trading partners, shared parent, historical enemies/allies.
    /// Event-driven: war, alliance, embargo, scandal, atrocities.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(OrgAlignmentUpdateSystem))]
    public partial struct OrgRelationInitSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton<TimeState>(out var timeState))
                return;
            
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            var currentTick = timeState.Tick;

            // Find all org pairs that need relations initialized
            var orgs = SystemAPI.QueryBuilder()
                .WithAll<OrgTag, OrgId>()
                .Build()
                .ToEntityArray(Allocator.Temp);

            for (int i = 0; i < orgs.Length; i++)
            {
                for (int j = i + 1; j < orgs.Length; j++)
                {
                    var orgA = orgs[i];
                    var orgB = orgs[j];

                    // Check if relation already exists
                    if (RelationExists(ref state, orgA, orgB))
                        continue;

                    // Check if they should interact (proximity, shared parent, etc.)
                    if (!ShouldCreateRelation(state, orgA, orgB))
                        continue;

                    // Create relation entity
                    var relationEntity = ecb.CreateEntity();
                    ecb.AddComponent(relationEntity, new OrgRelationTag());

                    // Compute baseline values
                    var baseline = ComputeBaselineRelation(ref state, orgA, orgB);

                    ecb.AddComponent(relationEntity, new OrgRelation
                    {
                        OrgA = orgA,
                        OrgB = orgB,
                        Kind = DetermineRelationKind(baseline.Attitude),
                        Treaties = OrgTreatyFlags.None,
                        Attitude = baseline.Attitude,
                        Trust = baseline.Trust,
                        Fear = baseline.Fear,
                        Respect = baseline.Respect,
                        Dependence = 0f,
                        EstablishedTick = currentTick,
                        LastUpdateTick = currentTick
                    });
                }
            }
        }

        private static bool RelationExists(ref SystemState state, Entity orgA, Entity orgB)
        {
            // Check if relation entity exists for this pair
            var query = state.EntityManager.CreateEntityQuery(typeof(OrgRelation), typeof(OrgRelationTag));
            var relations = query.ToComponentDataArray<OrgRelation>(Allocator.Temp);
            
            for (int i = 0; i < relations.Length; i++)
            {
                var relation = relations[i];
                if ((relation.OrgA == orgA && relation.OrgB == orgB) ||
                    (relation.OrgA == orgB && relation.OrgB == orgA))
                {
                    relations.Dispose();
                    query.Dispose();
                    return true;
                }
            }
            
            relations.Dispose();
            query.Dispose();
            return false;
        }

        private static bool ShouldCreateRelation(SystemState state, Entity orgA, Entity orgB)
        {
            // For now, create relations for all org pairs
            // In production, add proximity checks, shared parent checks, etc.
            return true;
        }

        private static BaselineRelation ComputeBaselineRelation(ref SystemState state, Entity orgA, Entity orgB)
        {
            var alignmentLookup = state.GetComponentLookup<VillagerAlignment>(true);
            alignmentLookup.Update(ref state);
            
            var alignmentA = alignmentLookup.HasComponent(orgA) 
                ? alignmentLookup[orgA] 
                : new VillagerAlignment();
            var alignmentB = alignmentLookup.HasComponent(orgB) 
                ? alignmentLookup[orgB] 
                : new VillagerAlignment();

            // Compute alignment compatibility (reuse formulas from Entity_Relations_And_Interactions.md)
            float moralDelta = ComputeMoralAxisDelta(alignmentA.MoralAxis, alignmentB.MoralAxis);
            float orderDelta = ComputeOrderAxisDelta(alignmentA.OrderAxis, alignmentB.OrderAxis);
            float purityDelta = ComputePurityAxisDelta(alignmentA.PurityAxis, alignmentB.PurityAxis);

            float alignmentCompatibility = moralDelta + orderDelta + purityDelta;

            // Base attitude from alignment compatibility
            float baseAttitude = math.clamp(alignmentCompatibility, -100f, 100f);

            // Base trust from alignment strength similarity
            float trust = math.clamp((alignmentA.AlignmentStrength + alignmentB.AlignmentStrength) / 2f, 0f, 1f);

            // Base fear from relative power (simplified - use alignment strength as proxy)
            float fear = math.clamp(math.abs(alignmentA.AlignmentStrength - alignmentB.AlignmentStrength), 0f, 1f);

            // Base respect from alignment similarity
            float respect = math.clamp(1f - (math.abs(alignmentA.MoralAxis - alignmentB.MoralAxis) + 
                                             math.abs(alignmentA.OrderAxis - alignmentB.OrderAxis) +
                                             math.abs(alignmentA.PurityAxis - alignmentB.PurityAxis)) / 600f, 0f, 1f);

            return new BaselineRelation
            {
                Attitude = baseAttitude,
                Trust = trust,
                Fear = fear,
                Respect = respect
            };
        }

        private static float ComputeMoralAxisDelta(sbyte moral1, sbyte moral2)
        {
            int delta = math.abs(moral1 - moral2);
            bool bothGood = moral1 > 30 && moral2 > 30;
            bool bothEvil = moral1 < -30 && moral2 < -30;
            bool opposite = (moral1 > 30 && moral2 < -30) || (moral1 < -30 && moral2 > 30);

            if (bothGood)
                return 20f - (delta * 0.2f);
            if (bothEvil)
                return 15f - (delta * 0.15f);
            if (opposite)
                return -(delta * 0.3f);
            return 0f;
        }

        private static float ComputeOrderAxisDelta(sbyte order1, sbyte order2)
        {
            int delta = math.abs(order1 - order2);
            bool bothLawful = order1 > 30 && order2 > 30;
            bool bothChaotic = order1 < -30 && order2 < -30;
            bool opposite = (order1 > 30 && order2 < -30) || (order1 < -30 && order2 > 30);

            if (bothLawful)
                return 15f - (delta * 0.1f);
            if (bothChaotic)
            {
                // Chaotic unpredictable - use hash-based random
                var hash = math.hash(new uint2((uint)order1, (uint)order2));
                var rng = new Unity.Mathematics.Random(hash);
                return rng.NextFloat(-10f, 10f);
            }
            if (opposite)
                return -(delta * 0.2f);
            return 0f;
        }

        private static float ComputePurityAxisDelta(sbyte purity1, sbyte purity2)
        {
            int delta = math.abs(purity1 - purity2);
            bool bothPure = purity1 > 30 && purity2 > 30;
            bool bothCorrupt = purity1 < -30 && purity2 < -30;
            bool opposite = (purity1 > 30 && purity2 < -30) || (purity1 < -30 && purity2 > 30);

            if (bothPure)
                return 10f - (delta * 0.05f);
            if (bothCorrupt)
                return -(delta * 0.1f);
            if (opposite)
            {
                if (purity1 > 30)
                    return -(delta * 0.15f);
                else
                    return -(delta * 0.05f);
            }
            return 0f;
        }

        private static OrgRelationKind DetermineRelationKind(float attitude)
        {
            if (attitude >= 50f)
                return OrgRelationKind.Allied;
            if (attitude >= 25f)
                return OrgRelationKind.Friendly;
            if (attitude <= -50f)
                return OrgRelationKind.Hostile;
            if (attitude <= -25f)
                return OrgRelationKind.Rival;
            return OrgRelationKind.Neutral;
        }

        private struct BaselineRelation
        {
            public float Attitude;
            public float Trust;
            public float Fear;
            public float Respect;
        }
    }
}

