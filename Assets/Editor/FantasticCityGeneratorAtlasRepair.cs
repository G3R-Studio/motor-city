#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class FantasticCityGeneratorAtlasRepair
{
    private const string MaterialPath =
        "Assets/Resources/MotorCity/Environment/FCGMaterials/Atlas-1_";

    public static void Repair()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog(
                "Motor City — Atlas-1 Repair",
                "Останови Play Mode перед исправлением.",
                "OK");
            return;
        }

        string[] guids =
            AssetDatabase.FindAssets(
                "t:Material",
                new[]
                {
                    "Assets/Resources/MotorCity/Environment/FCGMaterials"
                });

        Material material =
            null;

        string path =
            string.Empty;

        foreach (string guid in guids)
        {
            string candidatePath =
                AssetDatabase.GUIDToAssetPath(
                    guid);

            Material candidate =
                AssetDatabase.LoadAssetAtPath<Material>(
                    candidatePath);

            if (candidate != null &&
                candidate.name == "FCG_Atlas-1")
            {
                material =
                    candidate;

                path =
                    candidatePath;

                break;
            }
        }

        if (material == null)
        {
            EditorUtility.DisplayDialog(
                "Motor City — Atlas-1 Repair",
                "FCG_Atlas-1 не найден.",
                "OK");
            return;
        }

        Shader urpLit =
            Shader.Find(
                "Universal Render Pipeline/Lit");

        if (urpLit == null)
        {
            EditorUtility.DisplayDialog(
                "Motor City — Atlas-1 Repair",
                "Universal Render Pipeline/Lit не найден.",
                "OK");
            return;
        }

        Undo.RecordObject(
            material,
            "Repair FCG Atlas-1 visibility");

        material.shader =
            urpLit;

        if (material.HasProperty("_Surface"))
            material.SetFloat("_Surface", 0f);

        if (material.HasProperty("_AlphaClip"))
            material.SetFloat("_AlphaClip", 0f);

        if (material.HasProperty("_Cutoff"))
            material.SetFloat("_Cutoff", 0f);

        if (material.HasProperty("_SrcBlend"))
            material.SetFloat("_SrcBlend", (float)BlendMode.One);

        if (material.HasProperty("_DstBlend"))
            material.SetFloat("_DstBlend", (float)BlendMode.Zero);

        if (material.HasProperty("_ZWrite"))
            material.SetFloat("_ZWrite", 1f);

        if (material.HasProperty("_Cull"))
            material.SetFloat("_Cull", (float)CullMode.Back);

        if (material.HasProperty("_BaseColor"))
        {
            Color color =
                material.GetColor(
                    "_BaseColor");

            color.a =
                1f;

            material.SetColor(
                "_BaseColor",
                color);
        }

        material.DisableKeyword(
            "_SURFACE_TYPE_TRANSPARENT");

        material.DisableKeyword(
            "_ALPHATEST_ON");

        material.SetOverrideTag(
            "RenderType",
            "Opaque");

        material.renderQueue =
            (int)RenderQueue.Geometry;

        material.enableInstancing =
            true;

        EditorUtility.SetDirty(
            material);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            "Motor City: repaired FCG_Atlas-1 opaque visibility. " +
            $"Material={path}, BaseMap=" +
            $"{(material.HasProperty("_BaseMap") && material.GetTexture("_BaseMap") != null ? material.GetTexture("_BaseMap").name : "<none>")}");

        EditorUtility.DisplayDialog(
            "Motor City — Atlas-1 Repair",
            "FCG_Atlas-1 переведён в гарантированно непрозрачный режим.\n\n" +
            "Текстура не менялась.\n" +
            "Теперь выполни Build Runtime City from Saved FCG City и проверь те же места.",
            "OK");
    }
}
#endif
