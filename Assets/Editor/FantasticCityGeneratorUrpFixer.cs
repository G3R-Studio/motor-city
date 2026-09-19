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

    [MenuItem("Motor City/Fantastic City Generator/Fix Materials in Saved FCG City")]
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

        if (!FantasticCityGeneratorSceneSource.TryOpenSourceScene(
                out Scene scene,
                out bool openedTemporarily,
                out Scene previousActiveScene))
        {
            EditorUtility.DisplayDialog(
                "Motor City — FCG URP Fix",
                "Не найдена сохранённая локальная сцена с City-Maker.\n\n" +
                "Сохрани сгенерированный город в Assets/LocalGenerated.",
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

        var generatedMaterials =
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
                {
                    generatedMaterials.Add(
                        material);
                    continue;
                }

                if (BelongsToFantasticCityGenerator(
                        material))
                {
                    sourceMaterials.Add(
                        material);
                }
            }
        }

        if (sourceMaterials.Count == 0 &&
            generatedMaterials.Count == 0)
        {
            EditorUtility.DisplayDialog(
                "Motor City — FCG URP Fix",
                "В сохранённой сцене City-Maker не найдено материалов Fantastic City Generator.",
                "OK");

            FantasticCityGeneratorSceneSource.FinishSourceScene(
                scene,
                openedTemporarily,
                previousActiveScene,
                false);

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
            int repairedGenerated =
                0;

            foreach (Material generated in
                     generatedMaterials)
            {
                RepairGeneratedUrpMaterial(
                    generated,
                    urpLit);

                repairedGenerated++;
            }

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
                $"Converted {converted.Count} source materials, repaired " +
                $"{repairedGenerated} existing URP materials and updated " +
                $"{changedRenderers} renderers.");

            EditorUtility.DisplayDialog(
                "Motor City — FCG URP Fix",
                "Готово.\n\n" +
                $"Новых материалов конвертировано: {converted.Count}\n" +
                $"Существующих URP-материалов исправлено: {repairedGenerated}\n" +
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

            FantasticCityGeneratorSceneSource.FinishSourceScene(
                scene,
                openedTemporarily,
                previousActiveScene,
                true);
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

        bool foliage =
            IsCutoutFoliageMaterialName(
                source.name);

        Shader targetShader =
            foliage
                ? Shader.Find(
                      "MotorCity/TwoSidedFoliage") ??
                  urpLit
                : urpLit;

        Material material =
            AssetDatabase.LoadAssetAtPath<Material>(
                path);

        if (material == null)
        {
            material =
                new Material(
                    targetShader);

            AssetDatabase.CreateAsset(
                material,
                path);
        }
        else
        {
            material.shader =
                targetShader;
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
        string normalizedSourceName =
            NormalizeMaterialName(
                source.name);

        bool grassSplat =
            normalizedSourceName.Contains(
                "grasssplat");

        string sourceProperty =
            grassSplat
                ? FindBestBaseTextureProperty(
                    source)
                : FirstExistingProperty(
                    source,
                    "_BaseMap",
                    "_MainTex",
                    "_BaseColorMap",
                    "_Albedo",
                    "_AlbedoMap",
                    "_Diffuse",
                    "_DiffuseMap",
                    "_ColorMap",
                    "_Texture");

        if (sourceProperty == null)
        {
            sourceProperty =
                FindBestBaseTextureProperty(
                    source);
        }

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

        bool foliage =
            IsCutoutFoliageMaterialName(
                materialName);

        bool cutout =
            (foliage &&
             HasUsableAlphaTexture(destination)) ||
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
                    foliage
                        ? 0.12f
                        : source.HasProperty(
                            "_Cutoff")
                            ? Mathf.Clamp01(
                                source.GetFloat(
                                    "_Cutoff"))
                            : 0.45f);
            }

            destination.EnableKeyword(
                "_ALPHATEST_ON");

            destination.renderQueue =
                (int)RenderQueue.AlphaTest;

            destination.SetOverrideTag(
                "RenderType",
                "TransparentCutout");

            if (foliage &&
                destination.HasProperty(
                    "_Cull"))
            {
                destination.SetFloat(
                    "_Cull",
                    (float)CullMode.Off);
            }

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

    private static void RepairGeneratedUrpMaterial(
        Material material,
        Shader urpLit)
    {
        if (material == null)
            return;

        string generatedName =
            material.name.StartsWith(
                "FCG_",
                StringComparison.OrdinalIgnoreCase)
                ? material.name.Substring(4)
                : material.name;

        bool foliage =
            IsCutoutFoliageMaterialName(
                generatedName);

        Shader targetShader =
            foliage
                ? Shader.Find(
                      "MotorCity/TwoSidedFoliage") ??
                  urpLit
                : urpLit;

        material.shader =
            targetShader;

        Material source =
            FindOriginalFcgMaterialForGenerated(
                material,
                generatedName);

        if (source != null)
        {
            // Foliage materials are intentionally refreshed every pass. Some
            // old FCG shaders leave a non-null default white texture in the
            // generated URP material, so checking only for null is not enough.
            if (foliage ||
                !HasUsefulBaseTexture(material))
            {
                CopyBaseMap(
                    source,
                    material);
            }

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
        }

        if (foliage)
        {
            EnsureFoliageBaseTexture(
                material,
                generatedName);

            ConfigureFoliageFallback(
                material);
        }

        material.enableInstancing =
            true;

        EditorUtility.SetDirty(
            material);
    }

    private static Material FindOriginalFcgMaterialForGenerated(
        Material generated,
        string materialName)
    {
        if (generated != null)
        {
            string generatedPath =
                AssetDatabase.GetAssetPath(
                    generated);

            string fileName =
                Path.GetFileNameWithoutExtension(
                    generatedPath);

            int separator =
                fileName.LastIndexOf(
                    '_');

            if (separator >= 0 &&
                separator <
                fileName.Length - 1)
            {
                string shortGuid =
                    fileName.Substring(
                        separator + 1);

                if (shortGuid.Length == 8 &&
                    shortGuid.All(
                        IsHexCharacter))
                {
                    string[] guids =
                        AssetDatabase.FindAssets(
                            "t:Material",
                            new[]
                            {
                                FcgRoot.TrimEnd('/')
                            });

                    foreach (string guid in guids)
                    {
                        if (!guid.StartsWith(
                                shortGuid,
                                StringComparison.OrdinalIgnoreCase))
                            continue;

                        string sourcePath =
                            AssetDatabase.GUIDToAssetPath(
                                guid);

                        Material exact =
                            AssetDatabase.LoadAssetAtPath<Material>(
                                sourcePath);

                        if (exact != null)
                        {
                            return exact;
                        }
                    }
                }
            }
        }

        return
            FindOriginalFcgMaterial(
                materialName);
    }

    private static bool IsHexCharacter(
        char value)
    {
        return
            (value >= '0' &&
             value <= '9') ||
            (value >= 'a' &&
             value <= 'f') ||
            (value >= 'A' &&
             value <= 'F');
    }

    private static Material FindOriginalFcgMaterial(
        string materialName)
    {
        if (string.IsNullOrWhiteSpace(
                materialName))
            return null;

        string wanted =
            NormalizeMaterialName(
                materialName);

        if (string.IsNullOrWhiteSpace(
                wanted))
            return null;

        Material best =
            null;

        int bestScore =
            int.MinValue;

        string[] guids =
            AssetDatabase.FindAssets(
                "t:Material",
                new[]
                {
                    FcgRoot.TrimEnd('/')
                });

        foreach (string guid in guids)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(
                    guid);

            Material candidate =
                AssetDatabase.LoadAssetAtPath<Material>(
                    path);

            if (candidate == null)
                continue;

            string normalized =
                NormalizeMaterialName(
                    candidate.name);

            if (string.IsNullOrWhiteSpace(
                    normalized))
                continue;

            int score =
                0;

            if (normalized == wanted)
                score += 100;

            if (wanted.Contains(normalized) ||
                normalized.Contains(wanted))
                score += 35;

            string[] wantedTokens =
                MaterialNameTokens(
                    wanted);

            string[] candidateTokens =
                MaterialNameTokens(
                    normalized);

            foreach (string token in wantedTokens)
            {
                if (candidateTokens.Contains(
                        token))
                {
                    score += 8;
                }
            }

            if (score <= bestScore)
                continue;

            bestScore =
                score;

            best =
                candidate;
        }

        return
            bestScore >= 16
                ? best
                : null;
    }

    private static bool HasUsefulBaseTexture(
        Material material)
    {
        if (material == null ||
            !material.HasProperty("_BaseMap"))
            return false;

        Texture texture =
            material.GetTexture(
                "_BaseMap");

        if (texture == null)
            return false;

        string path =
            AssetDatabase.GetAssetPath(
                texture);

        return
            !string.IsNullOrWhiteSpace(
                path);
    }

    private static void EnsureFoliageBaseTexture(
        Material material,
        string materialName)
    {
        if (material == null ||
            !material.HasProperty("_BaseMap"))
            return;

        Texture current =
            material.GetTexture(
                "_BaseMap");

        string currentPath =
            current != null
                ? AssetDatabase.GetAssetPath(
                    current)
                : string.Empty;

        if (!string.IsNullOrWhiteSpace(
                currentPath) &&
            IsFcgPath(
                currentPath))
            return;

        Texture fallback =
            FindFcgTextureForMaterial(
                materialName);

        if (fallback == null)
            return;

        material.SetTexture(
            "_BaseMap",
            fallback);

        Debug.Log(
            "Motor City: restored FCG foliage texture " +
            $"'{fallback.name}' for material '{material.name}'.");
    }

    private static Texture FindFcgTextureForMaterial(
        string materialName)
    {
        string wanted =
            NormalizeMaterialName(
                materialName);

        string[] wantedTokens =
            MaterialNameTokens(
                wanted);

        Texture best =
            null;

        int bestScore =
            int.MinValue;

        string[] guids =
            AssetDatabase.FindAssets(
                "t:Texture2D",
                new[]
                {
                    FcgRoot.TrimEnd('/')
                });

        foreach (string guid in guids)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(
                    guid);

            Texture texture =
                AssetDatabase.LoadAssetAtPath<Texture>(
                    path);

            if (texture == null)
                continue;

            string normalizedTexture =
                NormalizeMaterialName(
                    texture.name);

            int score =
                0;

            if (normalizedTexture == wanted)
                score += 100;

            if (wanted.Contains(
                    normalizedTexture) ||
                normalizedTexture.Contains(
                    wanted))
                score += 30;

            string[] textureTokens =
                MaterialNameTokens(
                    normalizedTexture);

            foreach (string token in wantedTokens)
            {
                if (textureTokens.Contains(
                        token))
                {
                    score += 9;
                }
            }

            string lower =
                texture.name.ToLowerInvariant();

            if (wanted.Contains("grass") &&
                lower.Contains("grass"))
                score += 20;

            if ((wanted.Contains("tree") ||
                 wanted.Contains("leaf") ||
                 wanted.Contains("foliage")) &&
                (lower.Contains("tree") ||
                 lower.Contains("leaf") ||
                 lower.Contains("foliage")))
                score += 20;

            if (lower.Contains("normal") ||
                lower.Contains("bump") ||
                lower.Contains("mask") ||
                lower.Contains("control") ||
                lower.Contains("metal") ||
                lower.Contains("spec") ||
                lower.Contains("ao") ||
                lower.Contains("occlusion"))
                score -= 40;

            if (score <= bestScore)
                continue;

            bestScore =
                score;

            best =
                texture;
        }

        return
            bestScore >= 20
                ? best
                : null;
    }

    private static string NormalizeMaterialName(
        string value)
    {
        if (string.IsNullOrWhiteSpace(
                value))
            return string.Empty;

        string normalized =
            value.Trim();

        if (normalized.StartsWith(
                "FCG_",
                StringComparison.OrdinalIgnoreCase))
        {
            normalized =
                normalized.Substring(4);
        }

        normalized =
            normalized.Replace(
                "(Instance)",
                string.Empty);

        int lastDot =
            normalized.LastIndexOf('.');

        if (lastDot >= 0 &&
            lastDot < normalized.Length - 1)
        {
            string suffix =
                normalized.Substring(
                    lastDot + 1);

            if (suffix.All(char.IsDigit))
            {
                normalized =
                    normalized.Substring(
                        0,
                        lastDot);
            }
        }

        return new string(
            normalized
                .ToLowerInvariant()
                .Where(char.IsLetterOrDigit)
                .ToArray());
    }

    private static string[] MaterialNameTokens(
        string normalized)
    {
        if (string.IsNullOrWhiteSpace(
                normalized))
            return Array.Empty<string>();

        var tokens =
            new List<string>();

        foreach (string known in new[]
        {
            "grass",
            "splat",
            "tree",
            "trees",
            "leaf",
            "leaves",
            "foliage",
            "palm",
            "fern",
            "road",
            "highway",
            "atlas"
        })
        {
            if (normalized.Contains(
                    known))
            {
                tokens.Add(
                    known);
            }
        }

        return tokens
            .Distinct()
            .ToArray();
    }

    private static string FindBestBaseTextureProperty(
        Material material)
    {
        if (material == null)
            return null;

        string[] properties;

        try
        {
            properties =
                material.GetTexturePropertyNames();
        }
        catch
        {
            return null;
        }

        string best =
            null;

        int bestScore =
            int.MinValue;

        bool grassSplat =
            NormalizeMaterialName(
                material.name)
                .Contains(
                    "grasssplat");

        foreach (string property in properties)
        {
            Texture texture =
                SafeGetTexture(
                    material,
                    property);

            if (texture == null)
                continue;

            string lower =
                property.ToLowerInvariant();

            if (lower.Contains("normal") ||
                lower.Contains("bump") ||
                lower.Contains("mask") ||
                lower.Contains("metal") ||
                lower.Contains("smooth") ||
                lower.Contains("spec") ||
                lower.Contains("occlusion") ||
                lower.Contains("ao") ||
                lower.Contains("emission") ||
                lower.Contains("illum"))
                continue;

            int score =
                0;

            if (lower.Contains("main"))
                score +=
                    grassSplat
                        ? -8
                        : 8;

            if (grassSplat &&
                lower.Contains("splat"))
            {
                score += 22;
            }

            if (lower.Contains("base"))
                score += 7;

            if (lower.Contains("albedo"))
                score += 7;

            if (lower.Contains("diffuse"))
                score += 6;

            if (lower.Contains("color"))
                score += 4;

            string textureName =
                texture.name.ToLowerInvariant();

            if (textureName.Contains("diff") ||
                textureName.Contains("albedo") ||
                textureName.Contains("color"))
                score += 3;

            if (grassSplat &&
                textureName.Contains("grass"))
            {
                score += 14;
            }

            if (grassSplat &&
                (textureName.Contains("control") ||
                 textureName.Contains("mask")))
            {
                score -= 30;
            }

            if (score <= bestScore)
                continue;

            bestScore =
                score;

            best =
                property;
        }

        return best;
    }

    private static bool IsFoliageMaterialName(
        string materialName)
    {
        if (string.IsNullOrWhiteSpace(
                materialName))
            return false;

        string lower =
            materialName.ToLowerInvariant();

        return
            lower.Contains("grass") ||
            lower.Contains("tree") ||
            lower.Contains("leaf") ||
            lower.Contains("leaves") ||
            lower.Contains("foliage") ||
            lower.Contains("vegetation") ||
            lower.Contains("fern") ||
            lower.Contains("palm");
    }

    private static bool IsCutoutFoliageMaterialName(
        string materialName)
    {
        if (string.IsNullOrWhiteSpace(
                materialName))
            return false;

        string lower =
            materialName.ToLowerInvariant();

        // "Grass-Splat" and "Grass-01" are ground materials, not leaf cards.
        return
            lower.Contains("tree") ||
            lower.Contains("leaf") ||
            lower.Contains("leaves") ||
            lower.Contains("foliage") ||
            lower.Contains("vegetation") ||
            lower.Contains("fern") ||
            lower.Contains("palm");
    }

    private static bool HasUsableAlphaTexture(
        Material material)
    {
        if (material == null ||
            !material.HasProperty(
                "_BaseMap"))
            return false;

        Texture texture =
            material.GetTexture(
                "_BaseMap");

        if (texture == null)
            return false;

        string path =
            AssetDatabase.GetAssetPath(
                texture);

        if (string.IsNullOrWhiteSpace(
                path))
            return false;

        TextureImporter importer =
            AssetImporter.GetAtPath(
                path) as TextureImporter;

        if (importer == null)
            return false;

        try
        {
            return importer.DoesSourceTextureHaveAlpha();
        }
        catch
        {
            return importer.alphaSource !=
                TextureImporterAlphaSource.None;
        }
    }

    private static void ConfigureFoliageFallback(
        Material material)
    {
        if (material == null)
            return;

        string name =
            NormalizeMaterialName(
                material.name);

        bool cutoutFoliage =
            IsCutoutFoliageMaterialName(
                name);

        bool hasAlpha =
            HasUsableAlphaTexture(
                material);

        if (material.HasProperty(
                "_Surface"))
        {
            material.SetFloat(
                "_Surface",
                0f);
        }

        material.DisableKeyword(
            "_SURFACE_TYPE_TRANSPARENT");

        if (cutoutFoliage &&
            hasAlpha)
        {
            if (material.HasProperty(
                    "_AlphaClip"))
            {
                material.SetFloat(
                    "_AlphaClip",
                    1f);
            }

            if (material.HasProperty(
                    "_Cutoff"))
            {
                // Keep this deliberately low. Some FCG tree atlases use
                // soft alpha and disappear almost entirely with a 0.4-0.5
                // cutout threshold.
                material.SetFloat(
                    "_Cutoff",
                    0.12f);
            }

            material.EnableKeyword(
                "_ALPHATEST_ON");

            material.SetOverrideTag(
                "RenderType",
                "TransparentCutout");

            material.renderQueue =
                (int)RenderQueue.AlphaTest;
        }
        else
        {
            if (material.HasProperty(
                    "_AlphaClip"))
            {
                material.SetFloat(
                    "_AlphaClip",
                    0f);
            }

            material.DisableKeyword(
                "_ALPHATEST_ON");

            material.SetOverrideTag(
                "RenderType",
                "Opaque");

            material.renderQueue =
                (int)RenderQueue.Geometry;
        }

        if (cutoutFoliage &&
            material.HasProperty(
                "_Cull"))
        {
            material.SetFloat(
                "_Cull",
                (float)CullMode.Off);
        }

        if (cutoutFoliage)
        {
            material.doubleSidedGI =
                true;

            if (material.HasProperty(
                    "_BaseColor"))
            {
                material.SetColor(
                    "_BaseColor",
                    Color.white);
            }

            if (material.HasProperty(
                    "_Color"))
            {
                material.SetColor(
                    "_Color",
                    Color.white);
            }

            if (material.HasProperty(
                    "_BumpMap"))
            {
                material.SetTexture(
                    "_BumpMap",
                    null);
            }

            material.DisableKeyword(
                "_NORMALMAP");

            if (material.HasProperty(
                    "_Metallic"))
            {
                material.SetFloat(
                    "_Metallic",
                    0f);
            }

            if (material.HasProperty(
                    "_Smoothness"))
            {
                material.SetFloat(
                    "_Smoothness",
                    0f);
            }
        }
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
