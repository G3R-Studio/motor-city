#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using MotorCity.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

[InitializeOnLoad]
public static class FantasticCityGeneratorDayNightBuilder
{
    private const string RuntimeRoot =
        "Assets/Resources/MotorCity/Environment";

    private const string SettingsPath =
        RuntimeRoot +
        "/DayNightSettings.asset";

    private const string ImportedSourcePath =
        "Assets/LocalGenerated/MotorCity_DayNightSource.prefab";

    static FantasticCityGeneratorDayNightBuilder()
    {
        EditorApplication.delayCall +=
            TryBuildSilently;
    }

    public static void BuildMenu()
    {
        Build(
            true);
    }

    public static void ImportPrefabMenu()
    {
        string path =
            EditorUtility.OpenFilePanel(
                "Choose DayNight.prefab",
                Environment.GetFolderPath(
                    Environment.SpecialFolder.UserProfile),
                "prefab");

        if (string.IsNullOrWhiteSpace(
                path))
            return;

        EnsureFolder(
            "Assets/LocalGenerated");

        string destination =
            Path.GetFullPath(
                ImportedSourcePath);

        string source =
            Path.GetFullPath(
                path);

        if (!string.Equals(
                source,
                destination,
                StringComparison.OrdinalIgnoreCase))
        {
            File.Copy(
                source,
                destination,
                true);
        }

        AssetDatabase.ImportAsset(
            ImportedSourcePath,
            ImportAssetOptions.ForceUpdate);

        Build(
            true);
    }

    private static void TryBuildSilently()
    {
        if (AssetDatabase.LoadAssetAtPath<DayNightSettings>(
                SettingsPath) != null)
            return;

        Build(
            false);
    }

    private static void Build(
        bool showDialogs)
    {
        if (!TryFindSource(
                out string sourcePath,
                out MonoBehaviour sourceComponent))
        {
            if (showDialogs)
            {
                EditorUtility.DisplayDialog(
                    "Motor City — Day/Night",
                    "Не найден FCG DayNight prefab.\n\n" +
                    "Если prefab лежит отдельно, используй " +
                    "Motor City > Fantastic City Generator > Import DayNight Prefab...",
                    "OK");
            }

            return;
        }

        SerializedObject source =
            new(
                sourceComponent);

        Material daySkybox =
            source.FindProperty(
                    "skyBoxDay")
                ?.objectReferenceValue as Material;

        Material nightSkybox =
            source.FindProperty(
                    "skyBoxNight")
                ?.objectReferenceValue as Material;

        if (daySkybox == null &&
            nightSkybox == null)
        {
            if (showDialogs)
            {
                EditorUtility.DisplayDialog(
                    "Motor City — Day/Night",
                    "В выбранном prefab не найдены skyBoxDay / skyBoxNight.",
                    "OK");
            }

            return;
        }

        EnsureFolder(
            RuntimeRoot);

        DayNightSettings settings =
            AssetDatabase.LoadAssetAtPath<DayNightSettings>(
                SettingsPath);

        if (settings == null)
        {
            settings =
                ScriptableObject.CreateInstance<DayNightSettings>();

            AssetDatabase.CreateAsset(
                settings,
                SettingsPath);
        }

        SerializedObject destination =
            new(
                settings);

        SetObject(
            destination,
            "daySkybox",
            daySkybox);

        SetObject(
            destination,
            "nightSkybox",
            nightSkybox);

        CopyColor(
            source,
            "skyColorDay",
            destination,
            "daySkyColor");

        CopyColor(
            source,
            "equatorColorDay",
            destination,
            "dayEquatorColor");

        CopyColor(
            source,
            "skyColorNight",
            destination,
            "nightSkyColor");

        CopyColor(
            source,
            "equatorColorNight",
            destination,
            "nightEquatorColor");

        CopyColor(
            source,
            "sunLightColor",
            destination,
            "sunColor");

        CopyColor(
            source,
            "moonLightColor",
            destination,
            "moonColor");

        CopyIntensity(
            source,
            "intenseSunLight",
            destination,
            "sunIntensity",
            1.05f);

        CopyIntensity(
            source,
            "intenseMoonLight",
            destination,
            "moonIntensity",
            0.28f);

        if (GraphicsSettings.currentRenderPipeline != null)
        {
            // The installed FCG source in this project is the Standard
            // variant. Keep its skybox references, but use the lighting
            // values shipped with the FCG URP package so rebuilding these
            // settings cannot silently restore the weaker Standard profile.
            SetColor(
                destination,
                "nightSkyColor",
                new Color(
                    0.5849056f,
                    0.5849056f,
                    0.5849056f,
                    1f));

            SetColor(
                destination,
                "nightEquatorColor",
                new Color(
                    0.6509434f,
                    0.6509434f,
                    0.6509434f,
                    1f));

            SetColor(
                destination,
                "sunColor",
                new Color(
                    0.7921569f,
                    0.627451f,
                    0.38431373f,
                    1f));

            SetColor(
                destination,
                "moonColor",
                new Color(
                    0.6167675f,
                    0.6167675f,
                    0.7924528f,
                    1f));

            SetFloat(
                destination,
                "sunIntensity",
                1.1842105f);

            SetFloat(
                destination,
                "moonIntensity",
                0.2090909f);
        }

        destination.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(
            settings);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            "Motor City: FCG DayNight settings built. " +
            $"Source={sourcePath}, " +
            $"day sky={ClipName(daySkybox)}, " +
            $"night sky={ClipName(nightSkybox)}, " +
            $"asset={SettingsPath}");

        if (showDialogs)
        {
            EditorUtility.DisplayDialog(
                "Motor City — Day/Night",
                "Готово.\n\n" +
                $"Источник: {sourcePath}\n" +
                $"Дневное небо: {ClipName(daySkybox)}\n" +
                $"Ночное небо: {ClipName(nightSkybox)}\n\n" +
                "Цикл дня и ночи будет создан автоматически при Play.",
                "OK");
        }
    }

    private static bool TryFindSource(
        out string path,
        out MonoBehaviour component)
    {
        path =
            null;

        component =
            null;

        string[] roots =
        {
            "Assets/LocalGenerated",
            "Assets/Fantastic City Generator"
        };

        foreach (string root in roots)
        {
            if (!AssetDatabase.IsValidFolder(
                    root))
                continue;

            string[] guids =
                AssetDatabase.FindAssets(
                    "DayNight t:Prefab",
                    new[]
                    {
                        root
                    });

            foreach (string guid in guids)
            {
                string candidate =
                    AssetDatabase.GUIDToAssetPath(
                        guid);

                GameObject prefab =
                    AssetDatabase.LoadAssetAtPath<GameObject>(
                        candidate);

                if (prefab == null)
                    continue;

                foreach (MonoBehaviour behaviour in
                         prefab.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (behaviour == null)
                        continue;

                    SerializedObject serialized =
                        new(
                            behaviour);

                    if (serialized.FindProperty(
                            "skyBoxDay") == null ||
                        serialized.FindProperty(
                            "skyBoxNight") == null)
                        continue;

                    path =
                        candidate;

                    component =
                        behaviour;

                    return true;
                }
            }
        }

        return false;
    }

    private static void CopyColor(
        SerializedObject source,
        string sourceName,
        SerializedObject destination,
        string destinationName)
    {
        SerializedProperty from =
            source.FindProperty(
                sourceName);

        SerializedProperty to =
            destination.FindProperty(
                destinationName);

        if (from == null ||
            to == null)
            return;

        to.colorValue =
            from.colorValue;
    }

    private static void CopyIntensity(
        SerializedObject source,
        string sourceName,
        SerializedObject destination,
        string destinationName,
        float fallback)
    {
        SerializedProperty from =
            source.FindProperty(
                sourceName);

        SerializedProperty to =
            destination.FindProperty(
                destinationName);

        if (to == null)
            return;

        float value =
            from != null
                ? from.floatValue
                : fallback;

        if (value > 5f)
            value /=
                100f;

        to.floatValue =
            Mathf.Clamp(
                value,
                0.02f,
                2.5f);
    }

    private static void SetColor(
        SerializedObject destination,
        string propertyName,
        Color value)
    {
        SerializedProperty property =
            destination.FindProperty(
                propertyName);

        if (property != null)
        {
            property.colorValue =
                value;
        }
    }

    private static void SetFloat(
        SerializedObject destination,
        string propertyName,
        float value)
    {
        SerializedProperty property =
            destination.FindProperty(
                propertyName);

        if (property != null)
        {
            property.floatValue =
                value;
        }
    }

    private static void SetObject(
        SerializedObject destination,
        string propertyName,
        UnityEngine.Object value)
    {
        SerializedProperty property =
            destination.FindProperty(
                propertyName);

        if (property != null)
        {
            property.objectReferenceValue =
                value;
        }
    }

    private static string ClipName(
        UnityEngine.Object value)
    {
        return
            value != null
                ? value.name
                : "<none>";
    }

    private static void EnsureFolder(
        string path)
    {
        if (AssetDatabase.IsValidFolder(
                path))
            return;

        string parent =
            Path.GetDirectoryName(
                    path)
                ?.Replace(
                    '\\',
                    '/');

        string name =
            Path.GetFileName(
                path);

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
}
#endif
