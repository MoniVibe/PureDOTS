using System.Linq;
using UnityEditor;
using UnityEngine;
using Unity.Scenes;
#if UNITY_EDITOR
using PureDOTS.Authoring;
using PureDOTS.Runtime.Components;

public static class ApplyPlaceholderVisuals
{
    [MenuItem("Tools/PureDOTS/Apply Placeholder Visuals (SubScene)")]
    public static void Execute()
    {
        // Ensure we operate on the active scene (expected to be the SubScene when called)
        var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        if (!scene.IsValid()) { Debug.LogError("Active scene invalid."); return; }

        // Helper to ensure a child primitive surrogate exists and returns its GameObject
        GameObject EnsureChild(string parentName, string childName, PrimitiveType type)
        {
            var parent = GameObject.Find(parentName);
            if (parent == null) return null;
            var child = parent.transform.Find(childName)?.gameObject;
            if (child == null)
            {
                child = GameObject.CreatePrimitive(type);
                child.name = childName;
                child.transform.SetParent(parent.transform, false);
            }
            return child;
        }

        // Ensure URP/Lit shader
        var urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            Debug.LogWarning("URP/Lit shader not found. Materials will keep current shader.");
        }

        // Villager surrogate (crate)
        var villager = EnsureChild("/Villager", "Surrogate_Villager", PrimitiveType.Cube);
        if (villager != null)
        {
            ApplyPVA(villager, PlaceholderVisualKind.Crate, 1.0f, Vector3.zero, new Color(0.3f,0.6f,1f,1f));
            EnsureURPLit(villager, urpLit);
        }

        // Resource barrels
        var barrel = EnsureChild("/ResourceNode", "Surrogate_Barrel", PrimitiveType.Cylinder);
        if (barrel != null)
        {
            ApplyPVA(barrel, PlaceholderVisualKind.Barrel, 1.0f, Vector3.zero, new Color(0.3f,0.9f,0.4f,1f));
            EnsureURPLit(barrel, urpLit);
        }

        // Storehouse crates
        var crates = EnsureChild("/Storehouse", "Surrogate_Crates", PrimitiveType.Cube);
        if (crates != null)
        {
            ApplyPVA(crates, PlaceholderVisualKind.Crate, 1.2f, new Vector3(0,0.25f,0), new Color(1.0f,0.6f,0.2f,1f));
            EnsureURPLit(crates, urpLit);
        }

        // Miracle sphere at root (if exists)
        var miracle = GameObject.Find("/Miracle") ?? GameObject.CreatePrimitive(PrimitiveType.Sphere);
        if (miracle != null)
        {
            miracle.name = "Miracle";
            if (miracle.transform.parent == null)
                miracle.transform.SetParent(null);
            ApplyPVA(miracle, PlaceholderVisualKind.Miracle, 0.7f, Vector3.zero, Color.white);
            var mr = miracle.GetComponent<Renderer>();
            if (mr != null)
            {
                EnsureURPLit(miracle, urpLit);
                foreach (var m in mr.sharedMaterials.Where(m=>m!=null))
                {
                    m.EnableKeyword("_EMISSION");
                    if (m.HasProperty("_EmissionColor"))
                        m.SetColor("_EmissionColor", new Color(0.8f,0.9f,1f,1f));
                }
            }
        }

        // Vegetation sprout at root (if exists)
        var veg = GameObject.Find("/Vegetation") ?? GameObject.CreatePrimitive(PrimitiveType.Capsule);
        if (veg != null)
        {
            veg.name = "Vegetation";
            ApplyPVA(veg, PlaceholderVisualKind.Vegetation, 0.6f, Vector3.zero, new Color(0.2f,0.8f,0.3f,1f));
            EnsureURPLit(veg, urpLit);
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        AssetDatabase.Refresh();
        Debug.Log("Applied PlaceholderVisualAuthoring settings and ensured URP/Lit + emission where needed.");
    }

    static void ApplyPVA(GameObject go, PlaceholderVisualKind kind, float baseScale, Vector3 offset, Color baseColor)
    {
        var pva = go.GetComponent<PlaceholderVisualAuthoring>();
        if (pva == null) pva = go.AddComponent<PlaceholderVisualAuthoring>();
        pva.kind = kind;
        pva.baseScale = baseScale;
        pva.localOffset = offset;
        pva.baseColor = baseColor;
    }

    static void EnsureURPLit(GameObject go, Shader urpLit)
    {
        var r = go.GetComponent<Renderer>();
        if (r == null) return;
        if (urpLit == null) return;
        foreach (var m in r.sharedMaterials.Where(m=>m!=null))
        {
            if (m.shader != urpLit)
            {
                m.shader = urpLit;
                EditorUtility.SetDirty(m);
            }
        }
    }
}
#endif
