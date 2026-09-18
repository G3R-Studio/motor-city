#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class FantasticCityGeneratorUrpFixer
{
    private const string FcgRoot =
        "Assets/Fantastic City Generator/";

    private const string RuntimeRoot =
        "Assets/Resources/MotorCity/Environment";

    private const string MaterialRoot =
        RuntimeRoot + "/FCGMaterials";

    [MenuItem("Motor City/Fantastic City Generator/Fix Pink Materials in Active Scene")]
    public static void FixPinkMaterialsInActiveScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog(
                "Motor City — FCG URP Fix",
                "Останови Play Mode перед конвертацией материалов.",
                "OK");
            return;
        }

        Shader urpLit =
            Shader.Find(
                "Universal Render Pipeline/Lit");

        if (urpLit == null)
        {
            EditorUtility.DisplayDialog(
                "Motor City — FCG URP Fix",
                "Shader Universal Render Pipeline/Lit не найден.",
                "OK");
            return;
        }

        Scene scene =
            SceneManager.GetActiveScene();

        if (!scene.IsValid() ||
            !scene.isLoaded)
        {
            EditorUtility.DisplayDialog(
                "Motor City — FCG URP Fix",
                "Нет активной загруженной сцены.",
                "OK");
            return;
        }

        Renderer[] renderers =
            scene
                .GetRootGameObjects()
                .SelectMany(
                    root =>
                        root.GetComponentsInChildren<Renderer>(
                            true))
                .Where(renderer => renderer != null)
                .ToArray();

        var sourceMaterials =
            new HashSet<Material>();

        foreach (Renderer renderer in renderers)
        {
            foreach (Material material in
                     renderer.sharedMaterials)
            {
                if (material == null)
                    continue;

                if (IsGeneratedUrpMaterial(
                        material))
                    continue;

                if (BelongsToFantasticCityGenerator(
                        material))
                {
                    sourceMaterials.Add(
                        material);
                }
            }
        }

        if (sourceMaterials.Count == 0)
        {
            EditorUtility.DisplayDialog(
                "Motor City — FCG URP Fix",
                "В активной сцене не найдено материалов Fantastic City Generator. " +
                "Сначала сгенерируй город.",
                "OK");
            return;
        }

        EnsureFolder(
            RuntimeRoot);

        EnsureFolder(
            MaterialRoot);

        var converted =
            new Dictionary<Material, Material>();

        try
        {
            int materialIndex =
                0;

            foreach (Material source in
                     sourceMaterials)
            {
                EditorUtility.DisplayProgressBar(
                    "Motor City — FCG URP Fix",
                    "Конвертация " +
                    source.name,
                    sourceMaterials.Count > 0
                        ? materialIndex /
                          (float)sourceMaterials.Count
                        : 1f);

                Material runtime =
                    CreateOrUpdateUrpMaterial(
                        source,
                        urpLit,
                        materialIndex);

                converted[source] =
                    runtime;

                materialIndex++;
            }

            int changedRenderers =
                0;

            foreach (Renderer renderer in renderers)
            {
                Material[] materials =
                    renderer.sharedMaterials;

                bool changed =
                    false;

                for (int i = 0;
                     i < materials.Length;
                     i++)
                {
                    Material source =
                        materials[i];

                    if (source == null)
                        continue;

                    if (!converted.TryGetValue(
                            source,
                            out Material runtime))
                        continue;

                    materials[i] =
                        runtime;

                    changed =
                        true;
                }

                if (!changed)
                    continue;

                Undo.RecordObject(
                    renderer,
                    "Fix FCG URP Materials");

                renderer.sharedMaterials =
                    materials;

                EditorUtility.SetDirty(
                    renderer);

                changedRenderers++;
            }

            AssetDatabase.SaveAssets();

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                scene);

            Debug.Log(
                "Motor City: Fantastic City Generator URP conversion complete. " +
                $"Converted {converted.Count} materials and updated " +
                $"{changedRenderers} renderers.");

            EditorUtility.DisplayDialog(
                "Motor City — FCG URP Fix",
                "Готово.\n\n" +
                $"Материалов конвертировано: {converted.Count}\n" +
                $"Renderer'ов обновлено: {changedRenderers}\n\n" +
                "Сохрани сцену (Ctrl+S).",
                "OK");
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "Motor City: FCG URP material conversion failed. " +
                exception);

            EditorUtility.DisplayDialog(
                "Motor City — FCG URP Fix",
                "Конвертация завершилась ошибкой. " +
                "Посмотри Console / Editor.log.",
                "OK");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    [MenuItem("Motor City/Fantastic City Generator/Diagnose Materials in Active Scene")]
    public static void DiagnoseMaterialsInActiveScene()
    {
        Scene scene =
            SceneManager.GetActiveScene();

        if (!scene.IsValid() ||
            !scene.isLoaded)
            return;

        var counts =
            new Dictionary<string, int>(
                StringComparer.OrdinalIgnoreCase);

        int fcgRendererCount =
            0;

        foreach (Renderer renderer in
                 scene
                     .GetRootGameObjects()
                     .SelectMany(
                         root =>
                             root.GetComponentsInChildren<Renderer>(
                                 true)))
        {
            bool usesFcg =
                false;

            foreach (Material material in
                     renderer.sharedMaterials)
            {
                if (material == null)
                    continue;

                if (!BelongsToFantasticCityGenerator(
                        material) &&
                    !IsGeneratedUrpMaterial(
                        material))
                    continue;

                usesFcg =
                    true;

                string shaderName =
                    material.shader != null
                        ? material.shader.name
                        : "<missing shader>";

                counts.TryGetValue(
                    shaderName,
                    out int count);

                counts[shaderName] =
                    count + 1;
            }

            if (usesFcg)
                fcgRendererCount++;
        }

        string summary =
            string.Join(
                "\n",
                counts
                    .OrderByDescending(pair => pair.Value)
                    .Select(
                        pair =>
                            $"{pair.Key}: {pair.Value}"));

        Debug.Log(
            "Motor City: FCG material diagnostic. " +
            $"Renderers={fcgRendererCount}\n{summary}");

        EditorUtility.DisplayDialog(
            "Motor City — FCG Material Diagnostic",
            $"FCG Renderer'ов: {fcgRendererCount}\n\n{summary}",
            "OK");
    }

    private static bool BelongsToFantasticCityGenerator(
        Material material)
    {
        if (material == null)
            return false;

        string materialPath =
            AssetDatabase.GetAssetPath(
                material);

        if (IsFcgPath(
                materialPath))
            return true;

        Shader shader =
            material.shader;

        if (shader != null &&
            IsFcgPath(
                AssetDatabase.GetAssetPath(
                    shader)))
            return true;

        string[] textureProperties;

        try
        {
            textureProperties =
                material.GetTexturePropertyNames();
        }
        catch
        {
            return false;
        }

        foreach (string property in
                 textureProperties)
        {
            Texture texture;

            try
            {
                texture =
                    material.GetTexture(
                        property);
            }
            catch
            {
                continue;
            }

            if (texture == null)
                continue;

            if (IsFcgPath(
                    AssetDatabase.GetAssetPath(
                        texture)))
                return true;
        }

        return false;
    }

    private static bool IsGeneratedUrpMaterial(
        Material material)
    {
        if (material == null)
            return false;

        string path =
            AssetDatabase.GetAssetPath(
                material);

        return path.StartsWith(
            MaterialRoot + "/",
            StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsFcgPath(
        string path)
    {
        return
            !string.IsNullOrWhiteSpace(path) &&
            path.Replace('\\', '/')
                .StartsWith(
                    FcgRoot,
                    StringComparison.OrdinalIgnoreCase);
    }

    private static Material CreateOrUpdateUrpMaterial(
        Material source,
        Shader urpLit,
        int index)
    {
        string sourcePath =
            AssetDatabase.GetAssetPath(
                source);

        string guid =
            string.IsNullOrWhiteSpace(sourcePath)
                ? index.ToString("D4")
                : AssetDatabase.AssetPathToGUID(
                    sourcePath);

        if (string.IsNullOrWhiteSpace(guid))
            guid =
                index.ToString("D4");

        string shortGuid =
            guid.Length > 8
                ? guid.Substring(0, 8)
                : guid;

        string path =
            MaterialRoot +
            "/" +
            SanitizeFileName(
                source.name) +
            "_" +
            shortGuid +
            ".mat";

        Material material =
            AssetDatabase.LoadAssetAtPath<Material>(
                path);

        if (material == null)
        {
            material =
                new Material(
                    urpLit);

            AssetDatabase.CreateAsset(
                material,
                path);
        }
        else
        {
            material.shader =
                urpLit;
        }

        material.name =
            "FCG_" +
            source.name;

        material.enableInstancing =
            true;

        CopyBaseMap(
            source,
            material);

        CopyNormalMap(
            source,
            material);

        CopyOcclusionMap(
            source,
            material);

        CopyEmission(
            source,
            material);

        CopySurfaceValues(
            source,
            material);

        ConfigureSurfaceType(
            source,
            material);

        EditorUtility.SetDirty(
            material);

        return material;
    }

    private static void CopyBaseMap(
        Material source,
        Material destination)
    {
        string sourceProperty =
            FirstExistingProperty(
                source,
                "_BaseMap",
                "_MainTex",
                "_BaseColorMap",
                "_Albedo");

        if (sourceProperty != null)
        {
            Texture texture =
                SafeGetTexture(
                    source,
                    sourceProperty);

            if (texture != null &&
                destination.HasProperty(
                    "_BaseMap"))
            {
                destination.SetTexture(
                    "_BaseMap",
                    texture);

                destination.SetTextureScale(
                    "_BaseMap",
                    source.GetTextureScale(
                        sourceProperty));

                destination.SetTextureOffset(
                    "_BaseMap",
                    source.GetTextureOffset(
                        sourceProperty));
            }
        }

        Color color =
            Color.white;

        if (source.HasProperty(
                "_BaseColor"))
        {
            color =
                source.GetColor(
                    "_BaseColor");
        }
        else if (source.HasProperty(
                     "_Color"))
        {
            color =
                source.GetColor(
                    "_Color");
        }

        if (destination.HasProperty(
                "_BaseColor"))
        {
            destination.SetColor(
                "_BaseColor",
                color);
        }
    }

    private static void CopyNormalMap(
        Material source,
        Material destination)
    {
        string sourceProperty =
            FirstExistingProperty(
                source,
                "_BumpMap",
                "_NormalMap");

        if (sourceProperty == null)
            return;

        Texture texture =
            SafeGetTexture(
                source,
                sourceProperty);

        if (texture == null ||
            !destination.HasProperty(
                "_BumpMap"))
            return;

        destination.SetTexture(
            "_BumpMap",
            texture);

        if (source.HasProperty(
                "_BumpScale") &&
            destination.HasProperty(
                "_BumpScale"))
        {
            destination.SetFloat(
                "_BumpScale",
                source.GetFloat(
                    "_BumpScale"));
        }

        destination.EnableKeyword(
            "_NORMALMAP");
    }

    private static void CopyOcclusionMap(
        Material source,
        Material destination)
    {
        string sourceProperty =
            FirstExistingProperty(
                source,
                "_OcclusionMap",
                "_AOMap");

        if (sourceProperty == null)
            return;

        Texture texture =
            SafeGetTexture(
                source,
                sourceProperty);

        if (texture == null ||
            !destination.HasProperty(
                "_OcclusionMap"))
            return;

        destination.SetTexture(
            "_OcclusionMap",
            texture);

        if (destination.HasProperty(
                "_OcclusionStrength"))
        {
            destination.SetFloat(
                "_OcclusionStrength",
                source.HasProperty(
                    "_OcclusionStrength")
                    ? source.GetFloat(
                        "_OcclusionStrength")
                    : 1f);
        }
    }

    private static void CopyEmission(
        Material source,
        Material destination)
    {
        string sourceProperty =
            FirstExistingProperty(
                source,
                "_EmissionMap",
                "_Illum",
                "_Emission");

        Texture texture =
            sourceProperty != null
                ? SafeGetTexture(
                    source,
                    sourceProperty)
                : null;

        Color emissionColor =
            Color.black;

        if (source.HasProperty(
                "_EmissionColor"))
        {
            emissionColor =
                source.GetColor(
                    "_EmissionColor");
        }

        bool hasEmission =
            texture != null ||
            emissionColor.maxColorComponent > 0.001f;

        if (!hasEmission)
            return;

        if (texture != null &&
            destination.HasProperty(
                "_EmissionMap"))
        {
            destination.SetTexture(
                "_EmissionMap",
                texture);
        }

        if (destination.HasProperty(
                "_EmissionColor"))
        {
            destination.SetColor(
                "_EmissionColor",
                emissionColor.maxColorComponent > 0.001f
                    ? emissionColor
                    : Color.white);
        }

        destination.EnableKeyword(
            "_EMISSION");

        destination.globalIlluminationFlags =
            MaterialGlobalIlluminationFlags.BakedEmissive;
    }

    private static void CopySurfaceValues(
        Material source,
        Material destination)
    {
        if (destination.HasProperty(
                "_Metallic"))
        {
            destination.SetFloat(
                "_Metallic",
                source.HasProperty(
                    "_Metallic")
                    ? Mathf.Clamp01(
                        source.GetFloat(
                            "_Metallic"))
                    : 0f);
        }

        float smoothness =
            source.HasProperty(
                "_Smoothness")
                ? source.GetFloat(
                    "_Smoothness")
                : source.HasProperty(
                    "_Glossiness")
                    ? source.GetFloat(
                        "_Glossiness")
                    : source.HasProperty(
                        "_Shininess")
                        ? source.GetFloat(
                            "_Shininess")
                        : 0.25f;

        if (destination.HasProperty(
                "_Smoothness"))
        {
            destination.SetFloat(
                "_Smoothness",
                Mathf.Clamp01(
                    smoothness));
        }
    }

    private static void ConfigureSurfaceType(
        Material source,
        Material destination)
    {
        string shaderName =
            source.shader != null
                ? source.shader.name
                : string.Empty;

        string materialName =
            source.name.ToLowerInvariant();

        bool cutout =
            shaderName.IndexOf(
                "cutout",
                StringComparison.OrdinalIgnoreCase) >= 0 ||
            source.renderQueue ==
                (int)RenderQueue.AlphaTest;

        bool transparent =
            !cutout &&
            (shaderName.IndexOf(
                 "transparent",
                 StringComparison.OrdinalIgnoreCase) >= 0 ||
             source.renderQueue >=
                 (int)RenderQueue.Transparent ||
             materialName.Contains("glass"));

        destination.DisableKeyword(
            "_ALPHATEST_ON");

        destination.DisableKeyword(
            "_SURFACE_TYPE_TRANSPARENT");

        if (cutout)
        {
            if (destination.HasProperty(
                    "_Surface"))
                destination.SetFloat(
                    "_Surface",
                    0f);

            if (destination.HasProperty(
                    "_AlphaClip"))
                destination.SetFloat(
                    "_AlphaClip",
                    1f);

            if (destination.HasProperty(
                    "_Cutoff"))
            {
                destination.SetFloat(
                    "_Cutoff",
                    source.HasProperty(
                        "_Cutoff")
                        ? source.GetFloat(
                            "_Cutoff")
                        : 0.45f);
            }

            destination.EnableKeyword(
                "_ALPHATEST_ON");

            destination.renderQueue =
                (int)RenderQueue.AlphaTest;

            destination.SetOverrideTag(
                "RenderType",
                "TransparentCutout");

            return;
        }

        if (transparent)
        {
            if (destination.HasProperty(
                    "_Surface"))
                destination.SetFloat(
                    "_Surface",
                    1f);

            if (destination.HasProperty(
                    "_Blend"))
                destination.SetFloat(
                    "_Blend",
                    0f);

            if (destination.HasProperty(
                    "_SrcBlend"))
                destination.SetFloat(
                    "_SrcBlend",
                    (float)BlendMode.SrcAlpha);

            if (destination.HasProperty(
                    "_DstBlend"))
                destination.SetFloat(
                    "_DstBlend",
                    (float)BlendMode.OneMinusSrcAlpha);

            if (destination.HasProperty(
                    "_ZWrite"))
                destination.SetFloat(
                    "_ZWrite",
                    0f);

            destination.EnableKeyword(
                "_SURFACE_TYPE_TRANSPARENT");

            destination.SetOverrideTag(
                "RenderType",
                "Transparent");

            destination.renderQueue =
                (int)RenderQueue.Transparent;

            return;
        }

        if (destination.HasProperty(
                "_Surface"))
            destination.SetFloat(
                "_Surface",
                0f);

        if (destination.HasProperty(
                "_AlphaClip"))
            destination.SetFloat(
                "_AlphaClip",
                0f);

        if (destination.HasProperty(
                "_SrcBlend"))
            destination.SetFloat(
                "_SrcBlend",
                (float)BlendMode.One);

        if (destination.HasProperty(
                "_DstBlend"))
            destination.SetFloat(
                "_DstBlend",
                (float)BlendMode.Zero);

        if (destination.HasProperty(
                "_ZWrite"))
            destination.SetFloat(
                "_ZWrite",
                1f);

        destination.SetOverrideTag(
            "RenderType",
            "Opaque");

        destination.renderQueue =
            (int)RenderQueue.Geometry;
    }

    private static string FirstExistingProperty(
        Material material,
        params string[] names)
    {
        foreach (string name in names)
        {
            if (material.HasProperty(
                    name))
                return name;
        }

        return null;
    }

    private static Texture SafeGetTexture(
        Material material,
        string property)
    {
        try
        {
            return material.GetTexture(
                property);
        }
        catch
        {
            return null;
        }
    }

    private static void EnsureFolder(
        string assetPath)
    {
        string normalized =
            assetPath.Replace(
                '\\',
                '/');

        if (AssetDatabase.IsValidFolder(
                normalized))
            return;

        string parent =
            Path.GetDirectoryName(
                    normalized)
                ?.Replace(
                    '\\',
                    '/');

        string name =
            Path.GetFileName(
                normalized);

        if (string.IsNullOrWhiteSpace(
                parent) ||
            string.IsNullOrWhiteSpace(
                name))
            return;

        EnsureFolder(
            parent);

        AssetDatabase.CreateFolder(
            parent,
            name);
    }

    private static string SanitizeFileName(
        string value)
    {
        if (string.IsNullOrWhiteSpace(
                value))
            return "Material";

        char[] invalid =
            Path.GetInvalidFileNameChars();

        return new string(
            value
                .Select(
                    character =>
                        invalid.Contains(
                            character)
                            ? '_'
                            : character)
                .ToArray());
    }
}
#endif
