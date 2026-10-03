#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using MotorCity.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class FantasticCityGeneratorRuntimeBuilder
{
    private const string RuntimeRoot =
        "Assets/Resources/MotorCity/Environment";

    private const string RuntimePrefab =
        RuntimeRoot + "/CityVisual.prefab";

    [MenuItem("Motor City/Fantastic City Generator/5 - Build Runtime City")]
    public static void BuildRuntimeCity()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog(
                "Motor City - FCG Runtime City",
                "Останови Play Mode перед сборкой runtime-города.",
                "OK");
            return;
        }

        if (!FantasticCityGeneratorSceneSource.TryOpenSourceScene(
                out Scene scene,
                out bool openedTemporarily,
                out Scene previousActiveScene))
        {
            EditorUtility.DisplayDialog(
                "Motor City - FCG Runtime City",
                "Не найдена сохранённая локальная сцена с City-Maker.\n\n" +
                "Сохрани сгенерированный город в Assets/LocalGenerated.",
                "OK");
            return;
        }

        GameObject source =
            FantasticCityGeneratorSceneSource.FindCityRoot(
                scene);

        if (source == null)
        {
            FantasticCityGeneratorSceneSource.FinishSourceScene(
                scene,
                openedTemporarily,
                previousActiveScene,
                false);

            EditorUtility.DisplayDialog(
                "Motor City - FCG Runtime City",
                "В сохранённой сцене не найден корневой объект City-Maker.",
                "OK");
            return;
        }

        if (!string.IsNullOrWhiteSpace(
                scene.path))
        {
            EditorSceneManager.SaveScene(
                scene);
        }

        EnsureFolder(
            RuntimeRoot);

        GameObject trafficSystem =
            FindSceneObject(
                scene,
                "Traffic System");

        GameObject carContainer =
            FindSceneObject(
                scene,
                "CarContainer");

        GameObject garage =
            FindSceneObject(
                scene,
                "garage");

        GameObject backdrop =
            FindSceneObject(
                scene,
                "_Background 1") ??
            FindSceneObject(
                scene,
                "MotorCity_Background");

        if (backdrop == null)
        {
            Debug.LogWarning(
                "Motor City: no authored background root was found in the FCG source scene. " +
                "The runtime city will be built without a distant skyline.");
        }

        GameObject temporaryPackage =
            new GameObject(
                "__MotorCity_FCG_RuntimePackage");

        SceneManager.MoveGameObjectToScene(
            temporaryPackage,
            scene);

        Transform sourceOriginalParent =
            source.transform.parent;

        bool moveTrafficSeparately =
            trafficSystem != null &&
            !trafficSystem.transform.IsChildOf(
                source.transform);

        bool moveCarContainerSeparately =
            carContainer != null &&
            !carContainer.transform.IsChildOf(
                source.transform);

        bool moveGarageSeparately =
            garage != null &&
            !garage.transform.IsChildOf(
                source.transform);

        bool moveBackdropSeparately =
            backdrop != null &&
            !backdrop.transform.IsChildOf(
                source.transform);

        Transform trafficOriginalParent =
            moveTrafficSeparately
                ? trafficSystem.transform.parent
                : null;

        Transform carContainerOriginalParent =
            moveCarContainerSeparately
                ? carContainer.transform.parent
                : null;

        Transform garageOriginalParent =
            moveGarageSeparately
                ? garage.transform.parent
                : null;

        Transform backdropOriginalParent =
            moveBackdropSeparately
                ? backdrop.transform.parent
                : null;

        source.transform.SetParent(
            temporaryPackage.transform,
            true);

        if (moveTrafficSeparately)
        {
            trafficSystem.transform.SetParent(
                temporaryPackage.transform,
                true);
        }

        if (moveCarContainerSeparately)
        {
            carContainer.transform.SetParent(
                temporaryPackage.transform,
                true);
        }

        if (moveGarageSeparately)
        {
            garage.transform.SetParent(
                temporaryPackage.transform,
                true);
        }

        if (moveBackdropSeparately)
        {
            backdrop.transform.SetParent(
                temporaryPackage.transform,
                true);
        }

        GameObject clone =
            null;

        try
        {
            // Clone the authored runtime roots as one hierarchy. Keeping
            // City-Maker, traffic, garage and distant background together
            // preserves their exact Workbench transforms and lets Unity remap
            // serialized references inside the cloned hierarchy.
            clone =
                UnityEngine.Object.Instantiate(
                    temporaryPackage);

            clone.name =
                "MotorCity_FCGCity";

            // Runtime baking is intentionally material-passive. The
            // Workbench is the visual source of truth, and its renderers may
            // reference shared generated materials. Rebuilding or repairing
            // those materials here would mutate the Workbench itself because
            // the temporary clone shares the same material assets.
            //
            // Material conversion belongs only to the explicit "Fix Materials"
            // command. Build Runtime City only serializes the authored result.
            int remappedRenderers =
                0;

            PrefabUtility.SaveAsPrefabAsset(
                clone,
                RuntimePrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            int renderers =
                clone.GetComponentsInChildren<Renderer>(true).Length;

            ValidateSavedRuntimePrefab(
                clone,
                RuntimePrefab);

            ValidateGeneratedMaterialKeyConflicts();

            string includedRoots =
                "City-Maker" +
                (trafficSystem != null
                    ? ", Traffic System"
                    : string.Empty) +
                (carContainer != null
                    ? ", CarContainer"
                    : string.Empty) +
                (garage != null
                    ? ", garage"
                    : string.Empty) +
                (backdrop != null
                    ? ", Background"
                    : string.Empty);

            Debug.Log(
                "Motor City: Fantastic City Generator runtime package built from authored Workbench materials. " +
                $"Source={scene.path}, Roots={includedRoots}, Renderers={renderers}, " +
                $"MaterialRemaps={remappedRenderers}, prefab={RuntimePrefab}");

            EditorUtility.DisplayDialog(
                "Motor City - FCG Runtime City",
                "Готово.\n\n" +
                $"Источник: {scene.path}\n" +
                $"Включено: {includedRoots}\n" +
                $"Renderer'ов: {renderers}\n\n" +
                $"Материалов Renderer'ов нормализовано: {remappedRenderers}\n\n" +
                "CityVisual.prefab содержит авторские корневые объекты Workbench: город, traffic, garage и background.",
                "OK");
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "Motor City: failed to bake FCG runtime city. " +
                exception);

            EditorUtility.DisplayDialog(
                "Motor City - FCG Runtime City",
                "Не удалось собрать runtime-город. Посмотри Console / Editor.log.",
                "OK");
        }
        finally
        {
            if (clone != null)
            {
                UnityEngine.Object.DestroyImmediate(
                    clone);
            }

            source.transform.SetParent(
                sourceOriginalParent,
                true);

            if (moveTrafficSeparately)
            {
                trafficSystem.transform.SetParent(
                    trafficOriginalParent,
                    true);
            }

            if (moveCarContainerSeparately)
            {
                carContainer.transform.SetParent(
                    carContainerOriginalParent,
                    true);
            }

            if (moveGarageSeparately)
            {
                garage.transform.SetParent(
                    garageOriginalParent,
                    true);
            }

            if (moveBackdropSeparately)
            {
                backdrop.transform.SetParent(
                    backdropOriginalParent,
                    true);
            }

            if (temporaryPackage != null)
            {
                UnityEngine.Object.DestroyImmediate(
                    temporaryPackage);
            }

            FantasticCityGeneratorSceneSource.FinishSourceScene(
                scene,
                openedTemporarily,
                previousActiveScene,
                true);
        }
    }

    private static void ValidateSavedRuntimePrefab(
        GameObject sourceClone,
        string prefabPath)
    {
        if (sourceClone == null ||
            string.IsNullOrWhiteSpace(
                prefabPath))
        {
            return;
        }

        GameObject saved =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                prefabPath);

        if (saved == null)
        {
            Debug.LogWarning(
                "Motor City: runtime city validation could not load " +
                prefabPath +
                ".");

            return;
        }

        Renderer[] sourceRenderers =
            sourceClone.GetComponentsInChildren<Renderer>(
                true);

        Renderer[] savedRenderers =
            saved.GetComponentsInChildren<Renderer>(
                true);

        int sourceSlots =
            sourceRenderers.Sum(
                renderer =>
                    renderer != null &&
                    renderer.sharedMaterials != null
                        ? renderer.sharedMaterials.Length
                        : 0);

        int savedSlots =
            savedRenderers.Sum(
                renderer =>
                    renderer != null &&
                    renderer.sharedMaterials != null
                        ? renderer.sharedMaterials.Length
                        : 0);

        int sourceNullSlots =
            sourceRenderers.Sum(
                renderer =>
                    renderer == null ||
                    renderer.sharedMaterials == null
                        ? 0
                        : renderer.sharedMaterials.Count(
                            material =>
                                material == null));

        int savedNullSlots =
            savedRenderers.Sum(
                renderer =>
                    renderer == null ||
                    renderer.sharedMaterials == null
                        ? 0
                        : renderer.sharedMaterials.Count(
                            material =>
                                material == null));

        Transform[] sourceTransforms =
            sourceClone.GetComponentsInChildren<Transform>(
                true);

        Transform[] savedTransforms =
            saved.GetComponentsInChildren<Transform>(
                true);

        int materialReferenceMismatches =
            0;

        int comparableRendererCount =
            Mathf.Min(
                sourceRenderers.Length,
                savedRenderers.Length);

        for (int rendererIndex = 0;
             rendererIndex < comparableRendererCount;
             rendererIndex++)
        {
            Renderer sourceRenderer =
                sourceRenderers[
                    rendererIndex];

            Renderer savedRenderer =
                savedRenderers[
                    rendererIndex];

            if (sourceRenderer == null ||
                savedRenderer == null)
            {
                continue;
            }

            Material[] sourceMaterials =
                sourceRenderer.sharedMaterials;

            Material[] savedMaterials =
                savedRenderer.sharedMaterials;

            int comparableSlotCount =
                Mathf.Min(
                    sourceMaterials != null
                        ? sourceMaterials.Length
                        : 0,
                    savedMaterials != null
                        ? savedMaterials.Length
                        : 0);

            for (int slot = 0;
                 slot < comparableSlotCount;
                 slot++)
            {
                string sourcePath =
                    sourceMaterials[slot] != null
                        ? AssetDatabase.GetAssetPath(
                            sourceMaterials[slot])
                        : string.Empty;

                string savedPath =
                    savedMaterials[slot] != null
                        ? AssetDatabase.GetAssetPath(
                            savedMaterials[slot])
                        : string.Empty;

                if (!string.Equals(
                        sourcePath,
                        savedPath,
                        StringComparison.OrdinalIgnoreCase))
                {
                    materialReferenceMismatches++;
                }
            }
        }

        bool mismatch =
            sourceRenderers.Length !=
                savedRenderers.Length ||
            sourceTransforms.Length !=
                savedTransforms.Length ||
            sourceSlots !=
                savedSlots ||
            sourceNullSlots !=
                savedNullSlots ||
            materialReferenceMismatches >
                0;

        if (mismatch)
        {
            Debug.LogWarning(
                "Motor City: CityVisual validation mismatch after save. " +
                $"Source transforms={sourceTransforms.Length}, saved transforms={savedTransforms.Length}, " +
                $"source renderers={sourceRenderers.Length}, saved renderers={savedRenderers.Length}, " +
                $"source material slots={sourceSlots}, saved material slots={savedSlots}, " +
                $"source null slots={sourceNullSlots}, saved null slots={savedNullSlots}, " +
                $"material reference mismatches={materialReferenceMismatches}. " +
                "Build Runtime City did not serialize the authored hierarchy/material references 1:1.");
        }
        else
        {
            Debug.Log(
                "Motor City: CityVisual validation passed. " +
                $"{savedTransforms.Length} transforms, {savedRenderers.Length} renderers and " +
                $"{savedSlots} material slots preserved their authored material references.");
        }
    }

    private static void ValidateGeneratedMaterialKeyConflicts()
    {
        Material[] materials =
            Resources.LoadAll<Material>(
                "MotorCity/Environment/FCGMaterials");

        if (materials == null ||
            materials.Length == 0)
        {
            return;
        }

        var conflicts =
            materials
                .Where(material =>
                    material != null)
                .GroupBy(
                    material =>
                        RuntimeMaterialKeyForValidation(
                            material.name),
                    StringComparer.OrdinalIgnoreCase)
                .Where(group =>
                    !string.IsNullOrWhiteSpace(
                        group.Key) &&
                    group.Count() > 1)
                .Select(group =>
                    new
                    {
                        Key = group.Key,
                        Materials = group
                            .Select(material =>
                                material.name)
                            .OrderBy(name => name)
                            .ToArray()
                    })
                .ToArray();

        if (conflicts.Length == 0)
        {
            Debug.Log(
                "Motor City: FCG runtime material validation found no duplicate canonical keys.");

            return;
        }

        string details =
            string.Join(
                "\n",
                conflicts.Select(
                    conflict =>
                        conflict.Key +
                        " => " +
                        string.Join(
                            ", ",
                            conflict.Materials)));

        Debug.LogWarning(
            "Motor City: FCG runtime material validation found duplicate canonical keys. " +
            "The existing runtime rebind mechanic may choose between these generated materials " +
            "by load order when their priority is equal. No materials were changed automatically.\n" +
            details);
    }

    private static string RuntimeMaterialKeyForValidation(
        string materialName)
    {
        if (string.IsNullOrWhiteSpace(
                materialName))
        {
            return string.Empty;
        }

        string value =
            materialName
                .Replace(
                    "(Instance)",
                    string.Empty,
                    StringComparison.OrdinalIgnoreCase)
                .Trim();

        if (value.StartsWith(
                "FCG_",
                StringComparison.OrdinalIgnoreCase))
        {
            value =
                value.Substring(
                    4);
        }

        int separator =
            value.LastIndexOf(
                '_');

        if (separator >= 0 &&
            separator <
                value.Length - 1)
        {
            string suffix =
                value.Substring(
                    separator + 1);

            if (suffix.Length == 8 &&
                suffix.All(
                    Uri.IsHexDigit))
            {
                value =
                    value.Substring(
                        0,
                        separator);
            }
        }

        string key =
            new string(
                value
                    .ToLowerInvariant()
                    .Where(
                        char.IsLetterOrDigit)
                    .ToArray());

        return
            key switch
            {
                "winsnight" =>
                    "wins",
                "wins02night" =>
                    "wins02",
                "winglass01night" =>
                    "winglass01",
                "winglass01dn" =>
                    "winglass01d",
                "winglass03night" =>
                    "winglass03",
                "winglass04night" =>
                    "winglass04",
                _ =>
                    key
            };
    }

    private static string NormalizeName(
        string value)
    {
        if (string.IsNullOrWhiteSpace(
                value))
        {
            return string.Empty;
        }

        var result =
            new System.Text.StringBuilder(
                value.Length);

        foreach (char character in
                 value.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(
                    character))
            {
                result.Append(
                    character);
            }
        }

        return result.ToString();
    }

    private static GameObject FindSceneObject(
        Scene scene,
        string wantedName)
    {
        string wanted =
            NormalizeName(
                wantedName);

        foreach (GameObject root in
                 scene.GetRootGameObjects())
        {
            if (root == null)
                continue;

            Transform[] transforms =
                root.GetComponentsInChildren<Transform>(
                    true);

            for (int i = 0;
                 i < transforms.Length;
                 i++)
            {
                Transform candidate =
                    transforms[i];

                if (candidate == null)
                    continue;

                string normalized =
                    NormalizeName(
                        candidate.name);

                if (normalized ==
                        wanted ||
                    normalized ==
                        wanted +
                        "clone")
                {
                    return
                        candidate.gameObject;
                }
            }
        }

        return null;
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

        if (string.IsNullOrWhiteSpace(parent) ||
            string.IsNullOrWhiteSpace(name))
            return;

        EnsureFolder(
            parent);

        AssetDatabase.CreateFolder(
            parent,
            name);
    }
}
#endif
