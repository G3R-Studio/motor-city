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

    private const string FcgBackdropPrefab =
        "Assets/Fantastic City Generator/BackGrounds/_Background 1.prefab";

    private const string BackdropMaterial =
        RuntimeRoot + "/FCGBackdrop.mat";

    [MenuItem("Motor City/Fantastic City Generator/5 - Build Runtime City")]
    public static void BuildRuntimeCity()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog(
                "Motor City — FCG Runtime City",
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
                "Motor City — FCG Runtime City",
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
                "Motor City — FCG Runtime City",
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
            FindSceneRoot(
                scene,
                "Traffic System");

        GameObject carContainer =
            FindSceneRoot(
                scene,
                "CarContainer");


        GameObject backdrop =
            CreateRuntimeBackdrop(
                source);

        GameObject temporaryPackage =
            new GameObject(
                "__MotorCity_FCG_RuntimePackage");

        SceneManager.MoveGameObjectToScene(
            temporaryPackage,
            scene);

        Transform sourceOriginalParent =
            source.transform.parent;

        Transform trafficOriginalParent =
            trafficSystem != null
                ? trafficSystem.transform.parent
                : null;

        Transform carContainerOriginalParent =
            carContainer != null
                ? carContainer.transform.parent
                : null;

        source.transform.SetParent(
            temporaryPackage.transform,
            true);

        if (trafficSystem != null)
        {
            trafficSystem.transform.SetParent(
                temporaryPackage.transform,
                true);
        }

        if (carContainer != null)
        {
            carContainer.transform.SetParent(
                temporaryPackage.transform,
                true);
        }

        if (backdrop != null)
        {
            backdrop.transform.SetParent(
                temporaryPackage.transform,
                true);
        }

        GameObject clone =
            null;

        try
        {
            // Clone the three FCG roots as one hierarchy. Instantiating them
            // together is important because FCG traffic scripts can keep
            // serialized references between Traffic System, CarContainer and
            // City-Maker; Unity remaps those references correctly inside one
            // cloned hierarchy.
            clone =
                UnityEngine.Object.Instantiate(
                    temporaryPackage);

            clone.name =
                "MotorCity_FCGCity";

            // The authored FCG scene is the single source of truth.
            // Building the runtime prefab must not alter colliders, traffic,
            // cars, props, signals, lights or any other authored content.
            PrefabUtility.SaveAsPrefabAsset(
                clone,
                RuntimePrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            int renderers =
                clone.GetComponentsInChildren<Renderer>(true).Length;

            string includedRoots =
                "City-Maker" +
                (trafficSystem != null
                    ? ", Traffic System"
                    : string.Empty) +
                (carContainer != null
                    ? ", CarContainer"
                    : string.Empty) +
                (backdrop != null
                    ? ", Background"
                    : string.Empty);

            Debug.Log(
                "Motor City: Fantastic City Generator runtime package copied without map modifications. " +
                $"Source={scene.path}, Roots={includedRoots}, Renderers={renderers}, prefab={RuntimePrefab}");

            EditorUtility.DisplayDialog(
                "Motor City — FCG Runtime City",
                "Готово.\n\n" +
                $"Источник: {scene.path}\n" +
                $"Включено: {includedRoots}\n" +
                $"Renderer'ов: {renderers}\n\n" +
                "CityVisual.prefab содержит город и найденные FCG traffic roots без автоматических изменений.",
                "OK");
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "Motor City: failed to bake FCG runtime city. " +
                exception);

            EditorUtility.DisplayDialog(
                "Motor City — FCG Runtime City",
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

            if (trafficSystem != null)
            {
                trafficSystem.transform.SetParent(
                    trafficOriginalParent,
                    true);
            }

            if (carContainer != null)
            {
                carContainer.transform.SetParent(
                    carContainerOriginalParent,
                    true);
            }

            if (backdrop != null)
            {
                UnityEngine.Object.DestroyImmediate(
                    backdrop);
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

    private static GameObject CreateRuntimeBackdrop(
        GameObject citySource)
    {
        if (citySource == null)
            return null;

        GameObject prefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                FcgBackdropPrefab);

        if (prefab == null)
        {
            Debug.LogWarning(
                "Motor City: FCG background prefab was not found. " +
                "The runtime city will be built without the distant skyline.");

            return null;
        }

        GameObject backdrop =
            UnityEngine.Object.Instantiate(
                prefab);

        backdrop.name =
            "MotorCity_Background";

        Bounds cityBounds =
            CalculateRendererBounds(
                citySource);

        Bounds backdropBounds =
            CalculateRendererBounds(
                backdrop);

        float citySize =
            Mathf.Max(
                cityBounds.size.x,
                cityBounds.size.z);

        float backdropSize =
            Mathf.Max(
                backdropBounds.size.x,
                backdropBounds.size.z);

        if (citySize > 10f &&
            backdropSize > 0.1f)
        {
            float desiredSize =
                Mathf.Clamp(
                    citySize * 1.45f,
                    1400f,
                    6500f);

            float scale =
                desiredSize /
                backdropSize;

            backdrop.transform.localScale *=
                scale;
        }

        backdrop.transform.position =
            new Vector3(
                cityBounds.center.x,
                cityBounds.min.y - 2f,
                cityBounds.center.z);

        Material material =
            CreateOrUpdateBackdropMaterial(
                backdrop);

        foreach (Renderer renderer in
                 backdrop.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer == null)
                continue;

            if (material != null)
            {
                Material[] materials =
                    renderer.sharedMaterials;

                for (int i = 0;
                     i < materials.Length;
                     i++)
                {
                    materials[i] =
                        material;
                }

                renderer.sharedMaterials =
                    materials;
            }

            renderer.shadowCastingMode =
                UnityEngine.Rendering.ShadowCastingMode.Off;

            renderer.receiveShadows =
                false;

            renderer.lightProbeUsage =
                UnityEngine.Rendering.LightProbeUsage.Off;

            renderer.reflectionProbeUsage =
                UnityEngine.Rendering.ReflectionProbeUsage.Off;

            renderer.motionVectorGenerationMode =
                MotionVectorGenerationMode.ForceNoMotion;
        }

        foreach (Collider collider in
                 backdrop.GetComponentsInChildren<Collider>(true))
        {
            UnityEngine.Object.DestroyImmediate(
                collider);
        }

        return backdrop;
    }

    private static Material CreateOrUpdateBackdropMaterial(
        GameObject backdrop)
    {
        Shader shader =
            Shader.Find(
                "MotorCity/CityBackdrop");

        if (shader == null)
        {
            Debug.LogWarning(
                "Motor City: CityBackdrop shader was not found.");

            return null;
        }

        Texture texture =
            null;

        Renderer sourceRenderer =
            backdrop.GetComponentInChildren<Renderer>(
                true);

        if (sourceRenderer != null &&
            sourceRenderer.sharedMaterial != null)
        {
            Material sourceMaterial =
                sourceRenderer.sharedMaterial;

            if (sourceMaterial.HasProperty(
                    "_MainTex"))
            {
                texture =
                    sourceMaterial.GetTexture(
                        "_MainTex");
            }
            else if (sourceMaterial.HasProperty(
                         "_BaseMap"))
            {
                texture =
                    sourceMaterial.GetTexture(
                        "_BaseMap");
            }
        }

        Material material =
            AssetDatabase.LoadAssetAtPath<Material>(
                BackdropMaterial);

        if (material == null)
        {
            material =
                new Material(
                    shader);

            AssetDatabase.CreateAsset(
                material,
                BackdropMaterial);
        }
        else
        {
            material.shader =
                shader;
        }

        material.name =
            "MotorCity_FCGBackdrop";

        material.enableInstancing =
            true;

        if (texture != null &&
            material.HasProperty(
                "_BaseMap"))
        {
            material.SetTexture(
                "_BaseMap",
                texture);
        }

        if (material.HasProperty(
                "_DayTint"))
        {
            material.SetColor(
                "_DayTint",
                new Color(
                    0.78f,
                    0.82f,
                    0.86f,
                    1f));
        }

        if (material.HasProperty(
                "_NightTint"))
        {
            material.SetColor(
                "_NightTint",
                new Color(
                    0.055f,
                    0.075f,
                    0.11f,
                    1f));
        }

        if (material.HasProperty(
                "_NightBrightness"))
        {
            material.SetFloat(
                "_NightBrightness",
                0.42f);
        }

        EditorUtility.SetDirty(
            material);

        return material;
    }

    private static Bounds CalculateRendererBounds(
        GameObject root)
    {
        Renderer[] renderers =
            root.GetComponentsInChildren<Renderer>(
                true);

        if (renderers.Length == 0)
        {
            return
                new Bounds(
                    root.transform.position,
                    Vector3.one);
        }

        Bounds bounds =
            renderers[0].bounds;

        for (int i = 1;
             i < renderers.Length;
             i++)
        {
            bounds.Encapsulate(
                renderers[i].bounds);
        }

        return bounds;
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

    private static GameObject FindSceneRoot(
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

            string normalized =
                NormalizeName(
                    root.name);

            if (normalized ==
                    wanted ||
                normalized ==
                    wanted +
                    "clone")
            {
                return root;
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
