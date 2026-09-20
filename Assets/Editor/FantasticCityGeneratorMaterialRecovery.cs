#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class FantasticCityGeneratorMaterialRecovery
{
    private const string ReportPath =
        "MotorCity_FCGSceneReport.txt";

    private const string FcgRoot =
        "Assets/Fantastic City Generator";

    private static readonly Regex MaterialRegex =
        new Regex(
            @"([^,\[]+?) \[[^\]]+\](?:, |$)",
            RegexOptions.Compiled);

    public static void Recover()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog(
                "Motor City — FCG Material Recovery",
                "Останови Play Mode перед восстановлением материалов.",
                "OK");
            return;
        }

        string absoluteReportPath =
            Path.GetFullPath(
                ReportPath);

        if (!File.Exists(
                absoluteReportPath))
        {
            EditorUtility.DisplayDialog(
                "Motor City — FCG Material Recovery",
                "В корне проекта не найден MotorCity_FCGSceneReport.txt.\n\n" +
                "Положи восстановленный отчёт рядом с Assets и запусти команду ещё раз.",
                "OK");
            return;
        }

        if (!AssetDatabase.IsValidFolder(
                FcgRoot))
        {
            EditorUtility.DisplayDialog(
                "Motor City — FCG Material Recovery",
                "Не найден локальный пакет Assets/Fantastic City Generator.\n\n" +
                "Сначала верни/импортируй Fantastic City Generator.",
                "OK");
            return;
        }

        if (!FantasticCityGeneratorSceneSource.TryOpenSourceScene(
                out Scene scene,
                out bool openedTemporarily,
                out Scene previousActiveScene))
        {
            EditorUtility.DisplayDialog(
                "Motor City — FCG Material Recovery",
                "Не найдена сохранённая сцена City-Maker в Assets/LocalGenerated.",
                "OK");
            return;
        }

        try
        {
            Dictionary<string, string[]> materialMap =
                ReadReport(
                    absoluteReportPath);

            Dictionary<string, Material> sourceMaterials =
                BuildSourceMaterialLookup();

            int renderersSeen =
                0;

            int renderersRecovered =
                0;

            int missingPathMappings =
                0;

            var missingMaterialNames =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase);

            foreach (GameObject root in
                     scene.GetRootGameObjects())
            {
                foreach (Renderer renderer in
                         root.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer == null)
                        continue;

                    renderersSeen++;

                    string path =
                        GetHierarchyPath(
                            renderer.transform);

                    if (!materialMap.TryGetValue(
                            path,
                            out string[] requestedNames))
                    {
                        missingPathMappings++;
                        continue;
                    }

                    Material[] restored =
                        new Material[requestedNames.Length];

                    bool complete =
                        true;

                    for (int i = 0;
                         i < requestedNames.Length;
                         i++)
                    {
                        string requested =
                            requestedNames[i];

                        if (!TryResolveSourceMaterial(
                                requested,
                                sourceMaterials,
                                out Material source))
                        {
                            missingMaterialNames.Add(
                                requested);

                            complete =
                                false;

                            continue;
                        }

                        restored[i] =
                            source;
                    }

                    if (!complete)
                        continue;

                    Undo.RecordObject(
                        renderer,
                        "Recover FCG materials");

                    renderer.sharedMaterials =
                        restored;

                    EditorUtility.SetDirty(
                        renderer);

                    renderersRecovered++;
                }
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                scene);

            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(
                scene);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string missingSummary =
                missingMaterialNames.Count > 0
                    ? string.Join(
                        ", ",
                        missingMaterialNames)
                    : "нет";

            Debug.Log(
                "Motor City: FCG material references recovered from scene report. " +
                $"Report mappings={materialMap.Count}, renderers={renderersSeen}, " +
                $"recovered={renderersRecovered}, no path mapping={missingPathMappings}, " +
                $"missing source material names={missingSummary}.");

            EditorUtility.DisplayDialog(
                "Motor City — FCG Material Recovery",
                "Восстановление ссылок завершено.\n\n" +
                $"Записей в отчёте: {materialMap.Count}\n" +
                $"Renderer'ов в сцене: {renderersSeen}\n" +
                $"Восстановлено: {renderersRecovered}\n" +
                $"Без записи в отчёте: {missingPathMappings}\n" +
                $"Не найденные материалы: {missingSummary}\n\n" +
                "Теперь запусти:\n" +
                "Motor City > Fantastic City Generator > Fix Materials in Saved FCG City",
                "OK");
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "Motor City: FCG material recovery failed. " +
                exception);

            EditorUtility.DisplayDialog(
                "Motor City — FCG Material Recovery",
                "Восстановление завершилось ошибкой. Посмотри Console / Editor.log.",
                "OK");
        }
        finally
        {
            FantasticCityGeneratorSceneSource.FinishSourceScene(
                scene,
                openedTemporarily,
                previousActiveScene,
                true);
        }
    }

    private static Dictionary<string, string[]> ReadReport(
        string path)
    {
        var result =
            new Dictionary<string, string[]>(
                StringComparer.OrdinalIgnoreCase);

        foreach (string rawLine in
                 File.ReadLines(
                     path))
        {
            if (!rawLine.StartsWith(
                    "OBJECT ",
                    StringComparison.Ordinal))
                continue;

            int firstSeparator =
                rawLine.IndexOf(
                    " | ",
                    StringComparison.Ordinal);

            if (firstSeparator <=
                "OBJECT ".Length)
                continue;

            string hierarchyPath =
                rawLine.Substring(
                    "OBJECT ".Length,
                    firstSeparator -
                    "OBJECT ".Length);

            const string materialsToken =
                " | materials=";

            int materialsStart =
                rawLine.IndexOf(
                    materialsToken,
                    StringComparison.Ordinal);

            if (materialsStart < 0)
                continue;

            materialsStart +=
                materialsToken.Length;

            int materialsEnd =
                rawLine.IndexOf(
                    " | ",
                    materialsStart,
                    StringComparison.Ordinal);

            string materialText =
                materialsEnd >= 0
                    ? rawLine.Substring(
                        materialsStart,
                        materialsEnd -
                        materialsStart)
                    : rawLine.Substring(
                        materialsStart);

            MatchCollection matches =
                MaterialRegex.Matches(
                    materialText);

            if (matches.Count == 0)
                continue;

            string[] names =
                new string[matches.Count];

            for (int i = 0;
                 i < matches.Count;
                 i++)
            {
                names[i] =
                    matches[i]
                        .Groups[1]
                        .Value
                        .Trim();
            }

            result[hierarchyPath] =
                names;
        }

        return result;
    }

    private static Dictionary<string, Material> BuildSourceMaterialLookup()
    {
        var result =
            new Dictionary<string, Material>(
                StringComparer.OrdinalIgnoreCase);

        string[] guids =
            AssetDatabase.FindAssets(
                "t:Material",
                new[]
                {
                    FcgRoot
                });

        foreach (string guid in guids)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(
                    guid);

            Material material =
                AssetDatabase.LoadAssetAtPath<Material>(
                    path);

            if (material == null)
                continue;

            AddMaterialKey(
                result,
                material.name,
                material);

            AddMaterialKey(
                result,
                Path.GetFileNameWithoutExtension(
                    path),
                material);

            AddMaterialKey(
                result,
                "FCG_" + material.name,
                material);

            AddMaterialKey(
                result,
                "FCG_" +
                Path.GetFileNameWithoutExtension(
                    path),
                material);
        }

        return result;
    }

    private static void AddMaterialKey(
        Dictionary<string, Material> lookup,
        string key,
        Material material)
    {
        if (string.IsNullOrWhiteSpace(
                key) ||
            material == null ||
            lookup.ContainsKey(
                key))
            return;

        lookup.Add(
            key,
            material);
    }

    private static bool TryResolveSourceMaterial(
        string requested,
        Dictionary<string, Material> lookup,
        out Material material)
    {
        if (lookup.TryGetValue(
                requested,
                out material))
            return true;

        string withoutPrefix =
            requested.StartsWith(
                "FCG_",
                StringComparison.OrdinalIgnoreCase)
                ? requested.Substring(
                    4)
                : requested;

        if (lookup.TryGetValue(
                withoutPrefix,
                out material))
            return true;

        string normalizedRequested =
            NormalizeName(
                withoutPrefix);

        foreach (KeyValuePair<string, Material> pair in
                 lookup)
        {
            if (string.Equals(
                    NormalizeName(
                        pair.Key),
                    normalizedRequested,
                    StringComparison.OrdinalIgnoreCase))
            {
                material =
                    pair.Value;

                return true;
            }
        }

        material =
            null;

        return false;
    }

    private static string NormalizeName(
        string value)
    {
        if (string.IsNullOrWhiteSpace(
                value))
            return string.Empty;

        return
            value
                .Replace(
                    "FCG_",
                    string.Empty)
                .Replace(
                    " (Instance)",
                    string.Empty)
                .Trim();
    }

    private static string GetHierarchyPath(
        Transform item)
    {
        string path =
            item.name;

        Transform current =
            item.parent;

        while (current != null)
        {
            path =
                current.name +
                "/" +
                path;

            current =
                current.parent;
        }

        return path;
    }
}
#endif
