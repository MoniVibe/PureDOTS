using UnityEngine;

namespace PureDOTS.Config
{
    /// <summary>
    /// Configuration data for cognitive layer tick rates and sync intervals.
    /// </summary>
    [System.Serializable]
    public struct CognitiveTickProfileData
    {
        [Tooltip("Default cognitive tick rate (Hz). Range: 1-5 Hz")]
        [Range(1f, 5f)]
        public float DefaultCognitiveTickRate;

        [Tooltip("Body → Mind sync interval in seconds (default: 0.1s = 100ms)")]
        [Range(0.01f, 1f)]
        public float BodyToMindSyncInterval;

        [Tooltip("Mind → Body sync interval in seconds (default: 0.25s = 250ms)")]
        [Range(0.01f, 1f)]
        public float MindToBodySyncInterval;

        [Tooltip("Maximum active cognitive agents per frame")]
        [Range(1000, 100000)]
        public int MaxActiveCognitiveAgentsPerFrame;

        [Tooltip("Performance budget for sync operations (ms per frame)")]
        [Range(0.1f, 10f)]
        public float SyncPerformanceBudgetMs;

        public static CognitiveTickProfileData CreateDefault()
        {
            return new CognitiveTickProfileData
            {
                DefaultCognitiveTickRate = 2f, // 2 Hz default
                BodyToMindSyncInterval = 0.1f, // 100ms
                MindToBodySyncInterval = 0.25f, // 250ms
                MaxActiveCognitiveAgentsPerFrame = 50000,
                SyncPerformanceBudgetMs = 3f // 3ms per frame target
            };
        }

        public void Clamp()
        {
            DefaultCognitiveTickRate = Mathf.Clamp(DefaultCognitiveTickRate, 1f, 5f);
            BodyToMindSyncInterval = Mathf.Clamp(BodyToMindSyncInterval, 0.01f, 1f);
            MindToBodySyncInterval = Mathf.Clamp(MindToBodySyncInterval, 0.01f, 1f);
            MaxActiveCognitiveAgentsPerFrame = Mathf.Clamp(MaxActiveCognitiveAgentsPerFrame, 1000, 100000);
            SyncPerformanceBudgetMs = Mathf.Clamp(SyncPerformanceBudgetMs, 0.1f, 10f);
        }
    }
}

