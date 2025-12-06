using DefaultEcs;
using DefaultEcs.System;
using PureDOTS.AI.AggregateECS.Components;
using PureDOTS.Runtime.Bridges;
using PureDOTS.Runtime.Components;
using PureDOTS.Shared;
using UnityEngine;

namespace PureDOTS.AI.AggregateECS.Systems
{
    /// <summary>
    /// Mission allocation system using marginal-return heuristics.
    /// Allocates limbs/modules where they yield most value.
    /// </summary>
    public class MissionAllocationSystem : AEntitySetSystem<float>
    {
        private float _lastUpdateTime;
        private const float UpdateInterval = 1.0f; // 1 Hz allocation updates
        private AgentSyncBus _syncBus;

        public MissionAllocationSystem(World world, AgentSyncBus syncBus) 
            : base(world.GetEntities().With<AggregateEntity>().AsSet())
        {
            _lastUpdateTime = 0f;
            _syncBus = syncBus;
        }

        protected override void Update(float deltaTime, in Entity entity)
        {
            var currentTime = Time.time;
            
            // Throttle updates (1 Hz)
            if (currentTime - _lastUpdateTime < UpdateInterval)
            {
                return;
            }

            if (!World.Has<AggregateEntity>(entity))
            {
                return;
            }

            var aggregate = World.Get<AggregateEntity>(entity);

            // Allocate missions using marginal-return heuristics
            // Evaluate per-tick "mission value delta" to prioritize CPU attention
            AllocateMissions(aggregate);

            _lastUpdateTime = currentTime;
        }

        private void AllocateMissions(AggregateEntity aggregate)
        {
            if (_syncBus == null || aggregate.MemberGuids.Count == 0)
            {
                return;
            }

            // Marginal-return heuristics:
            // 1. Calculate value per unit of resource/time for each available mission
            // 2. Allocate agents to missions with highest marginal return
            // 3. Consider agent capabilities (limbs/modules) when allocating
            // 4. Update mission value deltas for CPU prioritization

            // In full implementation:
            // - Read available missions from TaskNetwork
            // - Calculate marginal returns for each mission
            // - Assign agents to missions based on highest return
            // - Update MissionValueDelta components
        }
    }
}

