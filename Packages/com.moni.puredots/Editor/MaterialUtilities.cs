using UnityEditor;
using UnityEngine;

public static class MaterialUtilities
{
    public static void ApplyURPLit(string materialAssetPath, Color? baseColor = null, bool enableEmission = false, Color? emissionColor = null)
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(materialAssetPath);
        if (mat == null)
        {
            Debug.LogError($"Material not found at {materialAssetPath}");
            return;
        }
        var urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            Debug.LogError("URP/Lit shader not found in project.");
            return;
        }
        mat.shader = urpLit;
        if (baseColor.HasValue)
        {
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColor.Value);
            else if (mat.HasProperty("_Color")) mat.SetColor("_Color", baseColor.Value);
        }
        if (enableEmission)
        {
            mat.EnableKeyword("_EMISSION");
            Color e = emissionColor.HasValue ? emissionColor.Value : (baseColor.HasValue ? baseColor.Value : Color.white);
            if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", e);
        }
        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();
        Debug.Log($"Applied URP/Lit to {materialAssetPath}");
    }
}
