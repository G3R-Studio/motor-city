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

    [MenuItem("Motor City/Fantastic City Generator/Build Runtime City from Saved FCG City")]
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
            StripGeneratorRuntimeComponents(
                clone);

            int colliders =
                EnsureDriveableColliders(
                    clone);

            CityCollisionUtility.Result collisionResult =
                CityCollisionUtility.Prepare(
                    clone);

            MarkStatic(
                clone);

            PrefabUtility.SaveAsPrefabAsset(
                clone,
                RuntimePrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            int renderers =
                clone.GetComponentsInChildren<Renderer>(true).Length;

            Debug.Log(
                "Motor City: Fantastic City Generator runtime city baked. " +
                $"Source={scene.path}, Renderers={renderers}, " +
                $"added driveable colliders={colliders}, " +
                $"pass-through prop colliders disabled={collisionResult.DisabledStreetPropColliders}, " +
                $"building MeshColliders added={collisionResult.AddedBuildingMeshColliders}, " +
                $"safety floor={collisionResult.SafetyFloorReady}, prefab={RuntimePrefab}");

            EditorUtility.DisplayDialog(
                "Motor City — FCG Runtime City",
                "Готово.\n\n" +
                $"Источник: {scene.path}\n" +
                $"Renderer'ов: {renderers}\n" +
                $"Добавлено дорожных/хайвей/парковочных MeshCollider: {colliders}\n" +
                $"Добавлено MeshCollider зданий: {collisionResult.AddedBuildingMeshColliders}\n" +
                $"Отключено коллайдеров проезжаемых городских объектов: {collisionResult.DisabledStreetPropColliders}\n" +
                $"Страховочный пол: {(collisionResult.SafetyFloorReady ? "да" : "нет")}\n\n" +
                "Runtime-город сохранён локально и переживёт git reset.",
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
        if (item == null)
            return false;

        string normalized =
            NormalizeName(
                item.name);

        return
            normalized.StartsWith(
                "spotlight",
                StringComparison.OrdinalIgnoreCase);
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

            if (!ShouldHaveDriveableCollider(
                    filter.transform,
                    renderer))
                continue;

            if (filter.GetComponent<Collider>() != null)
                continue;

            MeshCollider collider =
                filter.gameObject.AddComponent<MeshCollider>();

            collider.sharedMesh =
                filter.sharedMesh;

            collider.convex =
                false;

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
