using DefaultEcs;
using DefaultEcs.System;
using PureDOTS.AI.AggregateECS.Components;
using PureDOTS.Runtime.AI.Social;
using PureDOTS.Shared;
using Unity.Mathematics;
using UnityEngine;

namespace PureDOTS.AI.AggregateECS.Systems
{
    /// <summary>
    /// Group goal system for aggregates.
    /// Maintains GroupGoal components and modulates by personal profiles.
    /// Based on Pagliuca et al. (2023) goal balancing patterns.
    /// </summary>
    public class GroupGoalSystem : AEntitySetSystem<float>
    {
        private float _lastUpdateTime;
        private const float UpdateInterval = 1.0f; // 1 Hz (same as AggregateIntentSystem)

        public GroupGoalSystem(World world) 
            : base(world.GetEntities().With<AggregateEntity>().AsSet())
        {
            _lastUpdateTime = 0f;
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

            // GroupGoal is a struct component in Body ECS, not Mind ECS
            // This system would work with aggregate-level goal data
            // For now, we provide the evaluation logic structure

            // Evaluate group goals based on aggregate stats
            // Results would be stored in aggregate intent or synced to Body ECS
            EvaluateGroupGoal(aggregate);

            _lastUpdateTime = currentTime;
        }

        private void EvaluateGroupGoal(AggregateEntity aggregate)
        {
            var stats = aggregate.Stats;

            // Calculate cooperation/competition weights based on aggregate state
            // High morale and resources -> more cooperation
            // Low resources or high threat -> more competition

            // Calculate cooperation/competition weights
            // These would be stored in AggregateIntent or synced to Body ECS GroupGoal components
            float cooperationWeight;
            float competitionWeight;

            if (stats.Morale > 60f && stats.Food > 50f)
            {
                // Cooperative state
                cooperationWeight = 0.7f;
                competitionWeight = 0.2f;
            }
            else if (stats.Food < 30f || stats.Defense < 40f)
            {
                // Competitive/defensive state
                cooperationWeight = 0.3f;
                competitionWeight = 0.6f;
            }
            else
            {
                // Balanced state
                cooperationWeight = 0.5f;
                competitionWeight = 0.3f;
            }

            // Store in AggregateIntent if available
            if (World.Has<AggregateIntent>(entity))
            {
                var intent = World.Get<AggregateIntent>(entity);
                // Intent would be updated with cooperation/competition weights
                // This will be integrated with AggregateIntentSystem
            }
        }

        /// <summary>
        /// Samples group goal and modulates by personal profile.
        /// EffectiveCoop = Group.CooperationWeight * Personality.Altruism
        /// </summary>
        public static float SampleEffectiveCooperation(GroupGoal groupGoal, float personalityAltruism)
        {
            return groupGoal.CooperationWeight * personalityAltruism;
        }

        /// <summary>
        /// Samples group goal and modulates by personal profile.
        /// EffectiveComp = Group.CompetitionWeight * Personality.Ambition
        /// </summary>
        public static float SampleEffectiveCompetition(GroupGoal groupGoal, float personalityAmbition)
        {
            return groupGoal.CompetitionWeight * personalityAmbition;
        }
    }
}

