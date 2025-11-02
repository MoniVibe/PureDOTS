using PureDOTS.Runtime.Camera;
using PureDOTS.Runtime.Config;
using PureDOTS.Runtime.Debugging;
using PureDOTS.Systems;
using Unity.Entities;
using UnityEngine;

namespace PureDOTS.Runtime.Debugging
{
    [DisallowMultipleComponent]
    internal sealed class DiagnosticsOverlayBehaviour : MonoBehaviour
    {
        private Rect _windowRect = new Rect(20f, 20f, 360f, 260f);
        private bool _visible;

        private void Update()
        {
            _visible = DebugConfigVars.DiagnosticsOverlayEnabled != null && DebugConfigVars.DiagnosticsOverlayEnabled.BoolValue;
        }

        private void OnGUI()
        {
            if (!_visible)
                return;

            _windowRect = GUILayout.Window(GetInstanceID(), _windowRect, DrawWindow, "Runtime Diagnostics");
        }

        private void DrawWindow(int id)
        {
            GUILayout.Label($"Camera Mode: {(CameraRigService.IsEcsCameraEnabled ? "ECS" : "Hybrid")}");
            if (CameraRigService.HasState)
            {
                var state = CameraRigService.Current;
                GUILayout.Label($"Pos: {state.Position.x:F1}, {state.Position.y:F1}, {state.Position.z:F1}");
                GUILayout.Label($"Yaw: {Mathf.Rad2Deg * state.Yaw:F1}°  Pitch: {Mathf.Rad2Deg * state.Pitch:F1}°");
            }
            else
            {
                GUILayout.Label("Camera state unavailable");
            }

            var world = World.DefaultGameObjectInjectionWorld;
            if (world != null && world.IsCreated)
            {
                var entityManager = world.EntityManager;
                using var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<ManualPhaseControl>());
                if (!query.IsEmptyIgnoreFilter)
                {
                    var control = query.GetSingleton<ManualPhaseControl>();
                    GUILayout.Label($"Phases – Camera:{control.CameraPhaseEnabled} Transport:{control.TransportPhaseEnabled} History:{control.HistoryPhaseEnabled}");
                }
                else
                {
                    GUILayout.Label("Manual phase control not initialised");
                }
            }
            else
            {
                GUILayout.Label("World unavailable");
            }

            if (PhysicsHistoryCaptureSystem.Instance != null)
            {
                var instance = PhysicsHistoryCaptureSystem.Instance!;
                if (instance.TryCloneLatest(out var worldSnapshot, out var tick))
                {
                    GUILayout.Label($"Physics history latest tick: {tick}");
                    worldSnapshot.Dispose();
                }
                else
                {
                    GUILayout.Label("Physics history empty");
                }
            }
            else
            {
                GUILayout.Label("Physics history disabled");
            }

            GUILayout.Space(6f);
            GUILayout.Label("Commands: overlay show/hide/toggle, history dump");
            GUI.DragWindow();
        }
    }
}


