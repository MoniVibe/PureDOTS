using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace PureDOTS.Editor
{
    /// <summary>
    /// Tool for placing entities, modifying blobs, and triggering rewinds in the editor.
    /// Keeps presentation code isolated (P23/P24 rules).
    /// </summary>
    public class InWorldEntityPlacer : EditorWindow
    {
        [MenuItem("PureDOTS/In-World Entity Placer")]
        public static void ShowWindow()
        {
            GetWindow<InWorldEntityPlacer>("Entity Placer");
        }

        private void OnGUI()
        {
            GUILayout.Label("In-World Entity Placer", EditorStyles.boldLabel);

            if (GUILayout.Button("Place Entity"))
            {
                // Place entity at mouse position
            }

            if (GUILayout.Button("Modify Blob"))
            {
                // Open blob modification UI
            }

            if (GUILayout.Button("Trigger Rewind"))
            {
                // Trigger rewind to specific tick
            }
        }
    }
}

