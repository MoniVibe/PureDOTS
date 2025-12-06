using DefaultEcs;
using DefaultEcs.System;
using PureDOTS.AI.AggregateECS.Components;
using PureDOTS.Runtime.Bridges;
using PureDOTS.Shared;
using Unity.Mathematics;
using UnityEngine;

namespace PureDOTS.AI.AggregateECS.Systems
{
    /// <summary>
    /// DefaultEcs system that evaluates aggregate state and produces group-level goals.
    /// Runs at 1 Hz (configurable, slower than individual agent cognition).
    /// </summary>
    public class AggregateIntentSystem : AEntitySetSystem<float>
    {
        private float _lastUpdateTime;
        private const float UpdateInterval = 1.0f; // 1 Hz
        private AgentSyncBus _syncBus;

        // Thresholds for goal evaluation
        private const float FoodThreshold = 30f; // Below this, prioritize Harvest
        private const float DefenseThreshold = 40f; // Below this, prioritize Defend
        private const float MoraleThreshold = 50f; // Below this, prioritize Rest

        public AggregateIntentSystem(World world, AgentSyncBus syncBus) 
            : base(world.GetEntities().With<AggregateEntity>().With<AggregateIntent>().AsSet())
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

            if (!World.Has<AggregateEntity>(entity) || !World.Has<AggregateIntent>(entity))
            {
                return;
            }

            var aggregate = World.Get<AggregateEntity>(entity);
            var intent = World.Get<AggregateIntent>(entity);

            // Evaluate aggregate state and produce goals
            EvaluateAggregateIntent(aggregate, intent);

            // Get cooperation/competition weights from GroupGoalSystem if available
            float cooperationWeight = 0.5f;
            float competitionWeight = 0.3f;
            float resourcePriority = 0.6f;
            float threatLevel = 0f;

            // Try to get GroupGoal data (would be synced from Body ECS or calculated here)
            // For now, calculate based on aggregate stats
            if (stats.Morale > 60f && stats.Food > 50f)
            {
                cooperationWeight = 0.7f;
                competitionWeight = 0.2f;
            }
            else if (stats.Food < 30f || stats.Defense < 40f)
            {
                cooperationWeight = 0.3f;
                competitionWeight = 0.6f;
            }

            resourcePriority = math.clamp(1f - (stats.Food / 100f), 0f, 1f);
            threatLevel = math.clamp(1f - (stats.Defense / 100f), 0f, 1f);

            // Publish intent to AgentSyncBus
            if (_syncBus != null && intent.CurrentGoal != "Idle" && intent.Priority > 0f)
            {
                var message = new AggregateIntentMessage
                {
                    AggregateGuid = aggregate.AggregateGuid,
                    GoalType = intent.CurrentGoal,
                    Priority = intent.Priority,
                    TargetPosition = intent.TargetPosition,
                    DistributionRatios = new System.Collections.Generic.Dictionary<string, float>(intent.DistributionRatios),
                    CooperationWeight = cooperationWeight,
                    CompetitionWeight = competitionWeight,
                    ResourcePriority = resourcePriority,
                    ThreatLevel = threatLevel
                };

                _syncBus.EnqueueAggregateIntent(message);
            }

            _lastUpdateTime = currentTime;
        }

        private void EvaluateAggregateIntent(AggregateEntity aggregate, AggregateIntent intent)
        {
            var stats = aggregate.Stats;

            // Reset intent
            intent.CurrentGoal = "Idle";
            intent.Priority = 0f;
            intent.DistributionRatios.Clear();

            // Priority 1: Food crisis - Harvest goal
            if (stats.Food < FoodThreshold && stats.Population > 0)
            {
                intent.CurrentGoal = "Harvest";
                intent.Priority = math.clamp(1f - (stats.Food / FoodThreshold), 0.5f, 1f);
                intent.SetDistribution("Harvest", 0.7f);
                intent.SetDistribution("Defend", 0.2f);
                intent.SetDistribution("Rest", 0.1f);
                return;
            }

            // Priority 2: Defense crisis - Defend goal
            if (stats.Defense < DefenseThreshold && stats.Population > 0)
            {
                intent.CurrentGoal = "Defend";
                intent.Priority = math.clamp(1f - (stats.Defense / DefenseThreshold), 0.5f, 1f);
                intent.SetDistribution("Defend", 0.6f);
                intent.SetDistribution("Harvest", 0.3f);
                intent.SetDistribution("Rest", 0.1f);
                return;
            }

            // Priority 3: Morale crisis - Rest goal
            if (stats.Morale < MoraleThreshold && stats.Population > 0)
            {
                intent.CurrentGoal = "Rest";
                intent.Priority = math.clamp(1f - (stats.Morale / MoraleThreshold), 0.3f, 0.7f);
                intent.SetDistribution("Rest", 0.5f);
                intent.SetDistribution("Harvest", 0.3f);
                intent.SetDistribution("Defend", 0.2f);
                return;
            }

            // Default: Balanced distribution
            if (stats.Population > 0)
            {
                intent.CurrentGoal = "Patrol";
                intent.Priority = 0.3f;
                intent.SetDistribution("Harvest", 0.4f);
                intent.SetDistribution("Defend", 0.3f);
                intent.SetDistribution("Rest", 0.2f);
                intent.SetDistribution("Patrol", 0.1f);
            }
        }
    }
}

