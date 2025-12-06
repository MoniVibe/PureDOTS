using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Core;
using PureDOTS.Runtime.AI;
using PureDOTS.Systems;

namespace PureDOTS.Systems.AI
{
    /// <summary>
    /// Perception fusion system (Body ECS) that outputs compressed feature vectors.
    /// Unifies sensor data (vision, sound, smell, radar) into shared semantic buffers.
    /// Burst-compiled fusion math for low-bandwidth AI perception.
    /// Event-driven: processes entities where sensor readings or weights changed.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(AISystemGroup))]
    [UpdateAfter(typeof(Systems.AI.VisionSystem))]
    [UpdateAfter(typeof(Systems.AI.SmellSystem))]
    [UpdateAfter(typeof(Systems.AI.HearingSystem))]
    [UpdateAfter(typeof(Systems.AI.RadarSystem))]
    public partial struct PerceptionFusionSystem : ISystem
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
            var rewindState = SystemAPI.GetSingleton<RewindState>();
            
            if (rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            // Fuse sensor data into compressed feature vectors
            var fusionQuery = state.GetEntityQuery(
                typeof(SensorReadingBuffer),
                typeof(SensorWeights),
                typeof(PerceptionFusionState));

            if (fusionQuery.IsEmpty)
            {
                return;
            }

            var job = new FusePerceptionJob
            {
                CurrentTick = tickState.Tick
            };

            state.Dependency = job.ScheduleParallel(fusionQuery, state.Dependency);
        }

        [BurstCompile]
        private partial struct FusePerceptionJob : IJobEntity
        {
            public uint CurrentTick;

            public void Execute(
                in DynamicBuffer<SensorReadingBuffer> sensorReadings,
                in SensorWeights weights,
                ref PerceptionFusionState fusionState,
                DynamicBuffer<PerceptionFeatureVector> featureVector)
            {
                // Clear previous features
                featureVector.Clear();

                // Fuse sensor readings into feature vectors
                // In full implementation, would:
                // 1. Read sensor readings from SensorReadingBuffer
                // 2. Apply sensor weights (VisionWeight, SoundWeight, etc.)
                // 3. Compute fused confidence scores
                // 4. Create PerceptionFeatureVector entries
                // 5. Apply smell bias and radar trust adjustments

                // Example fusion logic (simplified):
                for (int i = 0; i < sensorReadings.Length; i++)
                {
                    var reading = sensorReadings[i];
                    
                    // Map sensor type to weights
                    var confidence = reading.Confidence;
                    var visionScore = 0f;
                    var soundScore = 0f;
                    var smellScore = 0f;
                    var radarScore = 0f;
                    
                    switch (reading.SensorType)
                    {
                        case SensorType.Vision:
                            visionScore = confidence * weights.VisionWeight;
                            break;
                        case SensorType.Hearing:
                            soundScore = confidence * weights.SoundWeight;
                            break;
                        case SensorType.Smell:
                            smellScore = confidence * weights.SmellWeight * (1f + weights.SmellBias);
                            break;
                        case SensorType.Radar:
                            radarScore = confidence * weights.RadarWeight * weights.RadarTrust;
                            break;
                    }

                    // Combined confidence
                    var combinedConfidence = math.clamp(
                        visionScore + soundScore + smellScore + radarScore,
                        0f, 1f);

                    // Add feature if above threshold
                    if (combinedConfidence > 0.1f) // Threshold
                    {
                        featureVector.Add(new PerceptionFeatureVector
                        {
                            VisionScore = visionScore,
                            SoundScore = soundScore,
                            SmellScore = smellScore,
                            RadarScore = radarScore,
                            SourcePosition = reading.Position,
                            Confidence = combinedConfidence,
                            DetectionTick = CurrentTick
                        });
                    }
                }

                fusionState.LastFusionTick = CurrentTick;
                fusionState.FeatureCount = featureVector.Length;
            }
        }
    }
}

