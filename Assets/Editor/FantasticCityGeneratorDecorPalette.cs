#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class FantasticCityGeneratorDecorPalette : EditorWindow
{
    private readonly struct DecorItem
    {
        public DecorItem(
            string label,
            string path)
        {
            Label = label;
            Path = path;
        }

        public string Label { get; }
        public string Path { get; }
    }

    private static readonly DecorItem[] StreetItems =
    {
        new(
            "Bus Stop",
            "Assets/Fantastic City Generator/Objects/Others/Prefab/BusStop-01.prefab"),
        new(
            "Park Bench",
            "Assets/Fantastic City Generator/Objects/Parkbench/Prefab/Parkbench-01.prefab"),
        new(
            "Hydrant",
            "Assets/Fantastic City Generator/Objects/Others/Prefab/hydrant-01.prefab"),
        new(
            "Dumpster",
            "Assets/Fantastic City Generator/Objects/Others/Prefab/Dumpster.prefab"),
        new(
            "Trash Can",
            "Assets/Fantastic City Generator/Objects/Others/Prefab/Trash-01.prefab"),
        new(
            "Traffic Cone",
            "Assets/Fantastic City Generator/Objects/Others/Prefab/Cone.prefab"),
        new(
            "Park Lamp",
            "Assets/Fantastic City Generator/Objects/Others/Prefab/ParkLamp.prefab")
    };

    private static readonly DecorItem[] SignItems =
    {
        new(
            "Pedestrian Sign",
            "Assets/Fantastic City Generator/Objects/Signs/Prefab/pedestrian.prefab"),
        new(
            "Do Not Enter",
            "Assets/Fantastic City Generator/Objects/Signs/Prefab/do_not_enter.prefab"),
        new(
            "One Way Left",
            "Assets/Fantastic City Generator/Objects/Signs/Prefab/Oneway-Left.prefab"),
        new(
            "One Way Right",
            "Assets/Fantastic City Generator/Objects/Signs/Prefab/Oneway-Right.prefab"),
        new(
            "Bus Stop Sign",
            "Assets/Fantastic City Generator/Objects/Signs/Prefab/Bus Stop.prefab")
    };

    private static readonly DecorItem[] GreenItems =
    {
        new(
            "Flowerbed",
            "Assets/Fantastic City Generator/Objects/Flowerbed/Prefab/Flowerbed-01.prefab"),
        new(
            "Garden",
            "Assets/Fantastic City Generator/Objects/Flowerbed/Prefab/Garden-01.prefab"),
        new(
            "Small Park",
            "Assets/Fantastic City Generator/Objects/Flowerbed/Prefab/Park-01.prefab"),
        new(
            "Fountain",
            "Assets/Fantastic City Generator/Objects/Flowerbed/Prefab/Fwater-01.prefab")
    };

    private static readonly DecorItem[] ParkedVehicleItems =
    {
        new(
            "Parked Car",
            "Assets/Fantastic City Generator/Traffic System/Vehicles/Parked Prefabs/Car_06.prefab"),
        new(
            "Parked Gran Fury",
            "Assets/Fantastic City Generator/Traffic System/Vehicles/Parked Prefabs/GranFury.prefab"),
        new(
            "Parked Van",
            "Assets/Fantastic City Generator/Traffic System/Vehicles/Parked Prefabs/Furgao.prefab"),
        new(
            "Parked Truck",
            "Assets/Fantastic City Generator/Traffic System/Vehicles/Parked Prefabs/Truck-01.prefab")
    };

    private Vector2 scroll;

    [MenuItem("Motor City/Fantastic City Generator/6 - Open Decor Palette")]
    public static void Open()
    {
        FantasticCityGeneratorDecorPalette window =
            GetWindow<FantasticCityGeneratorDecorPalette>();

        window.titleContent =
            new GUIContent(
                "Motor City Decor");

        window.minSize =
            new Vector2(
                360f,
                500f);

        window.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(8);

        EditorGUILayout.LabelField(
            "Motor City — FCG Decor Palette",
            EditorStyles.boldLabel);

        EditorGUILayout.HelpBox(
            "Работай в FCG_Workbench. Объект ставится возле текущего Scene View, " +
            "по возможности прилипает к поверхности и сохраняется внутри City-Maker. " +
            "После расстановки: Ctrl+S → 4 - Fix Materials → 5 - Build Runtime City.",
            MessageType.Info);

        scroll =
            EditorGUILayout.BeginScrollView(
                scroll);

        DrawSection(
            "Street Props",
            StreetItems);

        DrawSection(
            "Signs",
            SignItems);

        DrawSection(
            "Greenery / Parks",
            GreenItems);

        DrawSection(
            "Parked Vehicles",
            ParkedVehicleItems);

        EditorGUILayout.EndScrollView();
    }

    private static void DrawSection(
        string title,
        DecorItem[] items)
    {
        EditorGUILayout.Space(10);

        EditorGUILayout.LabelField(
            title,
            EditorStyles.boldLabel);

        foreach (DecorItem item in items)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(
                    item.Label,
                    GUILayout.Width(180f));

                if (GUILayout.Button(
                        "Place",
                        GUILayout.Height(24f)))
                {
                    Place(
                        item);
                }
            }
        }
    }

    private static void Place(
        DecorItem item)
    {
        Scene scene =
            SceneManager.GetActiveScene();

        if (!scene.IsValid() ||
            !scene.isLoaded)
        {
            EditorUtility.DisplayDialog(
                "Motor City — Decor",
                "Нет активной сцены.",
                "OK");

            return;
        }

        GameObject cityRoot =
            FantasticCityGeneratorSceneSource.FindCityRoot(
                scene);

        if (cityRoot == null)
        {
            EditorUtility.DisplayDialog(
                "Motor City — Decor",
                "В активной сцене не найден City-Maker. Открой FCG_Workbench.",
                "OK");

            return;
        }

        GameObject prefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                item.Path);

        if (prefab == null)
        {
            EditorUtility.DisplayDialog(
                "Motor City — Decor",
                "Prefab не найден:\n" +
                item.Path +
                "\n\nПроверь, что Fantastic City Generator импортирован полностью.",
                "OK");

            return;
        }

        Transform decorRoot =
            FindOrCreateDecorRoot(
                cityRoot.transform);

        GameObject instance =
            PrefabUtility.InstantiatePrefab(
                prefab,
                scene) as GameObject;

        if (instance == null)
            return;

        Undo.RegisterCreatedObjectUndo(
            instance,
            "Place Motor City Decor");

        instance.transform.SetParent(
            decorRoot,
            true);

        Vector3 position =
            ResolvePlacementPoint();

        instance.transform.position =
            position;

        Selection.activeGameObject =
            instance;

        SceneView sceneView =
            SceneView.lastActiveSceneView;

        if (sceneView != null)
        {
            sceneView.FrameSelected();
        }

        EditorSceneManager.MarkSceneDirty(
            scene);
    }

    private static Transform FindOrCreateDecorRoot(
        Transform cityRoot)
    {
        Transform existing =
            cityRoot.Find(
                "MotorCity_Decor");

        if (existing != null)
            return existing;

        GameObject root =
            new("MotorCity_Decor");

        Undo.RegisterCreatedObjectUndo(
            root,
            "Create Motor City Decor Root");

        root.transform.SetParent(
            cityRoot,
            false);

        return
            root.transform;
    }

    private static Vector3 ResolvePlacementPoint()
    {
        SceneView sceneView =
            SceneView.lastActiveSceneView;

        Vector3 guess =
            sceneView != null
                ? sceneView.pivot
                : Vector3.zero;

        Vector3 rayOrigin =
            guess +
            Vector3.up *
            150f;

        RaycastHit[] hits =
            Physics.RaycastAll(
                rayOrigin,
                Vector3.down,
                400f,
                ~0,
                QueryTriggerInteraction.Ignore);

        if (hits != null &&
            hits.Length > 0)
        {
            Array.Sort(
                hits,
                (a, b) =>
                    a.distance.CompareTo(
                        b.distance));

            foreach (RaycastHit hit in hits)
            {
                if (hit.collider == null)
                    continue;

                string path =
                    HierarchyPath(
                            hit.collider.transform)
                        .ToLowerInvariant();

                if (path.Contains(
                        "city-maker") ||
                    path.Contains(
                        "motorcity_fcgcity"))
                {
                    return
                        hit.point;
                }
            }

            return
                hits[0].point;
        }

        guess.y =
            0f;

        return
            guess;
    }

    private static string HierarchyPath(
        Transform item)
    {
        if (item == null)
            return string.Empty;

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
