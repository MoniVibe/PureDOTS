using PureDOTS.Runtime.AI.Cognitive;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace PureDOTS.Authoring.Cognitive
{
    /// <summary>
    /// Authoring component for CognitiveStats. Allows setting Intelligence, Wisdom, Curiosity, and Focus in the Unity Inspector.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CognitiveStatsAuthoring : MonoBehaviour
    {
        [Header("Cognitive Statistics (0-10 range)")]
        [Tooltip("Intelligence: computational efficiency, problem-solving rate. Higher = faster learning, more planning depth.")]
        [Range(0f, 10f)]
        public float intelligence = 5f;

        [Tooltip("Wisdom: integrative, experience-based reasoning. Higher = better retention, generalization, bias correction.")]
        [Range(0f, 10f)]
        public float wisdom = 5f;

        [Tooltip("Curiosity: exploration weight. Higher = more exploration, random affordance tests.")]
        [Range(0f, 10f)]
        public float curiosity = 5f;

        [Tooltip("Focus: current cognitive stamina. Decays during heavy reasoning, regenerates when idle.")]
        [Range(0f, 10f)]
        public float focus = 10f;

        [Tooltip("Maximum focus value. Used for fatigue calculations.")]
        [Range(0f, 10f)]
        public float maxFocus = 10f;
    }

    /// <summary>
    /// Baker for CognitiveStatsAuthoring. Creates CognitiveStats component from authoring data.
    /// </summary>
    public sealed class CognitiveStatsBaker : Baker<CognitiveStatsAuthoring>
    {
        public override void Bake(CognitiveStatsAuthoring authoring)
        {
            var entity = GetEntity(authoring, TransformUsageFlags.None);

            AddComponent(entity, new CognitiveStats
            {
                Intelligence = math.clamp(authoring.intelligence, 0f, 10f),
                Wisdom = math.clamp(authoring.wisdom, 0f, 10f),
                Curiosity = math.clamp(authoring.curiosity, 0f, 10f),
                Focus = math.clamp(authoring.focus, 0f, authoring.maxFocus),
                MaxFocus = math.max(0.1f, math.clamp(authoring.maxFocus, 0f, 10f)),
                LastFocusDecayTick = 0
            });
        }
    }
}

