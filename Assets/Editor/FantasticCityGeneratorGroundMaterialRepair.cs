#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class FantasticCityGeneratorGroundMaterialRepair
{
    private const string MaterialRoot =
        "Assets/Resources/MotorCity/Environment/FCGMaterials";

    private static readonly string[] GroundMaterialNames =
    {
        "FCG_Grade1",
        "FCG_Grade2",
        "FCG_Grass-01",
        "FCG_Grass-Splat"
    };

    public static void RepairInvisibleGroundPatches()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog(
                "Motor City — Ground Patch Repair",
                "Останови Play Mode перед восстановлением ground-материалов.",
                "OK");
            return;
        }

        Shader urpLit =
            Shader.Find(
                "Universal Render Pipeline/Lit");

        if (urpLit == null)
        {
            EditorUtility.DisplayDialog(
                "Motor City — Ground Patch Repair",
                "Shader Universal Render Pipeline/Lit не найден.",
                "OK");
            return;
        }

        string[] guids =
            AssetDatabase.FindAssets(
                "t:Material",
                new[]
                {
                    MaterialRoot
                });

        var repaired =
            new List<string>();

        foreach (string guid in guids)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(
                    guid);

            Material material =
                AssetDatabase.LoadAssetAtPath<Material>(
                    path);

            if (material == null ||
                Array.IndexOf(
                    GroundMaterialNames,
                    material.name) < 0)
                continue;

            Undo.RecordObject(
                material,
                "Repair invisible FCG ground patches");

            // Preserve the recovered textures exactly. Only restore the
            // rendering state required for opaque ground/fill submeshes.
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

            repaired.Add(
                material.name);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string summary =
            repaired.Count > 0
                ? string.Join(
                    ", ",
                    repaired)
                : "ничего";

        Debug.Log(
            "Motor City: repaired opaque rendering state for FCG ground materials: " +
            summary);

        EditorUtility.DisplayDialog(
            "Motor City — Ground Patch Repair",
            "Готово.\n\n" +
            "Исправлены только параметры прозрачности/рендеринга у:\n" +
            summary +
            "\n\nТекстуры и их ссылки не менялись.\n" +
            "Теперь заново выполни Build Runtime City from Saved FCG City.",
            "OK");
    }
}
#endif
