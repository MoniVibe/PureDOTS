using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Dynasty;
using PureDOTS.Runtime.Modularity;
using PureDOTS.Runtime.Organization;
using OrganizationComponent = PureDOTS.Runtime.Organization.Organization;
using PureDOTS.Runtime.Aggregates;
using PureDOTS.Runtime.Guild;
using Unity.Burst;
using Unity.Entities;

namespace PureDOTS.Runtime.Systems.Aggregates
{
    /// <summary>
    /// Ensures aggregate entities opt into collective memory and group knowledge modules.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderLast = true)]
    public partial struct AggregateCollectiveMemoryBootstrapSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var em = state.EntityManager;
            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);

            AddTagsForQuery(ref state, ecb, SystemAPI.QueryBuilder().WithAll<FactionId>().Build());
            AddTagsForQuery(ref state, ecb, SystemAPI.QueryBuilder().WithAll<CultureState>().Build());
            AddTagsForQuery(ref state, ecb, SystemAPI.QueryBuilder().WithAll<DynastyIdentity>().Build());
            AddTagsForQuery(ref state, ecb, SystemAPI.QueryBuilder().WithAll<OrganizationComponent>().Build());
            AddTagsForQuery(ref state, ecb, SystemAPI.QueryBuilder().WithAll<AggregateFaction>().Build());
            AddTagsForQuery(ref state, ecb, SystemAPI.QueryBuilder().WithAll<Guild>().Build());
            AddTagsForQuery(ref state, ecb, SystemAPI.QueryBuilder().WithAll<GuildId>().Build());

            ecb.Playback(em);
        }

        private static void AddTagsForQuery(ref SystemState state, EntityCommandBuffer ecb, EntityQuery query)
        {
            if (query.IsEmptyIgnoreFilter)
            {
                return;
            }

            var em = state.EntityManager;
            using var entities = query.ToEntityArray(state.WorldUpdateAllocator);
            for (int i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];
                if (!em.HasComponent<GroupMemoryModuleTag>(entity))
                {
                    ecb.AddComponent<GroupMemoryModuleTag>(entity);
                }

                if (!em.HasComponent<GroupKnowledgeModuleTag>(entity))
                {
                    ecb.AddComponent<GroupKnowledgeModuleTag>(entity);
                }
            }
        }
    }
}
