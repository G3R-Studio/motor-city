#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Explicit, one-time authoring migration; never runs in Play Mode or builds.
/// Moves the real lamp lights out of the legacy volumetric cone, then deletes
/// the cone from FCG_Workbench only. The source FCG prefabs are not modified.
/// </summary>
public static class MotorCityOneShotRemoveLegacyLightV
{
    private const string MenuPath =
        "Motor City/Fantastic City Generator/6 - Remove Legacy LightV Cones (One-Time)";

    [MenuItem(MenuPath)]
    public static void RemoveFromWorkbench()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Motor City - LightV",
                "Останови Play Mode перед изменением города.", "OK");
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded ||
            !string.Equals(scene.path, FantasticCityGeneratorWorkbench.WorkbenchScene,
                StringComparison.OrdinalIgnoreCase))
        {
            EditorUtility.DisplayDialog("Motor City - LightV",
                "Открой именно Assets/LocalGenerated/FCG_Workbench.unity. " +
                "Prototype и исходные FCG prefab-ассеты команда не изменяет.", "OK");
            return;
        }

        GameObject city = FantasticCityGeneratorSceneSource.FindCityRoot(scene);
        if (city == null)
        {
            EditorUtility.DisplayDialog("Motor City - LightV",
                "В открытой сцене нет City-Maker. Ничего не изменено.", "OK");
            return;
        }

        // The on-disk scene backup must include all existing manual edits.
        if (scene.isDirty)
        {
            EditorUtility.DisplayDialog("Motor City - LightV",
                "Сначала сохрани FCG_Workbench (Ctrl+S), затем запусти команду снова. " +
                "Это необходимо для резервной копии.", "OK");
            return;
        }

        var cones = city.GetComponentsInChildren<Transform>(true)
            .Where(t => t != null &&
                string.Equals(t.name, "_LightV", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (cones.Length == 0)
        {
            EditorUtility.DisplayDialog("Motor City - LightV",
                "_LightV не найдены. Изменения не нужны.", "OK");
            return;
        }

        int childCount = 0;
        int spotLights = 0;
        int noLightCones = 0;
        var prefabRoots = new HashSet<GameObject>();

        // Validate the entire batch before performing any destructive edits.
        foreach (Transform cone in cones)
        {
            Transform parent = cone.parent;
            if (parent == null || !IsUnderLamp(cone, city.transform))
            {
                Abort("Найден _LightV вне иерархии фонаря: " +
                      HierarchyPath(cone) + ". Ничего не изменено.");
                return;
            }

            Component[] components = cone.GetComponents<Component>();
            foreach (Component component in components)
            {
                if (component == null ||
                    (component is not Transform &&
                     component is not MeshRenderer &&
                     component is not MeshFilter))
                {
                    Abort("У _LightV обнаружен неожиданный компонент: " +
                          HierarchyPath(cone) + ". Удаление небезопасно.");
                    return;
                }
            }

            childCount += cone.childCount;
            int lightsHere = cone.GetComponentsInChildren<Light>(true).Length;
            spotLights += lightsHere;
            if (lightsHere == 0)
                noLightCones++;

            GameObject prefabRoot =
                PrefabUtility.GetOutermostPrefabInstanceRoot(cone.gameObject);

            if (prefabRoot == null)
                continue;

            // Unpacking a whole generated neighborhood or the entire city
            // would create huge unrelated overrides. Allow only isolated lamp
            // prefab instances and otherwise stop before touching the scene.
            if (!IsLampRoot(prefabRoot.name) ||
                prefabRoot.GetComponentsInChildren<Transform>(true).Length > 80)
            {
                Abort("Обнаружен вложенный prefab, который нельзя безопасно " +
                      "распаковать автоматически: " + HierarchyPath(cone) +
                      "\nPrefab root: " + prefabRoot.name +
                      "\nНичего не изменено.");
                return;
            }

            prefabRoots.Add(prefabRoot);
        }

        string preview =
            "Найдено конусов _LightV: " + cones.Length +
            "\nДочерних объектов для переноса: " + childCount +
            "\nСохранённых компонентов Light: " + spotLights +
            "\nКонусов без Light: " + noLightCones +
            "\nОтдельных prefab-экземпляров фонарей для распаковки: " +
            prefabRoots.Count +
            "\n\nИсходные FCG prefab-ассеты НЕ изменятся." +
            "\nСцена сначала будет скопирована в Temp/MotorCityOneShot." +
            "\nПосле операции сохрани FCG_Workbench и отдельно запусти " +
            "5 - Build Runtime City.\n\nПродолжить?";

        if (!EditorUtility.DisplayDialog(
                "Motor City - Preview LightV removal", preview,
                "Удалить конусы", "Отмена"))
            return;

        string sceneAbsolutePath = Path.Combine(
            Directory.GetParent(Application.dataPath).FullName, scene.path);
        string backupDir = Path.Combine(
            Directory.GetParent(Application.dataPath).FullName,
            "Temp", "MotorCityOneShot");
        string backup = Path.Combine(backupDir,
            "FCG_Workbench_before_LightV_" +
            DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff") + ".unity");

        try
        {
            Directory.CreateDirectory(backupDir);
            File.Copy(sceneAbsolutePath, backup, false);
        }
        catch (Exception ex)
        {
            Abort("Не удалось создать резервную копию сцены. " +
                  "Изменения отменены.\n" + ex.Message);
            return;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("One-time remove FCG LightV cones");
        int removed = 0;
        int moved = 0;

        try
        {
            foreach (GameObject prefabRoot in prefabRoots)
            {
                if (prefabRoot != null)
                {
                    PrefabUtility.UnpackPrefabInstance(
                        prefabRoot,
                        PrefabUnpackMode.Completely,
                        InteractionMode.UserAction);
                }
            }

            foreach (Transform cone in cones)
            {
                if (cone == null)
                    continue;

                Transform parent = cone.parent;
                if (parent == null)
                    throw new InvalidOperationException(
                        "LightV parent unexpectedly disappeared.");

                while (cone.childCount > 0)
                {
                    Transform child = cone.GetChild(0);
                    Vector3 position = child.position;
                    Quaternion rotation = child.rotation;

                    Undo.SetTransformParent(child, parent,
                        "Preserve FCG streetlamp light");

                    // Undo.SetTransformParent preserves world transforms.
                    // Refuse silent displacement if an unusual hierarchy breaks it.
                    if ((child.position - position).sqrMagnitude > 0.0001f ||
                        Quaternion.Angle(child.rotation, rotation) > 0.1f)
                    {
                        throw new InvalidOperationException(
                            "Moved child changed world pose: " +
                            HierarchyPath(child));
                    }

                    moved++;
                }

                Undo.DestroyObjectImmediate(cone.gameObject);
                removed++;
            }

            Undo.CollapseUndoOperations(undoGroup);
            EditorSceneManager.MarkSceneDirty(scene);

            string message =
                "Удалено _LightV: " + removed +
                "\nДочерних объектов перенесено: " + moved +
                "\nКомпонентов Light сохранено: " + spotLights +
                "\n\nРезервная копия:\n" + backup +
                "\n\nСохрани сцену Ctrl+S, затем запусти " +
                "Motor City > Fantastic City Generator > " +
                "5 - Build Runtime City. После этого запушь изменения.";
            Debug.Log("Motor City LightV migration: " + message);
            EditorUtility.DisplayDialog("Motor City - LightV complete",
                message, "OK");
        }
        catch (Exception ex)
        {
            Undo.RevertAllDownToGroup(undoGroup);
            Debug.LogError("Motor City LightV one-shot failed: " + ex);
            Abort("Операция прервана, выполнена попытка Undo. " +
                  "Если Unity сохранил частичные prefab-изменения, " +
                  "восстанови исходную сцену из резервной копии:\n" + backup +
                  "\nОшибка: " + ex.Message);
        }
    }

    private static bool IsUnderLamp(Transform cone, Transform cityRoot)
    {
        Transform current = cone.parent;
        while (current != null && current != cityRoot)
        {
            if (IsLampRoot(current.name))
                return true;
            current = current.parent;
        }
        return false;
    }

    private static bool IsLampRoot(string name)
    {
        return name.StartsWith("StreetLight", StringComparison.OrdinalIgnoreCase) ||
               name.StartsWith("ParkLamp", StringComparison.OrdinalIgnoreCase) ||
               name.StartsWith("ParkLight", StringComparison.OrdinalIgnoreCase);
    }

    private static string HierarchyPath(Transform transform)
    {
        var names = new List<string>();
        while (transform != null)
        {
            names.Add(transform.name);
            transform = transform.parent;
        }
        names.Reverse();
        return string.Join("/", names);
    }

    private static void Abort(string message)
    {
        Debug.LogWarning("Motor City LightV one-shot: " + message);
        EditorUtility.DisplayDialog("Motor City - LightV stopped", message, "OK");
    }
}
#endif
