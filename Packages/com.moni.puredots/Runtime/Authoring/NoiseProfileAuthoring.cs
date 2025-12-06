using PureDOTS.Runtime.Components;
using Unity.Entities;
using UnityEngine;

namespace PureDOTS.Authoring
{
    /// <summary>
    /// Authoring component for noise profiles.
    /// Creates NoiseProfile components for communication noise simulation.
    /// </summary>
    public class NoiseProfileAuthoring : MonoBehaviour
    {
        [Tooltip("Base message loss rate (0-1)")]
        [Range(0f, 1f)]
        public float lossRate = 0.1f;

        [Tooltip("Mean jitter in seconds")]
        [Min(0f)]
        public float jitterMean = 0.05f;

        [Tooltip("Standard deviation of jitter")]
        [Min(0f)]
        public float jitterStdDev = 0.02f;

        [Tooltip("Signal decay rate per unit distance (0-1)")]
        [Range(0f, 1f)]
        public float signalDecayRate = 0.01f;

        [Tooltip("Maximum communication distance")]
        [Min(0f)]
        public float maxDistance = 100f;

        [Tooltip("Unique profile identifier")]
        public uint profileId = 0;
    }

    /// <summary>
    /// Baker for NoiseProfileAuthoring.
    /// </summary>
    public class NoiseProfileBaker : Baker<NoiseProfileAuthoring>
    {
        public override void Bake(NoiseProfileAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new NoiseProfile
            {
                LossRate = authoring.lossRate,
                JitterMean = authoring.jitterMean,
                JitterStdDev = authoring.jitterStdDev,
                SignalDecayRate = authoring.signalDecayRate,
                MaxDistance = authoring.maxDistance,
                ProfileId = authoring.profileId
            });
        }
    }
}

