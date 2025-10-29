#if UNITY_EDITOR
using System.Collections.Generic;
using PureDOTS.Authoring;
using UnityEditor;
using UnityEngine;

namespace PureDOTS.Editor.Streaming
{
    public static class StreamingValidator
    {
        [MenuItem("PureDOTS/Streaming/Validate Sections")]
        private static void ValidateSections()
        {
            var issues = GatherIssues();

            if (issues.Count == 0)
            {
                EditorUtility.DisplayDialog("Streaming Validation", "No issues detected.", "OK");
            }
            else
            {
                var message = string.Join("\n", issues);
                Debug.LogWarning($"[Streaming Validator]\n{message}");
                EditorUtility.DisplayDialog("Streaming Validation", $"Found {issues.Count} issue(s). Check the Console for details.", "OK");
            }
        }

        public static List<string> GatherIssues()
        {
            var sections = Object.FindObjectsByType<StreamingSectionAuthoring>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var issues = new List<string>();

            // Missing GUIDs / SubScenes
            foreach (var section in sections)
            {
                if (section.subScene == null)
                {
                    issues.Add($"{FormatObject(section)} is missing a SubScene reference.");
                }
                else
                {
                    var guid = AssetDatabase.GUIDFromAssetPath(AssetDatabase.GetAssetPath(section.subScene.SceneAsset));
                    if (guid == default)
                    {
                        issues.Add($"{FormatObject(section)} references SubScene '{section.subScene.name}' without a valid GUID (not saved?).");
                    }
                }

                if (section.enterRadius <= 0f)
                {
                    issues.Add($"{FormatObject(section)} has non-positive Enter Radius.");
                }

                if (section.exitRadius < section.enterRadius)
                {
                    issues.Add($"{FormatObject(section)} Exit Radius is smaller than Enter Radius.");
                }
            }

            // Overlap detection
            for (int i = 0; i < sections.Length; i++)
            {
                var a = sections[i];
                var aCenter = a.useTransformCenter ? a.transform.position : a.manualCenter;
                for (int j = i + 1; j < sections.Length; j++)
                {
                    var b = sections[j];
                    var bCenter = b.useTransformCenter ? b.transform.position : b.manualCenter;
                    float distance = Vector3.Distance(aCenter, bCenter);
                    float minExit = Mathf.Min(a.exitRadius, b.exitRadius);
                    if (distance < minExit)
                    {
                        issues.Add($"Sections {FormatObject(a)} and {FormatObject(b)} overlap (distance {distance:F2} < min exit {minExit:F2}).");
                    }
                }
            }

            return issues;
        }

        private static string FormatObject(Object obj)
        {
            return obj != null ? $"'{obj.name}'" : "<null>";
        }
    }
}
#endif
