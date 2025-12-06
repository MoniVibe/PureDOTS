using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using PureDOTS.AI.AggregateECS;
using PureDOTS.AI.AggregateECS.Components;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Core;
using PureDOTS.Shared;
using DefaultEcs;

namespace PureDOTS.Runtime.Bridges
{
    /// <summary>
    /// Managed system that collects aggregate statistics from Body ECS and updates AggregateECSWorld.
    /// Runs at 1 Hz (configurable, slower than Mind ECS).
    /// </summary>
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    [UpdateAfter(typeof(VillagerSystemGroup))]
    public sealed partial class AggregateBridgeSystem : SystemBase
    {
        private float _lastSyncTime;
        private const float DefaultSyncInterval = 1.0f; // 1 Hz

        protected override void OnCreate()
        {
            _lastSyncTime = 0f;
            RequireForUpdate<TickTimeState>();
            
            // Initialize AggregateECSWorld if not already initialized
            var bus = AgentSyncBridgeCoordinator.GetBusFromDefaultWorld();
            if (bus != null)
            {
                AggregateECSWorld.Initialize(bus);
            }
            else
            {
                _ = AggregateECSWorld.Instance;
            }
        }

        protected override void OnUpdate()
        {
            var currentTime = (float)SystemAPI.Time.ElapsedTime;
            
            if (currentTime - _lastSyncTime < DefaultSyncInterval)
            {
                return;
            }

            var aggregateWorld = AggregateECSWorld.Instance;
            if (aggregateWorld == null)
            {
                return;
            }

            var tickState = SystemAPI.GetSingleton<TickTimeState>();

            // Collect aggregate statistics from Body ECS
            var entityQuery = GetEntityQuery(
                ComponentType.ReadOnly<AggregateMembership>(),
                ComponentType.ReadOnly<AgentSyncId>(),
                ComponentType.ReadOnly<LocalTransform>());

            if (entityQuery.IsEmpty)
            {
                _lastSyncTime = currentTime;
                return;
            }

            // Group agents by aggregate GUID and collect stats
            var aggregateStatsMap = new Dictionary<AgentGuid, AggregateStatsData>();

            var needsLookup = GetComponentLookup<VillagerNeeds>(true);
            needsLookup.Update(this);

            // Collect stats (managed operation - can't use Burst for Dictionary)
            var membershipArray = entityQuery.ToComponentDataArray<AggregateMembership>(Allocator.TempJob);
            var syncIdArray = entityQuery.ToComponentDataArray<AgentSyncId>(Allocator.TempJob);
            var transformArray = entityQuery.ToComponentDataArray<LocalTransform>(Allocator.TempJob);
            var entityArray = entityQuery.ToEntityArray(Allocator.TempJob);

            // Process stats (managed operation)
            for (int i = 0; i < membershipArray.Length; i++)
            {
                var membership = membershipArray[i];
                var syncId = syncIdArray[i];
                var transform = transformArray[i];
                var entity = entityArray[i];

                if (!aggregateStatsMap.ContainsKey(membership.AggregateGuid))
                {
                    aggregateStatsMap[membership.AggregateGuid] = new AggregateStatsData
                    {
                        MemberGuids = new List<AgentGuid>(),
                        FoodSum = 0f,
                        MoraleSum = 0f,
                        HealthSum = 0f,
                        EnergySum = 0f,
                        PositionSum = float3.zero,
                        MemberCount = 0
                    };
                }

                var stats = aggregateStatsMap[membership.AggregateGuid];
                stats.MemberGuids.Add(syncId.Guid);
                stats.PositionSum += transform.Position;
                stats.MemberCount++;

                // Get needs if available
                if (needsLookup.HasComponent(entity))
                {
                    var needs = needsLookup[entity];
                    stats.FoodSum += needs.HungerFloat;
                    stats.MoraleSum += needs.MoraleFloat;
                    stats.HealthSum += needs.Health;
                    stats.EnergySum += needs.EnergyFloat;
                }

                aggregateStatsMap[membership.AggregateGuid] = stats;
            }

            entityArray.Dispose();

            membershipArray.Dispose();
            syncIdArray.Dispose();
            transformArray.Dispose();

            // Update AggregateECSWorld entities
            UpdateAggregateWorld(aggregateWorld.World, aggregateStatsMap);

            _lastSyncTime = currentTime;
        }

        private void UpdateAggregateWorld(DefaultEcs.World aggregateWorld, Dictionary<AgentGuid, AggregateStatsData> statsMap)
        {
            foreach (var kvp in statsMap)
            {
                var aggregateGuid = kvp.Key;
                var statsData = kvp.Value;

                // Find or create aggregate entity
                DefaultEcs.Entity aggregateEntity = default;
                bool found = false;

                foreach (var entity in aggregateWorld.GetEntities().With<AggregateEntity>().AsSet().GetEntities())
                {
                    var aggregate = aggregateWorld.Get<AggregateEntity>(entity);
                    if (aggregate.AggregateGuid.Equals(aggregateGuid))
                    {
                        aggregateEntity = entity;
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    // Create new aggregate entity
                    aggregateEntity = aggregateWorld.CreateEntity();
                    var aggregate = new AggregateEntity
                    {
                        AggregateGuid = aggregateGuid,
                        Type = AggregateType.Village // Default type, can be configured
                    };
                    aggregateWorld.Set(aggregateEntity, aggregate);
                    aggregateWorld.Set(aggregateEntity, new AggregateIntent());
                }

                // Update aggregate entity
                var aggregateComp = aggregateWorld.Get<AggregateEntity>(aggregateEntity);
                
                // Update member GUIDs
                aggregateComp.MemberGuids.Clear();
                aggregateComp.MemberGuids.AddRange(statsData.MemberGuids);

                // Calculate average stats
                if (statsData.MemberCount > 0)
                {
                    aggregateComp.Stats = new AggregateStats
                    {
                        Food = statsData.FoodSum / statsData.MemberCount,
                        Morale = statsData.MoraleSum / statsData.MemberCount,
                        Health = statsData.HealthSum / statsData.MemberCount,
                        Energy = statsData.EnergySum / statsData.MemberCount,
                        Defense = statsData.HealthSum / statsData.MemberCount * 0.5f + statsData.MemberCount * 10f, // Simplified defense calculation
                        Population = statsData.MemberCount
                    };

                    // Calculate center position
                    aggregateComp.CenterPosition = statsData.PositionSum / statsData.MemberCount;
                }

                aggregateWorld.Set(aggregateEntity, aggregateComp);
            }
        }

        private struct AggregateStatsData
        {
            public List<AgentGuid> MemberGuids;
            public float FoodSum;
            public float MoraleSum;
            public float HealthSum;
            public float EnergySum;
            public float3 PositionSum;
            public int MemberCount;
        }
    }
}

