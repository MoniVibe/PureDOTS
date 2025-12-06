using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Runtime.Cognitive;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.IntergroupRelations;

namespace PureDOTS.Systems.IntergroupRelations
{
    /// <summary>
    /// Maintains culture memory graph singleton and aggregates culture profiles from org relations.
    /// Updates CultureProfile components based on OrgRelation events and individual experiences.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(OrgRelationEventImpactSystem))]
    public partial struct CultureMemoryGraphSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton<TimeState>(out var timeState))
                return;

            var currentTick = timeState.Tick;

            // Ensure CultureMemoryGraph singleton exists
            if (!SystemAPI.HasSingleton<CultureMemoryGraph>())
            {
                var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
                var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
                var singletonEntity = state.EntityManager.CreateEntity();
                ecb.AddComponent(singletonEntity, new CultureMemoryGraph
                {
                    CultureCount = 0,
                    LastUpdateTick = currentTick
                });
                return;
            }

            var graph = SystemAPI.GetSingletonRW<CultureMemoryGraph>();

            // Update culture profiles from org relations
            // Aggregate OrgRelation data into CultureProfile statistics
            var orgIdLookup = state.GetComponentLookup<OrgId>(true);
            var orgPersonaLookup = state.GetComponentLookup<OrgPersona>(true);
            var relationQuery = SystemAPI.QueryBuilder()
                .WithAll<OrgRelation, OrgRelationTag>()
                .Build();

            orgIdLookup.Update(ref state);
            orgPersonaLookup.Update(ref state);

            // Collect culture statistics from relations
            var cultureStats = new NativeHashMap<ushort, CultureStats>(32, Allocator.Temp);
            
            foreach (var relation in relationQuery.ToComponentDataArray<OrgRelation>(Allocator.Temp))
            {
                // Get culture IDs from orgs (assuming OrgId.Value maps to culture)
                ushort cultureA = (ushort)math.clamp(relation.OrgA.Index, 0, ushort.MaxValue);
                ushort cultureB = (ushort)math.clamp(relation.OrgB.Index, 0, ushort.MaxValue);

                // Aggregate relation data into culture stats
                if (!cultureStats.ContainsKey(cultureA))
                {
                    cultureStats[cultureA] = new CultureStats();
                }
                if (!cultureStats.ContainsKey(cultureB))
                {
                    cultureStats[cultureB] = new CultureStats();
                }

                var statsA = cultureStats[cultureA];
                var statsB = cultureStats[cultureB];

                // Update aggression from attitude (negative attitude = higher aggression)
                statsA.Aggression += math.max(0f, -relation.Attitude / 100f);
                statsB.Aggression += math.max(0f, -relation.Attitude / 100f);

                // Update trustworthiness from trust
                statsA.Trustworthiness += relation.Trust;
                statsB.Trustworthiness += relation.Trust;

                // Update reputation from respect
                statsA.Reputation += relation.Respect;
                statsB.Reputation += relation.Respect;

                statsA.RelationCount++;
                statsB.RelationCount++;

                cultureStats[cultureA] = statsA;
                cultureStats[cultureB] = statsB;
            }

            // Update or create CultureProfile entities
            var profileQuery = SystemAPI.QueryBuilder()
                .WithAll<CultureProfile>()
                .Build();

            var existingProfiles = new NativeHashMap<ushort, Entity>(32, Allocator.Temp);
            foreach (var (profile, entity) in SystemAPI.Query<RefRO<CultureProfile>>()
                .WithEntityAccess())
            {
                existingProfiles[profile.ValueRO.Id] = entity;
            }

            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var kvp in cultureStats)
            {
                var cultureId = kvp.Key;
                var stats = kvp.Value;

                if (existingProfiles.TryGetValue(cultureId, out var existingEntity))
                {
                    // Update existing profile
                    var profile = SystemAPI.GetComponentRW<CultureProfile>(existingEntity);
                    profile.ValueRW.Aggression = math.clamp(stats.Aggression / math.max(1, stats.RelationCount), 0f, 1f);
                    profile.ValueRW.Trustworthiness = math.clamp(stats.Trustworthiness / math.max(1, stats.RelationCount), 0f, 1f);
                    profile.ValueRW.Reputation = math.clamp(stats.Reputation / math.max(1, stats.RelationCount), 0f, 1f);
                    profile.ValueRW.LastUpdateTick = currentTick;
                }
                else
                {
                    // Create new profile
                    var newEntity = ecb.CreateEntity();
                    ecb.AddComponent(newEntity, new CultureProfile
                    {
                        Id = cultureId,
                        Aggression = math.clamp(stats.Aggression / math.max(1, stats.RelationCount), 0f, 1f),
                        Trustworthiness = math.clamp(stats.Trustworthiness / math.max(1, stats.RelationCount), 0f, 1f),
                        MagicStyle = 0.5f, // Default, can be updated from other sources
                        Reputation = math.clamp(stats.Reputation / math.max(1, stats.RelationCount), 0f, 1f),
                        LastUpdateTick = currentTick
                    });
                }
            }

            graph.ValueRW.CultureCount = cultureStats.Count;
            graph.ValueRW.LastUpdateTick = currentTick;

            cultureStats.Dispose();
            existingProfiles.Dispose();
        }

        private struct CultureStats
        {
            public float Aggression;
            public float Trustworthiness;
            public float Reputation;
            public int RelationCount;
        }
    }
}

