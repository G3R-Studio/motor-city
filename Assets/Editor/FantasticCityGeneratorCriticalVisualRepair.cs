#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class FantasticCityGeneratorCriticalVisualRepair
{
    private const string GeneratedRoot =
        "Assets/Resources/MotorCity/Environment/FCGMaterials";

    private const string SourceRoot =
        "Assets/Fantastic City Generator";

    private enum SurfaceMode
    {
        Opaque,
        Cutout,
        Transparent
    }

    private sealed class Rule
    {
        public string Name;
        public SurfaceMode Surface;
        public float Cutoff;
    }

    private static readonly Rule[] Rules =
    {
        new Rule { Name = "FCG_Atlas-1", Surface = SurfaceMode.Cutout, Cutoff = 0.12f },
        new Rule { Name = "FCG_Grass-01", Surface = SurfaceMode.Opaque },
        new Rule { Name = "FCG_Grass-Splat", Surface = SurfaceMode.Opaque },
        new Rule { Name = "FCG_HighWay", Surface = SurfaceMode.Opaque },
        new Rule { Name = "FCG_Wins", Surface = SurfaceMode.Opaque },
        new Rule { Name = "FCG_Wins-02", Surface = SurfaceMode.Opaque },
        new Rule { Name = "FCG_Glass-01", Surface = SurfaceMode.Transparent },
        new Rule { Name = "FCG_WinGlass-01", Surface = SurfaceMode.Transparent },
        new Rule { Name = "FCG_WinGlass-01-D", Surface = SurfaceMode.Transparent },
        new Rule { Name = "FCG_WinGlass-03", Surface = SurfaceMode.Transparent }
    };

    public static void Repair()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog(
                "Motor City — Critical Visual Repair",
                "Останови Play Mode перед исправлением материалов.",
                "OK");
            return;
        }

        Shader urpLit =
            Shader.Find("Universal Render Pipeline/Lit");

        if (urpLit == null)
        {
            EditorUtility.DisplayDialog(
                "Motor City — Critical Visual Repair",
                "Universal Render Pipeline/Lit не найден.",
                "OK");
            return;
        }

        Dictionary<string, Material> generated =
            LoadMaterials(GeneratedRoot);

        Dictionary<string, Material> source =
            LoadMaterials(SourceRoot);

        int repaired = 0;
        var details = new List<string>();
        var missing = new List<string>();

        foreach (Rule rule in Rules)
        {
            if (!generated.TryGetValue(rule.Name, out Material target) ||
                target == null)
            {
                missing.Add(rule.Name + " (generated)");
                continue;
            }

            string wantedSourceName =
                rule.Name.StartsWith("FCG_", StringComparison.OrdinalIgnoreCase)
                    ? rule.Name.Substring(4)
                    : rule.Name;

            Material original =
                FindSourceMaterial(source, wantedSourceName);

            if (original == null)
            {
                missing.Add(rule.Name + " (source)");
                continue;
            }

            Undo.RecordObject(
                target,
                "Repair recovered FCG visual material");

            target.shader =
                urpLit;

            Texture texture =
                FindBestBaseTexture(
                    original,
                    out Vector2 scale,
                    out Vector2 offset);

            if (texture != null &&
                target.HasProperty("_BaseMap"))
            {
                target.SetTexture("_BaseMap", texture);
                target.SetTextureScale("_BaseMap", scale);
                target.SetTextureOffset("_BaseMap", offset);
            }

            CopyBaseColor(
                original,
                target);

            ConfigureSurface(
                target,
                rule.Surface,
                rule.Cutoff);

            target.enableInstancing = true;

            EditorUtility.SetDirty(target);

            string texturePath =
                texture != null
                    ? AssetDatabase.GetAssetPath(texture)
                    : "<none>";

            details.Add(
                $"{rule.Name}: source={AssetDatabase.GetAssetPath(original)}, texture={texturePath}, mode={rule.Surface}");

            repaired++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string detailText =
            string.Join("\n", details);

        Debug.Log(
            "Motor City: critical recovered FCG visuals repaired.\n" +
            detailText +
            (missing.Count > 0
                ? "\nMissing: " + string.Join(", ", missing)
                : string.Empty));

        EditorUtility.DisplayDialog(
            "Motor City — Critical Visual Repair",
            "Готово.\n\n" +
            $"Исправлено материалов: {repaired}/{Rules.Length}\n" +
            $"Не найдено: {(missing.Count == 0 ? "0" : string.Join(", ", missing))}\n\n" +
            "Atlas-1 восстановлен как alpha-cutout (для стрелок/разметки), " +
            "земля/HighWay как opaque, стекло как transparent.\n" +
            "BaseMap заново привязан из оригинальных FCG-материалов.\n\n" +
            "Теперь выполни Build Runtime City from Saved FCG City.",
            "OK");
    }

    private static Dictionary<string, Material> LoadMaterials(
        string root)
    {
        var result =
            new Dictionary<string, Material>(
                StringComparer.OrdinalIgnoreCase);

        string[] guids =
            AssetDatabase.FindAssets(
                "t:Material",
                new[] { root });

        foreach (string guid in guids)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(guid);

            Material material =
                AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material == null ||
                string.IsNullOrWhiteSpace(material.name))
                continue;

            if (!result.ContainsKey(material.name))
                result.Add(material.name, material);

            string fileName =
                Path.GetFileNameWithoutExtension(path);

            if (!result.ContainsKey(fileName))
                result.Add(fileName, material);
        }

        return result;
    }

    private static Material FindSourceMaterial(
        Dictionary<string, Material> source,
        string wanted)
    {
        if (source.TryGetValue(wanted, out Material exact))
            return exact;

        string normalizedWanted =
            Normalize(wanted);

        foreach (KeyValuePair<string, Material> pair in source)
        {
            if (Normalize(pair.Key) ==
                normalizedWanted)
                return pair.Value;
        }

        return null;
    }

    private static string Normalize(
        string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        string text =
            value.Trim();

        if (text.StartsWith(
                "FCG_",
                StringComparison.OrdinalIgnoreCase))
        {
            text = text.Substring(4);
        }

        return new string(
            text.ToLowerInvariant()
                .Where(char.IsLetterOrDigit)
                .ToArray());
    }

    private static Texture FindBestBaseTexture(
        Material material,
        out Vector2 scale,
        out Vector2 offset)
    {
        scale = Vector2.one;
        offset = Vector2.zero;

        SerializedObject serialized =
            new SerializedObject(material);

        SerializedProperty texEnvs =
            serialized.FindProperty(
                "m_SavedProperties.m_TexEnvs");

        if (texEnvs == null ||
            !texEnvs.isArray)
            return null;

        Texture best =
            null;

        int bestScore =
            int.MinValue;

        string materialName =
            Normalize(material.name);

        bool grassSplat =
            materialName.Contains("grasssplat");

        for (int i = 0;
             i < texEnvs.arraySize;
             i++)
        {
            SerializedProperty entry =
                texEnvs.GetArrayElementAtIndex(i);

            SerializedProperty first =
                entry.FindPropertyRelative("first");

            SerializedProperty second =
                entry.FindPropertyRelative("second");

            if (first == null ||
                second == null)
                continue;

            SerializedProperty textureProperty =
                second.FindPropertyRelative("m_Texture");

            Texture candidate =
                textureProperty != null
                    ? textureProperty.objectReferenceValue as Texture
                    : null;

            if (candidate == null)
                continue;

            string propertyName =
                (first.stringValue ?? string.Empty)
                    .ToLowerInvariant();

            string textureName =
                (candidate.name ?? string.Empty)
                    .ToLowerInvariant();

            if (propertyName.Contains("normal") ||
                propertyName.Contains("bump") ||
                propertyName.Contains("mask") ||
                propertyName.Contains("metal") ||
                propertyName.Contains("spec") ||
                propertyName.Contains("occlusion") ||
                propertyName.Contains("emission"))
                continue;

            int score = 0;

            if (propertyName == "_basemap")
                score += 100;

            if (propertyName == "_maintex")
                score += grassSplat ? 15 : 85;

            if (propertyName.Contains("albedo"))
                score += 80;

            if (propertyName.Contains("diff"))
                score += 70;

            if (propertyName.Contains("base"))
                score += 60;

            if (grassSplat &&
                propertyName.Contains("splat"))
                score += 120;

            if (textureName.Contains("albedo") ||
                textureName.Contains("diff") ||
                textureName.Contains("color"))
                score += 25;

            if (grassSplat &&
                textureName.Contains("grass"))
                score += 35;

            string path =
                AssetDatabase.GetAssetPath(candidate);

            if (!string.IsNullOrWhiteSpace(path))
                score += 15;

            if (score <= bestScore)
                continue;

            bestScore = score;
            best = candidate;

            SerializedProperty scaleProperty =
                second.FindPropertyRelative("m_Scale");

            SerializedProperty offsetProperty =
                second.FindPropertyRelative("m_Offset");

            scale =
                scaleProperty != null
                    ? scaleProperty.vector2Value
                    : Vector2.one;

            offset =
                offsetProperty != null
                    ? offsetProperty.vector2Value
                    : Vector2.zero;
        }

        return best;
    }

    private static void CopyBaseColor(
        Material source,
        Material target)
    {
        Color color =
            Color.white;

        if (source.HasProperty("_BaseColor"))
            color = source.GetColor("_BaseColor");
        else if (source.HasProperty("_Color"))
            color = source.GetColor("_Color");

        color.a = 1f;

        if (target.HasProperty("_BaseColor"))
            target.SetColor("_BaseColor", color);
    }

    private static void ConfigureSurface(
        Material material,
        SurfaceMode mode,
        float cutoff)
    {
        material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.DisableKeyword("_ALPHATEST_ON");

        if (mode == SurfaceMode.Transparent)
        {
            if (material.HasProperty("_Surface"))
                material.SetFloat("_Surface", 1f);

            if (material.HasProperty("_AlphaClip"))
                material.SetFloat("_AlphaClip", 0f);

            if (material.HasProperty("_SrcBlend"))
                material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);

            if (material.HasProperty("_DstBlend"))
                material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);

            if (material.HasProperty("_ZWrite"))
                material.SetFloat("_ZWrite", 0f);

            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;
            return;
        }

        if (material.HasProperty("_Surface"))
            material.SetFloat("_Surface", 0f);

        if (material.HasProperty("_SrcBlend"))
            material.SetFloat("_SrcBlend", (float)BlendMode.One);

        if (material.HasProperty("_DstBlend"))
            material.SetFloat("_DstBlend", (float)BlendMode.Zero);

        if (material.HasProperty("_ZWrite"))
            material.SetFloat("_ZWrite", 1f);

        if (mode == SurfaceMode.Cutout)
        {
            if (material.HasProperty("_AlphaClip"))
                material.SetFloat("_AlphaClip", 1f);

            if (material.HasProperty("_Cutoff"))
                material.SetFloat("_Cutoff", cutoff);

            material.EnableKeyword("_ALPHATEST_ON");
            material.SetOverrideTag("RenderType", "TransparentCutout");
            material.renderQueue = (int)RenderQueue.AlphaTest;
            return;
        }

        if (material.HasProperty("_AlphaClip"))
            material.SetFloat("_AlphaClip", 0f);

        material.SetOverrideTag("RenderType", "Opaque");
        material.renderQueue = (int)RenderQueue.Geometry;
    }
}
#endif
