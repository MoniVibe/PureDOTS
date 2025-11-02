using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using PureDOTS.Authoring;
using PureDOTS.Runtime.Components;

#if UNITY_EDITOR
public static class PlaceholderVisualUtility
{
    private const string PlaceholderChildName = "__PlaceholderVisual";
    private static readonly Dictionary<(Color color, bool emission), Material> s_materialCache = new();

    public static GameObject EnsurePlaceholderVisual(GameObject parent, PlaceholderVisualAuthoring authoring, Shader preferredShader = null)
    {
        if (parent == null || authoring == null)
        {
            return null;
        }

        var visualRoot = parent.transform.Find(PlaceholderChildName)?.gameObject;
        if (visualRoot == null)
        {
            visualRoot = GameObject.CreatePrimitive(GetPrimitiveType(authoring.kind));
            visualRoot.name = PlaceholderChildName;
            visualRoot.transform.SetParent(parent.transform, false);

            var collider = visualRoot.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }
        }

        ApplyAppearance(visualRoot, authoring, preferredShader);
        return visualRoot;
    }

    private static void ApplyAppearance(GameObject visualObject, PlaceholderVisualAuthoring authoring, Shader preferredShader)
    {
        if (visualObject == null)
        {
            return;
        }

        visualObject.transform.localPosition = authoring.localOffset;
        visualObject.transform.localRotation = Quaternion.identity;
        visualObject.transform.localScale = GetScaleForKind(authoring);

        var meshFilter = visualObject.GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            meshFilter = visualObject.AddComponent<MeshFilter>();
        }

        var meshRenderer = visualObject.GetComponent<MeshRenderer>();
        if (meshRenderer == null)
        {
            meshRenderer = visualObject.AddComponent<MeshRenderer>();
        }

        if (meshFilter.sharedMesh == null)
        {
            var primitive = GameObject.CreatePrimitive(GetPrimitiveType(authoring.kind));
            var primitiveFilter = primitive.GetComponent<MeshFilter>();
            if (primitiveFilter != null)
            {
                meshFilter.sharedMesh = primitiveFilter.sharedMesh;
            }
            Object.DestroyImmediate(primitive);
        }

        var material = GetPlaceholderMaterial(authoring, preferredShader);
        meshRenderer.sharedMaterial = material;

        if (authoring.kind == PlaceholderVisualKind.Miracle && material != null)
        {
            material.EnableKeyword("_EMISSION");
        }
    }

    private static PrimitiveType GetPrimitiveType(PlaceholderVisualKind kind)
    {
        return kind switch
        {
            PlaceholderVisualKind.Barrel => PrimitiveType.Cylinder,
            PlaceholderVisualKind.Vegetation => PrimitiveType.Capsule,
            PlaceholderVisualKind.Miracle => PrimitiveType.Sphere,
            _ => PrimitiveType.Cube
        };
    }

    private static Vector3 GetScaleForKind(PlaceholderVisualAuthoring authoring)
    {
        var baseScale = authoring.baseScale <= 0f ? 1f : authoring.baseScale;
        return authoring.kind switch
        {
            PlaceholderVisualKind.Barrel => new Vector3(baseScale * 0.9f, baseScale * 1.2f, baseScale * 0.9f),
            PlaceholderVisualKind.Vegetation => new Vector3(baseScale * 0.8f, baseScale * 1.6f, baseScale * 0.8f),
            PlaceholderVisualKind.Miracle => Vector3.one * baseScale * 0.85f,
            _ => Vector3.one * baseScale
        };
    }

    private static Material GetPlaceholderMaterial(PlaceholderVisualAuthoring authoring, Shader preferredShader)
    {
        var key = (authoring.baseColor, authoring.kind == PlaceholderVisualKind.Miracle);
        if (s_materialCache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var material = TryLoadPresetMaterial(authoring.kind, authoring.baseColor) ?? CreateRuntimeMaterial(authoring.baseColor, authoring.kind == PlaceholderVisualKind.Miracle, preferredShader);
        s_materialCache[key] = material;
        return material;
    }

    private static Material TryLoadPresetMaterial(PlaceholderVisualKind kind, Color baseColor)
    {
        string path = kind switch
        {
            PlaceholderVisualKind.Barrel => "Assets/Materials/Placeholders/Resource_Barrel.mat",
            PlaceholderVisualKind.Vegetation => "Assets/Materials/Placeholders/Vegetation_Sprout.mat",
            PlaceholderVisualKind.Miracle => "Assets/Materials/Placeholders/Miracle_Glow.mat",
            PlaceholderVisualKind.Crate => SelectCrateMaterialPath(baseColor),
            _ => null
        };

        return !string.IsNullOrEmpty(path)
            ? AssetDatabase.LoadAssetAtPath<Material>(path)
            : null;
    }

    private static string SelectCrateMaterialPath(Color baseColor)
    {
        static bool Approximately(Color a, Color b)
        {
            const float tolerance = 0.02f;
            return Mathf.Abs(a.r - b.r) <= tolerance &&
                   Mathf.Abs(a.g - b.g) <= tolerance &&
                   Mathf.Abs(a.b - b.b) <= tolerance;
        }

        var storehouseColor = new Color(0.75f, 0.55f, 0.3f, 1f);
        var villagerColor = new Color(0.85f, 0.6f, 0.25f, 1f);

        if (Approximately(baseColor, storehouseColor))
        {
            return "Assets/Materials/Placeholders/Storehouse_Crate.mat";
        }

        if (Approximately(baseColor, villagerColor))
        {
            return "Assets/Materials/Placeholders/Villager_Crate.mat";
        }

        return "Assets/Materials/Placeholders/Villager_Crate.mat";
    }

    private static Material CreateRuntimeMaterial(Color color, bool emission, Shader preferredShader)
    {
        var shader = preferredShader ?? Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
        var material = shader != null ? new Material(shader) : new Material(Shader.Find("Sprites/Default"));
        material.hideFlags = HideFlags.DontSave;

        material.color = color;
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (emission)
        {
            var emissionColor = color * 1.5f;
            material.EnableKeyword("_EMISSION");
            if (material.HasProperty("_EmissionColor"))
            {
                material.SetColor("_EmissionColor", emissionColor);
            }
        }

        return material;
    }
}
#endif


