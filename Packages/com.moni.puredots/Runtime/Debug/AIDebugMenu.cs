using UnityEngine;

namespace PureDOTS.Runtime.Debug
{
    /// <summary>
    /// MonoBehaviour for toggling AI debug visualizations.
    /// Editor-only, disabled in release builds.
    /// </summary>
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public class AIDebugMenu : MonoBehaviour
    {
        public bool ShowPerceptionRanges = false;
        public bool ShowFlowfieldGradients = false;
        public bool ShowDecisionHeatmaps = false;

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label("AI Debug Overlay");
            ShowPerceptionRanges = GUILayout.Toggle(ShowPerceptionRanges, "Show Perception Ranges");
            ShowFlowfieldGradients = GUILayout.Toggle(ShowFlowfieldGradients, "Show Flowfield Gradients");
            ShowDecisionHeatmaps = GUILayout.Toggle(ShowDecisionHeatmaps, "Show Decision Heatmaps");
            GUILayout.EndArea();
        }
    }
#endif
}

