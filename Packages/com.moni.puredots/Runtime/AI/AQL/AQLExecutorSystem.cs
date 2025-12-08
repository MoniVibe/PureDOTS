using PureDOTS.Runtime.Core;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.AI.AQL
{
    /// <summary>
    /// System that executes AQL queries and caches results.
    /// Integrates with MindECS systems for declarative cognition.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(AISystemGroup))]
    public partial struct AQLExecutorSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TickTimeState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var tickState = SystemAPI.GetSingleton<TickTimeState>();
            if (tickState.IsPaused)
            {
                return;
            }

            // Execute AQL queries against entities tagged with AQLTag.Name == query.EntityType.
            // Conditions are parsed but currently ignored; extend as needed.
            foreach (var (queries, results, entity) in SystemAPI.Query<DynamicBuffer<AQLQueryElement>, DynamicBuffer<AQLResultElement>>().WithEntityAccess())
            {
                results.Clear();

                if (queries.Length == 0)
                    continue;

                foreach (var queryElem in queries)
                {
                    var q = queryElem.Query;
                    if (q.EntityType.Length == 0)
                        continue;

                    foreach (var (tag, taggedEntity) in SystemAPI.Query<RefRO<AQLTag>>().WithEntityAccess())
                    {
                        if (tag.ValueRO.Name.Equals(q.EntityType))
                        {
                            results.Add(new AQLResultElement { Entity = taggedEntity });
                        }
                    }
                }
            }
        }
    }
}
