using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Runtime.Components;

namespace PureDOTS.Runtime.IntergroupRelations
{
    /// <summary>
    /// Updates ownership shares for hostile/mutual acquisitions.
    /// Hostile acquisitions gradually increase OrgOwnership.Share over time.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(OrgRelationInitSystem))]
    public partial struct OrgOwnershipSystem : ISystem
    {
        private const float OWNERSHIP_INCREASE_RATE = 0.001f; // Per tick
        private const uint OWNERSHIP_UPDATE_INTERVAL = 100; // Update every 100 ticks

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton<TimeState>(out var timeState))
                return;
            
            var currentTick = timeState.Tick;
            var ownershipBufferLookup = state.GetBufferLookup<OrgOwnership>(false);
            ownershipBufferLookup.Update(ref state);

            // Process ownership changes for all orgs
            foreach (var (orgTag, orgEntity) in SystemAPI.Query<RefRO<OrgTag>>()
                .WithEntityAccess())
            {
                if (!ownershipBufferLookup.HasBuffer(orgEntity))
                    continue;
                    
                var ownershipBuffer = ownershipBufferLookup[orgEntity];
                
                for (int i = 0; i < ownershipBuffer.Length; i++)
                {
                    var ownership = ownershipBuffer[i];
                    
                    // Check if this is an active acquisition (share < 1.0)
                    if (ownership.Share >= 1f)
                        continue;

                    // Check if owner org still exists
                    if (!SystemAPI.Exists(ownership.OwnerOrg))
                    {
                        ownershipBuffer.RemoveAt(i);
                        i--;
                        continue;
                    }

                    // Check relation to determine acquisition type
                    var relation = GetRelation(ref state, ownership.OwnerOrg, orgEntity);
                    if (!relation.HasValue)
                        continue;

                    // Hostile acquisition: gradual increase if attitude is negative
                    if (relation.Value.Attitude < 0f && relation.Value.Kind == OrgRelationKind.Hostile)
                    {
                        // Increase ownership share gradually
                        ownership.Share = math.min(ownership.Share + OWNERSHIP_INCREASE_RATE * OWNERSHIP_UPDATE_INTERVAL, 1f);
                        ownershipBuffer[i] = ownership;
                    }
                    // Mutual merger: faster increase if trust is high and treaties include IntegrationProcess
                    else if (relation.Value.Trust > 0.7f && 
                             (relation.Value.Treaties & OrgTreatyFlags.IntegrationProcess) != 0)
                    {
                        // Faster increase for mutual mergers
                        ownership.Share = math.min(ownership.Share + OWNERSHIP_INCREASE_RATE * OWNERSHIP_UPDATE_INTERVAL * 2f, 1f);
                        ownershipBuffer[i] = ownership;
                    }
                }
            }
        }

        private static OrgRelation? GetRelation(ref SystemState state, Entity orgA, Entity orgB)
        {
            var query = state.EntityManager.CreateEntityQuery(typeof(OrgRelation), typeof(OrgRelationTag));
            var relations = query.ToComponentDataArray<OrgRelation>(Allocator.Temp);
            var entities = query.ToEntityArray(Allocator.Temp);
            
            for (int i = 0; i < relations.Length; i++)
            {
                var relation = relations[i];
                if ((relation.OrgA == orgA && relation.OrgB == orgB) ||
                    (relation.OrgA == orgB && relation.OrgB == orgA))
                {
                    relations.Dispose();
                    entities.Dispose();
                    query.Dispose();
                    return relation;
                }
            }
            
            relations.Dispose();
            entities.Dispose();
            query.Dispose();
            return null;
        }
    }
}

