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

        GameObject clone =
            UnityEngine.Object.Instantiate(
                source);

        clone.name =
            "MotorCity_FCGCity";

        try
        {
            // The authored FCG city is the single source of truth.
            // Building the runtime prefab must not alter colliders, props,
            // parked vehicles, signals, lights or any other map content.
            PrefabUtility.SaveAsPrefabAsset(
                clone,
                RuntimePrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            int renderers =
                clone.GetComponentsInChildren<Renderer>(true).Length;

            Debug.Log(
                "Motor City: Fantastic City Generator runtime city copied without map modifications. " +
                $"Source={scene.path}, Renderers={renderers}, prefab={RuntimePrefab}");

            EditorUtility.DisplayDialog(
                "Motor City — FCG Runtime City",
                "Готово.\n\n" +
                $"Источник: {scene.path}\n" +
                $"Renderer'ов: {renderers}\n\n" +
                "CityVisual.prefab сохранён без автоматических изменений карты.",
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

            FantasticCityGeneratorSceneSource.FinishSourceScene(
                scene,
                openedTemporarily,
                previousActiveScene,
                true);
        }
    }

    private static void StripGeneratorRuntimeComponents(
        GameObject root)
    {
        foreach (MonoBehaviour behaviour in
                 root.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (behaviour != null)
                UnityEngine.Object.DestroyImmediate(
                    behaviour);
        }

        foreach (Camera camera in
                 root.GetComponentsInChildren<Camera>(true))
        {
            if (camera != null)
                UnityEngine.Object.DestroyImmediate(
                    camera);
        }

        foreach (AudioListener listener in
                 root.GetComponentsInChildren<AudioListener>(true))
        {
            if (listener != null)
                UnityEngine.Object.DestroyImmediate(
                    listener);
        }

        foreach (AudioSource source in
                 root.GetComponentsInChildren<AudioSource>(true))
        {
            if (source != null)
                UnityEngine.Object.DestroyImmediate(
                    source);
        }

        foreach (Light light in
                 root.GetComponentsInChildren<Light>(true))
        {
            if (light == null)
                continue;

            if (IsFcgSpotLight(
                    light.transform))
            {
                light.enabled =
                    false;

                light.lightmapBakeType =
                    LightmapBakeType.Realtime;

                light.shadows =
                    LightShadows.None;

                continue;
            }

            UnityEngine.Object.DestroyImmediate(
                light);
        }

        foreach (Rigidbody body in
                 root.GetComponentsInChildren<Rigidbody>(true))
        {
            if (body != null)
                UnityEngine.Object.DestroyImmediate(
                    body);
        }

        foreach (ParticleSystem particle in
                 root.GetComponentsInChildren<ParticleSystem>(true))
        {
            if (particle != null)
                UnityEngine.Object.DestroyImmediate(
                    particle);
        }

        foreach (Transform item in
                 root.GetComponentsInChildren<Transform>(true))
        {
            if (item != null)
            {
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(
                    item.gameObject);
            }
        }
    }

    private static bool IsFcgSpotLight(
        Transform item)
    {
        Transform current =
            item;

        while (current != null)
        {
            string normalized =
                NormalizeName(
                    current.name);

            if (normalized.StartsWith(
                    "spotlight",
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(
                    current.name,
                    "MotorCity_FCGCity",
                    StringComparison.OrdinalIgnoreCase) ||
                string.Equals(
                    current.name,
                    "City-Maker",
                    StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            current =
                current.parent;
        }

        return false;
    }

    private static string NormalizeName(
        string value)
    {
        if (string.IsNullOrWhiteSpace(
                value))
            return string.Empty;

        return
            new string(
                value
                    .ToLowerInvariant()
                    .Where(
                        char.IsLetterOrDigit)
                    .ToArray());
    }

    private static bool IsStreetLightHierarchy(
        Transform item)
    {
        Transform current =
            item;

        while (current != null)
        {
            string name =
                current.name.ToLowerInvariant();

            if (name.StartsWith("streetlight") ||
                name.StartsWith("parklamp") ||
                name.Contains("street-light") ||
                name.Contains("street_light"))
            {
                return true;
            }

            current =
                current.parent;
        }

        return false;
    }

    private static int EnsureDriveableColliders(
        GameObject root)
    {
        int added =
            0;

        foreach (MeshFilter filter in
                 root.GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter == null ||
                filter.sharedMesh == null)
                continue;

            Renderer renderer =
                filter.GetComponent<Renderer>();

            if (renderer == null)
                continue;

            string meshName =
                filter.name.ToLowerInvariant();

            bool parking =
                meshName.StartsWith("park-04") ||
                meshName.StartsWith("park-05") ||
                meshName.StartsWith("park-06") ||
                meshName.StartsWith("park-08");

            if (!parking)
                continue;

            if (filter.GetComponent<Collider>() != null)
                continue;

            BoxCollider box =
                filter.gameObject.AddComponent<BoxCollider>();

            Bounds localBounds =
                renderer.localBounds;

            Vector3 size =
                localBounds.size;

            size.y =
                Mathf.Max(
                    size.y,
                    0.2f);

            Vector3 center =
                localBounds.center;

            center.y +=
                size.y *
                0.5f;

            box.center =
                center;

            box.size =
                size;

            added++;
        }

        return added;
    }

    private static bool ShouldHaveDriveableCollider(
        Transform transform,
        Renderer renderer)
    {
        string path =
            GetHierarchyPath(
                    transform)
                .ToLowerInvariant();

        string meshName =
            transform.name.ToLowerInvariant();

        bool parking =
            meshName.StartsWith("park-04") ||
            meshName.StartsWith("park-05") ||
            meshName.StartsWith("park-06") ||
            meshName.StartsWith("park-08");

        if (meshName.Contains("guardrail") ||
            meshName.Contains("guard-rail") ||
            path.Contains("guardrail") ||
            path.Contains("guard-rail"))
            return false;

        bool driveableMaterial =
            renderer.sharedMaterials.Any(
                material =>
                    material != null &&
                    (material.name.IndexOf(
                         "road",
                         StringComparison.OrdinalIgnoreCase) >= 0 ||
                     material.name.IndexOf(
                         "highway",
                         StringComparison.OrdinalIgnoreCase) >= 0));

        bool mainCityMesh =
            path.Contains("/meshes/") ||
            meshName.StartsWith("bd-") ||
            meshName.StartsWith("double-block");

        bool highwayMesh =
            meshName.StartsWith("hw-") ||
            path.Contains("/hw-");

        return
            parking ||
            (driveableMaterial &&
             (mainCityMesh ||
              highwayMesh));
    }

    private static string GetHierarchyPath(
        Transform transform)
    {
        string path =
            transform.name;

        Transform current =
            transform.parent;

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

    private static void MarkStatic(
        GameObject root)
    {
        foreach (Transform item in
                 root.GetComponentsInChildren<Transform>(true))
        {
            if (item != null)
                item.gameObject.isStatic =
                    true;
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
