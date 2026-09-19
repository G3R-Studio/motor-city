#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using MotorCity.World;
using UnityEditor;
using UnityEngine;

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

    [MenuItem("Motor City/Fantastic City Generator/Build Day-Night Settings")]
    public static void BuildMenu()
    {
        Build(
            true);
    }

    [MenuItem("Motor City/Fantastic City Generator/Import DayNight Prefab...")]
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
        DayNightSettings existing =
            AssetDatabase.LoadAssetAtPath<DayNightSettings>(
                SettingsPath);

        if (existing != null)
        {
            SerializedObject serialized =
                new(
                    existing);

            SerializedProperty dayMaterials =
                serialized.FindProperty(
                    "dayMaterials");

            SerializedProperty nightMaterials =
                serialized.FindProperty(
                    "nightMaterials");

            if (dayMaterials != null &&
                nightMaterials != null &&
                dayMaterials.arraySize > 0 &&
                nightMaterials.arraySize > 0)
            {
                return;
            }
        }

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

        CopyMaterialArray(
            source,
            "materialDay",
            destination,
            "dayMaterials");

        CopyMaterialArray(
            source,
            "materialNight",
            destination,
            "nightMaterials");

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

    private static void CopyMaterialArray(
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
            to == null ||
            !from.isArray ||
            !to.isArray)
            return;

        to.arraySize =
            from.arraySize;

        for (int i = 0;
             i < from.arraySize;
             i++)
        {
            SerializedProperty fromElement =
                from.GetArrayElementAtIndex(
                    i);

            SerializedProperty toElement =
                to.GetArrayElementAtIndex(
                    i);

            toElement.objectReferenceValue =
                fromElement.objectReferenceValue;
        }
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
