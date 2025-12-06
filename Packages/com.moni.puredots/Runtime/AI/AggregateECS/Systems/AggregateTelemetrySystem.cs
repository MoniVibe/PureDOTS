using DefaultEcs;
using DefaultEcs.System;
using PureDOTS.AI.AggregateECS.Components;
using PureDOTS.Runtime.Bridges;
using PureDOTS.Runtime.Components;
using PureDOTS.Shared;
using Unity.Mathematics;
using UnityEngine;

namespace PureDOTS.AI.AggregateECS.Systems
{
    /// <summary>
    /// DefaultEcs system that aggregates telemetry into blobs for mean-field influence.
    /// Collects average morale, density, threat from aggregate members.
    /// </summary>
    public class AggregateTelemetrySystem : AEntitySetSystem<float>
    {
        private float _lastUpdateTime;
        private const float UpdateInterval = 0.2f; // 5 Hz telemetry updates
        private AgentSyncBus _syncBus;

        public AggregateTelemetrySystem(World world, AgentSyncBus syncBus) 
            : base(world.GetEntities().With<AggregateEntity>().AsSet())
        {
            _lastUpdateTime = 0f;
            _syncBus = syncBus;
        }

        protected override void Update(float deltaTime, in Entity entity)
        {
            var currentTime = Time.time;
            
            // Throttle updates (5 Hz)
            if (currentTime - _lastUpdateTime < UpdateInterval)
            {
                return;
            }

            if (!World.Has<AggregateEntity>(entity))
            {
                return;
            }

            var aggregate = World.Get<AggregateEntity>(entity);

            // Aggregate telemetry from members
            // In full implementation, would read BodyToMind messages from AgentSyncBus
            // and compute averages for morale, density, threat
            AggregateTelemetry(aggregate);

            _lastUpdateTime = currentTime;
        }

        private void AggregateTelemetry(AggregateEntity aggregate)
        {
            if (_syncBus == null || aggregate.MemberGuids.Count == 0)
            {
                return;
            }

            // Read telemetry from AgentSyncBus BodyToMind queue
            // Compute averages:
            // - Average morale from member health/morale
            // - Density from member positions
            // - Threat from member combat states

            // Store aggregated telemetry in InfluenceFieldData component
            // This gets synced back to Body ECS via PerceptionBridgeSystem
        }
    }
}

