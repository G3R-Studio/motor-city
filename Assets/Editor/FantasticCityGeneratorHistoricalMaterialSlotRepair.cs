#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class FantasticCityGeneratorHistoricalMaterialSlotRepair
{
    private const string MaterialRoot =
        "Assets/Resources/MotorCity/Environment/FCGMaterials";

    private sealed class RepairRule
    {
        public string Path;
        public string Mesh;
        public string[] Materials;
    }

    private static readonly RepairRule[] Rules =
    {
        new RepairRule
        {
            Path = "City-Maker/Double-Block-02(Clone)/Buildings/Double/222/137,8801/building",
            Mesh = "M-003",
            Materials = new[]
            {
                "FCG_Roads",
                "FCG_Atlas-1"
            }
        },
        new RepairRule
        {
            Path = "City-Maker/Double-Block-02(Clone)/Buildings/Double/222/137,8801/building",
            Mesh = "M-016",
            Materials = new[]
            {
                "FCG_Atlas-1",
                "FCG_Wins-02",
                "FCG_Wins",
                "FCG_WinGlass-01",
                "FCG_WinGlass-01-D"
            }
        },
        new RepairRule
        {
            Path = "City-Maker/Border-Medium 2(Clone)/BD-BL",
            Mesh = "BD-BL",
            Materials = new[]
            {
                "FCG_Roads",
                "FCG_Atlas-1",
                "FCG_Grass-01",
                "FCG_HighWay"
            }
        },
        new RepairRule
        {
            Path = "City-Maker/Border-Medium 2(Clone)/BD-BL/BD-BL-Sidewalk",
            Mesh = "BD-BL-Sidewalk",
            Materials = new[]
            {
                "FCG_Roads",
                "FCG_Grass-01",
                "FCG_Atlas-1"
            }
        }
    };

    public static void Repair()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog(
                "Motor City — Historical Material Slots",
                "Останови Play Mode перед восстановлением.",
                "OK");
            return;
        }

        if (!FantasticCityGeneratorSceneSource.TryOpenSourceScene(
                out Scene scene,
                out bool openedTemporarily,
                out Scene previousActiveScene))
        {
            EditorUtility.DisplayDialog(
                "Motor City — Historical Material Slots",
                "Не найдена сохранённая FCG-сцена в Assets/LocalGenerated.",
                "OK");
            return;
        }

        try
        {
            Dictionary<string, Material> materials =
                LoadGeneratedMaterials();

            var missingMaterials =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase);

            foreach (RepairRule rule in Rules)
            {
                foreach (string name in rule.Materials)
                {
                    if (!materials.ContainsKey(name))
                        missingMaterials.Add(name);
                }
            }

            if (missingMaterials.Count > 0)
            {
                EditorUtility.DisplayDialog(
                    "Motor City — Historical Material Slots",
                    "Не найдены необходимые материалы:\n\n" +
                    string.Join(", ", missingMaterials) +
                    "\n\nСначала восстанови FCGMaterials.",
                    "OK");
                return;
            }

            Renderer[] renderers =
                scene
                    .GetRootGameObjects()
                    .SelectMany(
                        root =>
                            root.GetComponentsInChildren<Renderer>(true))
                    .ToArray();

            int repaired =
                0;

            var missingTargets =
                new List<string>();

            foreach (RepairRule rule in Rules)
            {
                Renderer target =
                    renderers.FirstOrDefault(
                        renderer =>
                            renderer != null &&
                            string.Equals(
                                GetHierarchyPath(renderer.transform),
                                rule.Path,
                                StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(
                                GetMeshName(renderer),
                                rule.Mesh,
                                StringComparison.OrdinalIgnoreCase));

                if (target == null)
                {
                    missingTargets.Add(
                        $"{rule.Path} | mesh={rule.Mesh}");
                    continue;
                }

                Material[] exact =
                    rule.Materials
                        .Select(name => materials[name])
                        .ToArray();

                Undo.RecordObject(
                    target,
                    "Restore historical FCG material slots");

                target.sharedMaterials =
                    exact;

                EditorUtility.SetDirty(
                    target);

                repaired++;

                Debug.Log(
                    "Motor City: restored historical FCG material slots. " +
                    $"Path={rule.Path}, mesh={rule.Mesh}, " +
                    $"materials={string.Join(", ", rule.Materials)}");
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                scene);

            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(
                scene);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string missingText =
                missingTargets.Count == 0
                    ? "нет"
                    : string.Join("\n", missingTargets);

            EditorUtility.DisplayDialog(
                "Motor City — Historical Material Slots",
                "Готово.\n\n" +
                $"Точно восстановлено renderer'ов: {repaired}/{Rules.Length}\n" +
                $"Не найденные цели: {missingText}\n\n" +
                "Восстановлены только 4 material-набора, которые отличались от исторического отчёта.\n" +
                "Теперь запусти Diagnose Material Slot Mismatches ещё раз, затем Build Runtime City.",
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

    private static Dictionary<string, Material> LoadGeneratedMaterials()
    {
        var result =
            new Dictionary<string, Material>(
                StringComparer.OrdinalIgnoreCase);

        string[] guids =
            AssetDatabase.FindAssets(
                "t:Material",
                new[]
                {
                    MaterialRoot
                });

        foreach (string guid in guids)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(
                    guid);

            Material material =
                AssetDatabase.LoadAssetAtPath<Material>(
                    path);

            if (material == null ||
                string.IsNullOrWhiteSpace(material.name))
                continue;

            if (!result.ContainsKey(material.name))
                result.Add(material.name, material);
        }

        return result;
    }

    private static string GetMeshName(
        Renderer renderer)
    {
        MeshFilter filter =
            renderer.GetComponent<MeshFilter>();

        if (filter != null &&
            filter.sharedMesh != null)
            return filter.sharedMesh.name;

        SkinnedMeshRenderer skinned =
            renderer as SkinnedMeshRenderer;

        return skinned != null &&
               skinned.sharedMesh != null
            ? skinned.sharedMesh.name
            : string.Empty;
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
